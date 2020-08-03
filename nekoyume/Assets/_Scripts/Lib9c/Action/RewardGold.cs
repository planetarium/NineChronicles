using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using Bencodex.Types;
using CsvHelper;
using CsvHelper.Configuration.Attributes;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model.State;

namespace Nekoyume.Action
{
    [Serializable]
    public class GoldDistribution : IEquatable<GoldDistribution>
    {
        [Ignore]
        public Address Address;

        [Index(0)]
        public string AddressString
        {
            get => Address.ToHex();
            set => Address = new Address(value);
        }

        [Index(1)]
        public BigInteger AmountPerBlock { get; set; }

        [Index(2)]
        public long StartBlock { get; set; }

        [Index(3)]
        public long EndBlock { get; set; }

        public static GoldDistribution[] LoadInDescendingEndBlockOrder(string csvPath)
        {
            GoldDistribution[] records;
            using (var reader = new StreamReader(csvPath))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                records = csv.GetRecords<GoldDistribution>().ToArray();
            }

            Array.Sort<GoldDistribution>(records, new DescendingEndBlockComparer());
            return records;
        }

        public GoldDistribution()
        {
        }

        public GoldDistribution(IValue serialized)
            : this((Bencodex.Types.Dictionary)serialized)
        {
        }

        public GoldDistribution(Bencodex.Types.Dictionary serialized)
        {
            Address = serialized["addr"].ToAddress();
            AmountPerBlock = serialized["amnt"].ToBigInteger();
            StartBlock = serialized["strt"].ToLong();
            EndBlock = serialized["end"].ToLong();
        }

        public Bencodex.Types.Dictionary Serialize() => Bencodex.Types.Dictionary.Empty
            .Add("addr", Address.Serialize())
            .Add("amnt", AmountPerBlock.Serialize())
            .Add("strt", StartBlock.Serialize())
            .Add("end", EndBlock.Serialize());

        public BigInteger GetAmount(long blockIndex)
        {
            if (StartBlock <= blockIndex && blockIndex <= EndBlock)
            {
                return AmountPerBlock;
            }

            return 0;
        }

        public bool Equals(GoldDistribution other) =>
            Address.Equals(other.Address) &&
            AmountPerBlock.Equals(other.AmountPerBlock) &&
            StartBlock.Equals(other.StartBlock) &&
            EndBlock.Equals(other.EndBlock);

        public override bool Equals(object obj) =>
            obj is GoldDistribution o && Equals(o);

        public override int GetHashCode() =>
            (Address, AmountPerBlock, StartBlock, EndBlock).GetHashCode();

        private class DescendingEndBlockComparer : IComparer<GoldDistribution>
        {
            public int Compare(GoldDistribution x, GoldDistribution y) =>
                y.EndBlock.CompareTo(x.EndBlock);
        }
    }

    [Serializable]
    [ActionType("reward_gold")]
    public class RewardGold : ActionBase
    {
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
            states = WeeklyArenaRankingBoard(context, states);
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
                states = states.TransferAsset(
                    fund,
                    distribution.Address,
                    goldCurrency,
                    amount
                );
            }
            return states;
        }

        public IAccountStateDelta WeeklyArenaRankingBoard(IActionContext ctx, IAccountStateDelta states)
        {
            var index = Math.Max((int) ctx.BlockIndex / GameConfig.WeeklyArenaInterval, 0);
            var weekly = states.GetWeeklyArenaState(index);
            var nextIndex = index + 1;
            var nextWeekly = states.GetWeeklyArenaState(nextIndex);
            if (nextWeekly is null)
            {
                nextWeekly = new WeeklyArenaState(nextIndex);
                states = states.SetState(nextWeekly.address, nextWeekly.Serialize());
            }
            if (ctx.BlockIndex % GameConfig.WeeklyArenaInterval == 0 && index > 0)
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
            else if (ctx.BlockIndex - weekly.ResetIndex >= GameConfig.DailyArenaInterval)
            {
                weekly.ResetCount(ctx.BlockIndex);
                states = states.SetState(weekly.address, weekly.Serialize());
            }

            return states;
        }

        public IAccountStateDelta MinerReward(IActionContext ctx, IAccountStateDelta states)
        {
            // 마이닝 보상
            // https://www.notion.so/planetarium/Mining-Reward-b7024ef463c24ebca40a2623027d497d
            BigInteger defaultMiningReward = 10;
            var countOfHalfLife = Convert.ToInt64(ctx.BlockIndex / 12614400) + 1;
            var miningReward = defaultMiningReward / countOfHalfLife;
            return states.TransferAsset(
                GoldCurrencyState.Address,
                ctx.Miner,
                states.GetGoldCurrency(),
                miningReward
            );
        }
    }
}
