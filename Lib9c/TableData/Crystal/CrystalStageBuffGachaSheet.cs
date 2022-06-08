using System;
using System.Collections.Generic;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData.Crystal
{
    [Serializable]
    public class CrystalStageBuffGachaSheet : Sheet<int, CrystalStageBuffGachaSheet.Row>
    {
        public class Row : SheetRow<int>
        {
            public override int Key => StageId;
            public int StageId;
            public int MaxStar;
            public int NormalCost;
            public int AdvancedCost;

            public override void Set(IReadOnlyList<string> fields)
            {
                StageId = ParseInt(fields[0]);
                MaxStar = ParseInt(fields[1]);
                NormalCost = ParseInt(fields[2]);
                AdvancedCost = ParseInt(fields[3]);
            }
        }

        public CrystalStageBuffGachaSheet() : base(nameof(CrystalStageBuffGachaSheet))
        {
        }
    }
}
