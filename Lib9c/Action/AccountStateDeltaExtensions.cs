using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using Bencodex.Types;
using Lib9c;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using LruCacheNet;
using Nekoyume.Helper;
using Nekoyume.Model.Coupons;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    public static class AccountStateDeltaExtensions
    {
        private const int SheetsCacheSize = 100;
        private static readonly LruCache<string, ISheet> SheetsCache = new LruCache<string, ISheet>(SheetsCacheSize);

        public static IAccountStateDelta MarkBalanceChanged(
            this IAccountStateDelta states,
            Currency currency,
            params Address[] accounts
        )
        {
            if (accounts.Length == 1)
            {
                return states.MintAsset(accounts[0], currency * 1);
            }
            else if (accounts.Length < 1)
            {
                return states;
            }

            for (int i = 1; i < accounts.Length; i++)
            {
                states = states.TransferAsset(accounts[i - 1], accounts[i], currency * 1, true);
            }

            return states;
        }


        public static IAccountStateDelta SetWorldBossKillReward(
            this IAccountStateDelta states,
            Address rewardInfoAddress,
            WorldBossKillRewardRecord rewardRecord,
            int rank,
            WorldBossState bossState,
            RuneWeightSheet runeWeightSheet,
            WorldBossKillRewardSheet worldBossKillRewardSheet,
            RuneSheet runeSheet,
            IRandom random,
            Address avatarAddress,
            Address agentAddress)
        {
            if (!rewardRecord.IsClaimable(bossState.Level))
            {
                throw new InvalidClaimException();
            }
#pragma warning disable LAA1002
            var filtered = rewardRecord
                .Where(kv => !kv.Value)
                .Select(kv => kv.Key)
                .ToList();
#pragma warning restore LAA1002
            foreach (var level in filtered)
            {
                List<FungibleAssetValue> rewards = RuneHelper.CalculateReward(
                    rank,
                    bossState.Id,
                    runeWeightSheet,
                    worldBossKillRewardSheet,
                    runeSheet,
                    random
                );
                rewardRecord[level] = true;
                foreach (var reward in rewards)
                {
                    if (reward.Currency.Equals(CrystalCalculator.CRYSTAL))
                    {
                        states = states.MintAsset(agentAddress, reward);
                    }
                    else
                    {
                        states = states.MintAsset(avatarAddress, reward);
                    }
                }
            }

            return states.SetState(rewardInfoAddress, rewardRecord.Serialize());
        }

#nullable enable
        public static IAccountStateDelta SetCouponWallet(
            this IAccountStateDelta states,
            Address agentAddress,
            IImmutableDictionary<Guid, Coupon> couponWallet,
            bool rehearsal = false)
        {
            Address walletAddress = agentAddress.Derive(CouponWalletKey);
            if (rehearsal)
            {
                return states.SetState(walletAddress, ActionBase.MarkChanged);
            }

            IValue serializedWallet = new Bencodex.Types.List(
                couponWallet.Values.OrderBy(c => c.Id).Select(v => v.Serialize())
            );
            return states.SetState(walletAddress, serializedWallet);
        }
#nullable disable

        public static IAccountStateDelta Mead(this IAccountStateDelta states, Address signer, BigInteger rawValue)
        {
            while (true)
            {
                var price = rawValue * Currencies.Mead;
                var balance = states.GetBalance(signer, Currencies.Mead);
                if (balance < price)
                {
                    var contractAddress = signer.Derive(nameof(BringEinheri));
                    if (states.GetState(contractAddress) is List contract && contract[1].ToBoolean())
                    {
                        var valkyrie = contract[0].ToAddress();
                        try
                        {
                            states = states.TransferAsset(valkyrie, signer, price);
                        }
                        catch (InsufficientBalanceException)
                        {
                            states = states.Mead(valkyrie, rawValue);
                            continue;
                        }
                    }
                    else
                    {
                        throw new InsufficientBalanceException("", signer, balance);
                    }
                }

                return states;
            }
        }
    }
}
