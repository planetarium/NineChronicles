using System;
using System.Collections.Generic;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    [Serializable]
    public class WeeklyArenaRewardSheet : Sheet<int, WeeklyArenaRewardSheet.Row>
    {
        [Serializable]
        public class RewardData: StageSheet.RewardData
        {
            public int RequiredLevel { get; }
            public RewardData(int itemId, decimal ratio, int min, int max, int requiredLevel) : base(itemId, ratio, min, max)
            {
                RequiredLevel = requiredLevel;
            }
        }

        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => Id;
            public int Id { get; private set; }
            public RewardData Reward { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = ParseInt(fields[0]);
                var itemId = ParseInt(fields[1]);
                Reward = new RewardData(itemId, ParseDecimal(fields[2]), ParseInt(fields[3]),
                    ParseInt(fields[4]), ParseInt(fields[5]));
            }
        }

        public WeeklyArenaRewardSheet() : base(nameof(WeeklyArenaRewardSheet))
        {
        }
    }
}
