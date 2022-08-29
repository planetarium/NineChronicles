using System;
using System.Collections.Generic;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    [Serializable]
    public class StakeActionPointCoefficientSheet : Sheet<int, StakeActionPointCoefficientSheet.Row>, IStakeRewardSheet
    {
        public IReadOnlyList<IStakeRewardRow> OrderedRows => OrderedList;
        public StakeActionPointCoefficientSheet() : base(nameof(StakeActionPointCoefficientSheet)) { }

        protected override void AddRow(int key, Row value)
        {
            if (!TryGetValue(key, out _))
            {
                Add(key, value);
            }
        }

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
    }
}
