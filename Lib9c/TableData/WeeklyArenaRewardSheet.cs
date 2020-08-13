using System;
using System.Collections.Generic;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    [Serializable]
    public class WeeklyArenaRewardSheet : Sheet<int, WeeklyArenaRewardSheet.Row>
    {
        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => Id;
            public int Id { get; private set; }
            public StageSheet.RewardData Reward { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = ParseInt(fields[0]);
                var itemId = ParseInt(fields[1]);
                Reward = new StageSheet.RewardData(itemId, ParseDecimal(fields[2]), ParseInt(fields[3]),
                    ParseInt(fields[4]));
            }
        }

        public WeeklyArenaRewardSheet() : base(nameof(WeeklyArenaRewardSheet))
        {
        }
    }
}
