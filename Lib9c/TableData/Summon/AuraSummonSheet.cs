using System.Collections.Generic;
using Bencodex.Types;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData.Summon
{
    public class AuraSummonSheet : Sheet<int, AuraSummonSheet.Row>
    {
        public class Row : SheetRow<int>
        {
            public override int Key => GroupId;

            public int GroupId { get; private set; }
            public int CostMaterial { get; private set; }
            public int CostMaterialCount { get; private set; }
            public int CostNcg { get; private set; }
            public int Recipe1 { get; private set; }
            public int Recipe2 { get; private set; }
            public int Recipe3 { get; private set; }
            public int Recipe4 { get; private set; }
            public int Recipe5 { get; private set; }
            public int Recipe6 { get; private set; }
            public int Recipe1Ratio { get; private set; }
            public int Recipe2Ratio { get; private set; }
            public int Recipe3Ratio { get; private set; }
            public int Recipe4Ratio { get; private set; }
            public int Recipe5Ratio { get; private set; }
            public int Recipe6Ratio { get; private set; }

            // For convenience
            public int CumulativeRecipe1Ratio { get; private set; }
            public int CumulativeRecipe2Ratio { get; private set; }
            public int CumulativeRecipe3Ratio { get; private set; }
            public int CumulativeRecipe4Ratio { get; private set; }
            public int CumulativeRecipe5Ratio { get; private set; }
            public int CumulativeRecipe6Ratio { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                GroupId = ParseInt(fields[0]);
                CostMaterial = ParseInt(fields[1]);
                CostMaterialCount = ParseInt(fields[2]);
                CostNcg = ParseInt(fields[3]);
                // WARNING: Be aware of fields index!!
                Recipe1 = ParseInt(fields[4]);
                Recipe2 = ParseInt(fields[6]);
                Recipe3 = TryParseInt(fields[8], out _) ? ParseInt(fields[8]) : 0;
                Recipe4 = TryParseInt(fields[10], out _) ? ParseInt(fields[10]) : 0;
                Recipe5 = TryParseInt(fields[12], out _) ? ParseInt(fields[12]) : 0;
                Recipe6 = TryParseInt(fields[14], out _) ? ParseInt(fields[14]) : 0;
                Recipe1Ratio = ParseInt(fields[5]);
                Recipe2Ratio = ParseInt(fields[7]);
                Recipe3Ratio = TryParseInt(fields[9], out _) ? ParseInt(fields[9]) : 0;
                Recipe4Ratio = TryParseInt(fields[11], out _) ? ParseInt(fields[11]) : 0;
                Recipe5Ratio = TryParseInt(fields[13], out _) ? ParseInt(fields[13]) : 0;
                Recipe6Ratio = TryParseInt(fields[15], out _) ? ParseInt(fields[15]) : 0;

                SetCumulativeRatio();
            }

            private void SetCumulativeRatio()
            {
                CumulativeRecipe1Ratio = Recipe1Ratio;
                CumulativeRecipe2Ratio = CumulativeRecipe1Ratio + Recipe2Ratio;
                CumulativeRecipe3Ratio = CumulativeRecipe2Ratio + Recipe3Ratio;
                CumulativeRecipe4Ratio = CumulativeRecipe3Ratio + Recipe4Ratio;
                CumulativeRecipe5Ratio = CumulativeRecipe4Ratio + Recipe5Ratio;
                CumulativeRecipe6Ratio = CumulativeRecipe5Ratio + Recipe6Ratio;
            }
        }

        public AuraSummonSheet() : base(nameof(AuraSummonSheet))
        {
        }
    }
}
