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
            public int CRYSTAL;
            public override void Set(IReadOnlyList<string> fields)
            {
                StageId = ParseInt(fields[0]);
                MaxStar = ParseInt(fields[1]);
                CRYSTAL = ParseInt(fields[2]);
            }
        }

        public CrystalStageBuffGachaSheet() : base(nameof(CrystalStageBuffGachaSheet))
        {
        }
    }
}
