using System.Collections.Generic;
using Nekoyume.Model.EnumType;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    public class RuneListSheet : Sheet<int, RuneListSheet.Row>
    {
        public class Row : SheetRow<int>
        {
            public int Id;
            public int DisplayIndex;
            public int Grade;
            public int RuneType;
            public int RequiredLevel;
            public int UsePlace;

            public override int Key => Id;
            public override void Set(IReadOnlyList<string> fields)
            {
                Id = ParseInt(fields[0]);
                DisplayIndex = ParseInt(fields[1]);
                Grade = ParseInt(fields[2]);
                RuneType = ParseInt(fields[3]);
                RequiredLevel = ParseInt(fields[4]);
                UsePlace = ParseInt(fields[5]);
            }
        }

        public RuneListSheet() : base(nameof(RuneListSheet))
        {
        }
    }
}
