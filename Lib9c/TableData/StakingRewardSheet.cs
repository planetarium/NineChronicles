using System;
using System.Collections.Generic;
using System.Linq;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    [Serializable]
    public class StakingRewardSheet : Sheet<int, StakingRewardSheet.Row>
    {
        [Serializable]
        public class RewardInfo
        {
            public readonly int ItemId;
            public readonly int Quantity;

            public RewardInfo(params string[] fields)
            {
                ItemId = ParseInt(fields[0]);
                Quantity = ParseInt(fields[1]);
            }
        }

        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => StakingLevel;
            public int StakingLevel { get; private set; }
            public List<RewardInfo> Rewards { get; private set; }
            public override void Set(IReadOnlyList<string> fields)
            {
                StakingLevel = ParseInt(fields[0]);
                var info = new RewardInfo(fields.Skip(1).ToArray());
                Rewards = new List<RewardInfo> {info};
            }
        }

        public StakingRewardSheet() : base(nameof(StakingRewardSheet))
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

    }
}
