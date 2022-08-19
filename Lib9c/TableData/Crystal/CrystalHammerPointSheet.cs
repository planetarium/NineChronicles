using System.Collections.Generic;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData.Crystal
{
    public class CrystalHammerPointSheet : Sheet<int, CrystalHammerPointSheet.Row>
    {
        public class Row : SheetRow<int>
        {
            public int RecipeId { get; private set; }
            public int MaxPoint { get; private set; }
            public int CRYSTAL { get; private set; }
            public override int Key => RecipeId;

            public override void Set(IReadOnlyList<string> fields)
            {
                RecipeId = ParseInt(fields[0], 0);
                MaxPoint = ParseInt(fields[1], 0);
                CRYSTAL = ParseInt(fields[2], 0);
            }
        }

        public CrystalHammerPointSheet() : base(nameof(CrystalHammerPointSheet))
        {
        }
    }
}
