using System.Collections.Generic;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData.Crystal
{
    public class CrystalEquipmentGrindingSheet: Sheet<int, CrystalEquipmentGrindingSheet.Row>
    {
        public class Row : SheetRow<int>
        {
            public override int Key => ItemId;
            public int ItemId;
            public int CRYSTAL;

            public override void Set(IReadOnlyList<string> fields)
            {
                ItemId = ParseInt(fields[0]);
                CRYSTAL = ParseInt(fields[1]);
            }
        }

        public CrystalEquipmentGrindingSheet() : base(nameof(CrystalEquipmentGrindingSheet))
        {
        }
    }
}
