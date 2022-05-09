using System.Collections.Generic;

namespace Nekoyume.TableData
{
    using static TableExtensions;

    public class SweepRequiredCPSheet : Sheet<int, SweepRequiredCPSheet.Row>
    {
        public class Row : SheetRow<int>
        {
            public override int Key => StageId;
            public int StageId { get; private set; }
            public int RequiredCP { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                StageId = ParseInt(fields[0]);
                RequiredCP = ParseInt(fields[1]);
            }
        }

        public SweepRequiredCPSheet() : base(nameof(SweepRequiredCPSheet))
        {
        }
    }
}
