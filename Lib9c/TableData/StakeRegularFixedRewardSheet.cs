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
    public class StakeRegularFixedRewardSheet : Sheet<int, StakeRegularFixedRewardSheet.Row>, IStakeRewardSheet
    {
        [Serializable]
        public class RewardInfo
        {
            protected bool Equals(RewardInfo other)
            {
                return ItemId == other.ItemId && Count == other.Count;
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
                    return (ItemId * 397) ^ Count;
                }
            }

            public readonly int ItemId;
            public readonly int Count;

            public RewardInfo(params string[] fields)
            {
                ItemId = ParseInt(fields[0]);
                Count = ParseInt(fields[1]);
            }

            public RewardInfo(int itemId, int count)
            {
                ItemId = itemId;
                Count = count;
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

        public StakeRegularFixedRewardSheet() : base(nameof(StakeRegularFixedRewardSheet))
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
    }
}
