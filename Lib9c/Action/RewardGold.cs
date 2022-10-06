using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Serilog;

namespace Nekoyume.Action
{
    /// <summary>
    /// Introduced at Initial commit(2e645be18a4e2caea031c347f00777fbad5dbcc6)
    /// Updated at many pull requests
    /// Updated at https://github.com/planetarium/lib9c/pull/1135
    /// </summary>
    [Serializable]
    public class RewardGold : ActionBase
    {
        // Start filtering inactivate ArenaInfo
        // https://github.com/planetarium/lib9c/issues/946
        public const long FilterInactiveArenaInfoBlockIndex = 3_976_000L;
        public override IValue PlainValue =>
            new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
            {
            });

        public override void LoadPlainValue(IValue plainValue)
        {
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            states = GenesisGoldDistribution(context, states);
            var addressesHex = GetSignerAndOtherAddressesHex(context, context.Signer);
            var started = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}RewardGold exec started", addressesHex);

            // Avoid InvalidBlockStateRootHashException before table patch.
            var arenaSheetAddress = Addresses.GetSheetAddress<ArenaSheet>();
            // Avoid InvalidBlockStateRootHashException in unit test genesis block evaluate.
            if (states.GetState(arenaSheetAddress) is null || context.BlockIndex == 0)
            {
                states = WeeklyArenaRankingBoard2(context, states);
            }

            var ended = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}RewardGold Total Executed Time: {Elapsed}", addressesHex, ended - started);
            return MinerReward(context, states);
        }

        public IAccountStateDelta GenesisGoldDistribution(IActionContext ctx, IAccountStateDelta states)
        {
            IEnumerable<GoldDistribution> goldDistributions = states.GetGoldDistribution();
            var index = ctx.BlockIndex;
            Currency goldCurrency = states.GetGoldCurrency();
            Address fund = GoldCurrencyState.Address;
            foreach(GoldDistribution distribution in goldDistributions)
            {
                BigInteger amount = distribution.GetAmount(index);
                if (amount <= 0) continue;

                // We should divide by 100 for only mainnet distributions.
                // See also: https://github.com/planetarium/lib9c/pull/170#issuecomment-713380172
                FungibleAssetValue fav = goldCurrency * amount;
                var testAddresses = new HashSet<Address>(
                    new []
                    {
                        new Address("F9A15F870701268Bd7bBeA6502eB15F4997f32f9"),
                        new Address("Fb90278C67f9b266eA309E6AE8463042f5461449"),
                    }
                );
                if (!testAddresses.Contains(distribution.Address))
                {
                    fav = fav.DivRem(100, out FungibleAssetValue _);
                }
                states = states.TransferAsset(
                    fund,
                    distribution.Address,
                    fav
                );
            }
            return states;
        }

        [Obsolete("Use WeeklyArenaRankingBoard2 for performance.")]
        public IAccountStateDelta WeeklyArenaRankingBoard(IActionContext ctx, IAccountStateDelta states)
        {
            var gameConfigState = states.GetGameConfigState();
            var index = Math.Max((int) ctx.BlockIndex / gameConfigState.WeeklyArenaInterval, 0);
            var weekly = states.GetWeeklyArenaState(index);
            var nextIndex = index + 1;
            var nextWeekly = states.GetWeeklyArenaState(nextIndex);
            if (nextWeekly is null)
            {
                nextWeekly = new WeeklyArenaState(nextIndex);
                states = states.SetState(nextWeekly.address, nextWeekly.Serialize());
            }

            // Beginning block of a new weekly arena.
            if (ctx.BlockIndex % gameConfigState.WeeklyArenaInterval == 0 && index > 0)
            {
                var prevWeekly = states.GetWeeklyArenaState(index - 1);
                if (!prevWeekly.Ended)
                {
                    prevWeekly.End();
                    weekly.Update(prevWeekly, ctx.BlockIndex);
                    states = states.SetState(prevWeekly.address, prevWeekly.Serialize());
                    states = states.SetState(weekly.address, weekly.Serialize());
                }
            }
            else if (ctx.BlockIndex - weekly.ResetIndex >= gameConfigState.DailyArenaInterval)
            {
                weekly.ResetCount(ctx.BlockIndex);
                states = states.SetState(weekly.address, weekly.Serialize());
            }
            return states;
        }

        public IAccountStateDelta WeeklyArenaRankingBoard2(IActionContext ctx, IAccountStateDelta states)
        {
            states = PrepareNextArena(ctx, states);
            states = ResetChallengeCount(ctx, states);
            return states;
        }

        public IAccountStateDelta PrepareNextArena(IActionContext ctx, IAccountStateDelta states)
        {
            var gameConfigState = states.GetGameConfigState();
            var index = Math.Max((int) ctx.BlockIndex / gameConfigState.WeeklyArenaInterval, 0);
            var weeklyAddress = WeeklyArenaState.DeriveAddress(index);
            var rawWeekly = (Dictionary) states.GetState(weeklyAddress);
            var nextIndex = index + 1;
            var nextWeekly = states.GetWeeklyArenaState(nextIndex);
            if (nextWeekly is null)
            {
                nextWeekly = new WeeklyArenaState(nextIndex);
                states = states.SetState(nextWeekly.address, nextWeekly.Serialize());
            }

            // Beginning block of a new weekly arena.
            if (ctx.BlockIndex % gameConfigState.WeeklyArenaInterval == 0 && index > 0)
            {
                var prevWeeklyAddress = WeeklyArenaState.DeriveAddress(index - 1);
                var rawPrevWeekly = (Dictionary) states.GetState(prevWeeklyAddress);
                if (!rawPrevWeekly["ended"].ToBoolean())
                {
                    rawPrevWeekly = rawPrevWeekly.SetItem("ended", true.Serialize());
                    var weekly = new WeeklyArenaState(rawWeekly);
                    var prevWeekly = new WeeklyArenaState(rawPrevWeekly);
                    var listAddress = weekly.address.Derive("address_list");
                    // Set ArenaInfo, address list for new RankingBattle.
                    var addressList = states.TryGetState(listAddress, out List rawList)
                        ? rawList.ToList(StateExtensions.ToAddress)
                        : new List<Address>();
                    var nextAddresses = rawList ?? List.Empty;
                    if (ctx.BlockIndex >= RankingBattle11.UpdateTargetBlockIndex)
                    {
                        weekly.ResetIndex = ctx.BlockIndex;

                        // Copy Map to address list.
                        if (ctx.BlockIndex == RankingBattle11.UpdateTargetBlockIndex)
                        {
                            foreach (var kv in prevWeekly.Map)
                            {
                                var address = kv.Key;
                                var lazyInfo = kv.Value;
                                var info = new ArenaInfo(lazyInfo.State);
                                states = states.SetState(
                                    weeklyAddress.Derive(address.ToByteArray()), info.Serialize());
                                if (!addressList.Contains(address))
                                {
                                    nextAddresses = nextAddresses.Add(address.Serialize());
                                }
                            }
                        }
                        else
                        {
                            bool filterInactive =
                                ctx.BlockIndex >= FilterInactiveArenaInfoBlockIndex;
                            // Copy addresses from prev weekly address list.
                            var prevListAddress = prevWeekly.address.Derive("address_list");

                            if (states.TryGetState(prevListAddress, out List prevRawList))
                            {
                                var prevList = prevRawList.ToList(StateExtensions.ToAddress);
                                foreach (var address in prevList.Where(address => !addressList.Contains(address)))
                                {
                                    addressList.Add(address);
                                }
                            }

                            // Copy activated ArenaInfo from prev ArenaInfo.
                            foreach (var address in addressList)
                            {
                                if (states.TryGetState(
                                        prevWeekly.address.Derive(address.ToByteArray()),
                                        out Dictionary rawInfo))
                                {
                                    var prevInfo = new ArenaInfo(rawInfo);
                                    var record = prevInfo.ArenaRecord;
                                    // Filter ArenaInfo
                                    if (filterInactive && record.Win == 0 && record.Draw == 0 &&
                                        record.Lose == 0)
                                    {
                                        continue;
                                    }

                                    nextAddresses = nextAddresses.Add(address.Serialize());
                                    states = states.SetState(
                                        weeklyAddress.Derive(address.ToByteArray()),
                                        new ArenaInfo(prevInfo).Serialize());
                                }
                            }
                        }
                        // Set address list.
                        states = states.SetState(listAddress, nextAddresses);
                    }
                    // Run legacy Update.
                    else
                    {
                        weekly.Update(prevWeekly, ctx.BlockIndex);
                    }

                    states = states.SetState(prevWeeklyAddress, rawPrevWeekly);
                    states = states.SetState(weeklyAddress, weekly.Serialize());
                }
            }
            return states;
        }

        public IAccountStateDelta ResetChallengeCount(IActionContext ctx, IAccountStateDelta states)
        {
            var gameConfigState = states.GetGameConfigState();
            var index = Math.Max((int) ctx.BlockIndex / gameConfigState.WeeklyArenaInterval, 0);
            var weeklyAddress = WeeklyArenaState.DeriveAddress(index);
            var rawWeekly = (Dictionary) states.GetState(weeklyAddress);
            var resetIndex = rawWeekly["resetIndex"].ToLong();

            if (ctx.BlockIndex - resetIndex >= gameConfigState.DailyArenaInterval)
            {
                var weekly = new WeeklyArenaState(rawWeekly);
                if (resetIndex >= RankingBattle11.UpdateTargetBlockIndex)
                {
                    // Reset count each ArenaInfo.
                    weekly.ResetIndex = ctx.BlockIndex;
                    var listAddress = weeklyAddress.Derive("address_list");
                    if (states.TryGetState(listAddress, out List rawList))
                    {
                        var addressList = rawList.ToList(StateExtensions.ToAddress);
                        foreach (var address in addressList)
                        {
                            var infoAddress = weeklyAddress.Derive(address.ToByteArray());
                            if (states.TryGetState(infoAddress, out Dictionary rawInfo))
                            {
                                var info = new ArenaInfo(rawInfo);
                                info.ResetCount();
                                states = states.SetState(infoAddress, info.Serialize());
                            }
                        }
                    }
                }
                else
                {
                    // Run legacy ResetCount.
                    weekly.ResetCount(ctx.BlockIndex);
                }
                states = states.SetState(weeklyAddress, weekly.Serialize());
            }
            return states;
        }

        public IAccountStateDelta MinerReward(IActionContext ctx, IAccountStateDelta states)
        {
            // 마이닝 보상
            // https://www.notion.so/planetarium/Mining-Reward-b7024ef463c24ebca40a2623027d497d
            Currency currency = states.GetGoldCurrency();
            FungibleAssetValue defaultMiningReward = currency * 10;
            var countOfHalfLife = (int)Math.Pow(2, Convert.ToInt64((ctx.BlockIndex - 1) / 12614400));
            FungibleAssetValue miningReward =
                defaultMiningReward.DivRem(countOfHalfLife, out FungibleAssetValue _);

            if (miningReward >= FungibleAssetValue.Parse(currency, "1.25"))
            {
                states = states.TransferAsset(
                    GoldCurrencyState.Address,
                    ctx.Miner,
                    miningReward
                );
            }

            return states;
        }
    }
}
