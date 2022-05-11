using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet.Assets;
using Nekoyume.Model.State;
using static Nekoyume.TableData.TableExtensions;
using static Lib9c.SerializeKeys;

namespace Nekoyume.TableData
{
    [Serializable]
    public class StakeRegularRewardSheet : Sheet<int, StakeRegularRewardSheet.Row>
    {
        [Serializable]
        public class RewardInfo
        {
            protected bool Equals(RewardInfo other)
            {
                return ItemId == other.ItemId && Rate == other.Rate;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((RewardInfo) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (ItemId * 397) ^ Rate;
                }
            }

            public readonly int ItemId;
            public readonly int Rate;

            public RewardInfo(params string[] fields)
            {
                ItemId = ParseInt(fields[0]);
                Rate = ParseInt(fields[1]);
            }

            public RewardInfo(int itemId, int rate)
            {
                ItemId = itemId;
                Rate = rate;
            }

            public RewardInfo(Dictionary dictionary)
            {
                ItemId = dictionary[IdKey].ToInteger();
                Rate = dictionary[RateKey].ToInteger();
            }
            public IValue Serialize()
            {
                return Dictionary.Empty
                    .Add(IdKey, ItemId.Serialize())
                    .Add(RateKey, Rate.Serialize());
            }
        }

        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => Level;

            public int Level { get; private set; }

            public long RequiredGold { get; private set; }

            public List<RewardInfo> Rewards { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                Level = ParseInt(fields[0]);
                RequiredGold = ParseInt(fields[1]);
                var info = new RewardInfo(fields.Skip(2).ToArray());
                Rewards = new List<RewardInfo> {info};
            }
        }

        public StakeRegularRewardSheet() : base(nameof(StakeRegularRewardSheet))
        {
        }

        protected override void AddRow(int key, Row value)
        {
            if (!TryGetValue(key, out var row))
            {
                Add(key, value);

                return;
            }

            if (!value.Rewards.Any())
            {
                return;
            }

            row.Rewards.Add(value.Rewards[0]);
        }

        public int FindLevelByStakedAmount(FungibleAssetValue balance)
        {
            var orderedRows = Values.OrderBy(row => row.RequiredGold).ToList();
            for (int i = 0; i < orderedRows.Count - 1; ++i)
            {
                if (balance.Currency * orderedRows[i].RequiredGold < balance &&
                    balance < balance.Currency * orderedRows[i + 1].RequiredGold)
                {
                    return orderedRows[i].Level;
                }
            }

            return orderedRows.Last().Level;
        }
    }
}
