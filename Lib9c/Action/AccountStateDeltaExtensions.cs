using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using Bencodex.Types;
using Lib9c;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Helper;
using Nekoyume.Model.Coupons;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    public static class AccountStateDeltaExtensions
    {
        public static IAccount SetWorldBossKillReward(
            this IAccount states,
            IActionContext context,
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
                        states = states.MintAsset(context, agentAddress, reward);
                    }
                    else
                    {
                        states = states.MintAsset(context, avatarAddress, reward);
                    }
                }
            }

            return states.SetState(rewardInfoAddress, rewardRecord.Serialize());
        }

#nullable enable
        public static IAccount SetCouponWallet(
            this IAccount states,
            Address agentAddress,
            IImmutableDictionary<Guid, Coupon> couponWallet)
        {
            Address walletAddress = agentAddress.Derive(CouponWalletKey);
            IValue serializedWallet = new Bencodex.Types.List(
                couponWallet.Values.OrderBy(c => c.Id).Select(v => v.Serialize())
            );
            return states.SetState(walletAddress, serializedWallet);
        }
#nullable disable

        public static IAccount Mead(
            this IAccount states, IActionContext context, Address signer, BigInteger rawValue)
        {
            while (true)
            {
                var price = rawValue * Currencies.Mead;
                var balance = states.GetBalance(signer, Currencies.Mead);
                if (balance < price)
                {
                    var requiredMead = price - balance;
                    var contractAddress = signer.Derive(nameof(RequestPledge));
                    if (states.GetState(contractAddress) is List contract && contract[1].ToBoolean())
                    {
                        var patron = contract[0].ToAddress();
                        try
                        {
                            states = states.TransferAsset(context, patron, signer, requiredMead);
                        }
                        catch (InsufficientBalanceException)
                        {
                            states = states.Mead(context, patron, rawValue);
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
