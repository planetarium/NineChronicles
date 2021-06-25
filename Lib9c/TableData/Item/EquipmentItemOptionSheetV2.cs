using System.Collections.Generic;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    public class EquipmentItemOptionSheetV2 : Sheet<int, EquipmentItemOptionSheetV2.Row>
    {
        public class Row: EquipmentItemOptionSheet.Row
        {
            public int Grade { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                base.Set(fields);

                Grade = ParseInt(fields[9], 1);
            }
        }

        public EquipmentItemOptionSheetV2() : base(nameof(EquipmentItemOptionSheetV2))
        {
        }
    }
}
