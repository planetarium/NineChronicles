using System.Collections.Generic;

namespace Nekoyume.TableData
{
    public class StageDialogSheet : Sheet<int, StageDialogSheet.Row>
    {
        public class Row : SheetRow<int>
        {
            public override int Key => Id;

            public int Id { get; private set; }
            
            public int StageId { get; private set; }

            public int DialogId { get; private set; }
            
            public override void Set(IReadOnlyList<string> fields)
            {
                Id = int.Parse(fields[0]);
                StageId = int.Parse(fields[1]);
                DialogId = int.Parse(fields[2]);
            }
        }

        public StageDialogSheet() : base(nameof(StageDialogSheet))
        {
        }
    }
}
