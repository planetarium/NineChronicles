using System;
using System.Collections.Generic;
using System.Linq;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    [Serializable]
    public class StakeRegularRewardSheet : Sheet<int, StakeRegularRewardSheet.Row>, IStakeRewardSheet
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
            public readonly StakeRewardType Type;

            public RewardInfo(params string[] fields)
            {
                ItemId = ParseInt(fields[0]);
                Rate = ParseInt(fields[1]);
                if (fields.Length > 2)
                {
                    Type = (StakeRewardType) Enum.Parse(typeof(StakeRewardType), fields[2]);
                }
                else
                {
                    Type = StakeRewardType.Item;
                }
            }

            public RewardInfo(int itemId, int rate)
            {
                ItemId = itemId;
                Rate = rate;
            }
        }

        [Serializable]
        public class Row : SheetRow<int>, IStakeRewardRow
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

        public IReadOnlyList<IStakeRewardRow> OrderedRows => OrderedList;

        public enum StakeRewardType
        {
            Item,
            Rune,
        }
    }
}
