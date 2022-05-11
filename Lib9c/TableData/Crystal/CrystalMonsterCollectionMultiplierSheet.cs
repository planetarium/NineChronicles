using System;
using System.Collections.Generic;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData.Crystal
{
    [Serializable]
    public class CrystalMonsterCollectionMultiplierSheet : Sheet<int, CrystalMonsterCollectionMultiplierSheet.Row>
    {
        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => Level;
            public int Level;
            public int Multiplier;
            public override void Set(IReadOnlyList<string> fields)
            {
                Level = ParseInt(fields[0]);
                Multiplier = ParseInt(fields[1]);
            }
        }

        public CrystalMonsterCollectionMultiplierSheet() : base(
            nameof(CrystalMonsterCollectionMultiplierSheet))
        {
        }
    }
}
