using System;
using System.Collections.Generic;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData.Crystal
{
    [Serializable]
    public class CrystalMaterialCostSheet: Sheet<int, CrystalMaterialCostSheet.Row>
    {
        public class Row : SheetRow<int>
        {
            public int ItemId { get; private set; }
            public int CRYSTAL { get; private set; }
            public override int Key => ItemId;

            public override void Set(IReadOnlyList<string> fields)
            {
                ItemId = ParseInt(fields[0]);
                CRYSTAL = ParseInt(fields[1]);
            }
        }

        public CrystalMaterialCostSheet() : base(nameof(CrystalMaterialCostSheet))
        {
        }
    }
}
