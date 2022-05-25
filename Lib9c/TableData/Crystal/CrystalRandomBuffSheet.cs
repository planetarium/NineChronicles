using System;
using System.Collections.Generic;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData.Crystal
{
    [Serializable]
    public class CrystalRandomBuffSheet : Sheet<int, CrystalRandomBuffSheet.Row>
    {
        public class Row : SheetRow<int>
        {
            public override int Key => BuffId;
            public int BuffId;
            public int Rank;
            public decimal Ratio;
            public override void Set(IReadOnlyList<string> fields)
            {
                BuffId = ParseInt(fields[0]);
                Rank = ParseInt(fields[1]);
                Ratio = ParseDecimal(fields[2]);
            }
        }

        public CrystalRandomBuffSheet() : base(nameof(CrystalRandomBuffSheet))
        {
        }
    }
}
