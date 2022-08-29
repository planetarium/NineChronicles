using System;
using System.Collections.Generic;
using System.Linq;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    public class StakeActionPointCoefficientSheet : Sheet<int, StakeActionPointCoefficientSheet.Row>, IStakeRewardSheet
    {
        [Serializable]
        public class Row : SheetRow<int>, IStakeRewardRow
        {
            public override int Key => Level;

            public int Level { get; private set; }

            public long RequiredGold { get; private set; }

            // percentage.
            public int Coefficient { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                Level = ParseInt(fields[0]);
                RequiredGold = ParseInt(fields[1]);
                Coefficient = ParseInt(fields[2]);
            }
        }

        public IReadOnlyList<IStakeRewardRow> OrderedRows => OrderedList;
    }
}
