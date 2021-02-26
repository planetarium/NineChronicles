using System;
using System.Collections.Generic;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    [Serializable]
    public class StageDialogSheet : Sheet<int, StageDialogSheet.Row>
    {
        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => Id;

            public int Id { get; private set; }
            
            public int StageId { get; private set; }

            public int DialogId { get; private set; }
            
            public override void Set(IReadOnlyList<string> fields)
            {
                Id = ParseInt(fields[0]);
                StageId = ParseInt(fields[1]);
                DialogId = ParseInt(fields[2]);
            }
        }

        public StageDialogSheet() : base(nameof(StageDialogSheet))
        {
        }
    }
}
