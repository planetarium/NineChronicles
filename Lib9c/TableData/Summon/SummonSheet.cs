using System.Collections.Generic;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData.Summon
{
    public class SummonSheet : Sheet<int, SummonSheet.Row>
    {
        public class Row : SheetRow<int>
        {
            public override int Key => GroupId;

            public int GroupId { get; private set; }
            public int CostMaterial { get; private set; }
            public int CostMaterialCount { get; private set; }
            public int CostNcg { get; private set; }

            public readonly List<(int, int)> Recipes = new List<(int, int)>();
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
                // Min. Two recipes are necessary
                Recipes.Add((ParseInt(fields[4]), ParseInt(fields[5])));
                CumulativeRecipe1Ratio = ParseInt(fields[5]);
                Recipes.Add((ParseInt(fields[6]), ParseInt(fields[7])));
                CumulativeRecipe2Ratio = CumulativeRecipe1Ratio + ParseInt(fields[7]);

                // Recipe3 ~ 6 are optional
                if (TryParseInt(fields[8], out _) && TryParseInt(fields[9], out _))
                {
                    Recipes.Add((ParseInt(fields[8]), ParseInt(fields[9])));
                    CumulativeRecipe3Ratio = CumulativeRecipe2Ratio + ParseInt(fields[9]);
                }
                else
                {
                    CumulativeRecipe3Ratio = CumulativeRecipe2Ratio;
                }

                if (TryParseInt(fields[10], out _) && TryParseInt(fields[11], out _))
                {
                    Recipes.Add((ParseInt(fields[10]), ParseInt(fields[11])));
                    CumulativeRecipe4Ratio = CumulativeRecipe3Ratio + ParseInt(fields[11]);
                }
                else
                {
                    CumulativeRecipe4Ratio = CumulativeRecipe3Ratio;
                }

                if (TryParseInt(fields[12], out _) && TryParseInt(fields[13], out _))
                {
                    Recipes.Add((ParseInt(fields[12]), ParseInt(fields[13])));
                    CumulativeRecipe5Ratio = CumulativeRecipe4Ratio + ParseInt(fields[13]);
                }
                else
                {
                    CumulativeRecipe5Ratio = CumulativeRecipe4Ratio;
                }
                if (TryParseInt(fields[14], out _) && TryParseInt(fields[15], out _))
                {
                    Recipes.Add((ParseInt(fields[14]), ParseInt(fields[15])));
                    CumulativeRecipe6Ratio = CumulativeRecipe5Ratio + ParseInt(fields[15]);
                }
                else
                {
                    CumulativeRecipe6Ratio = CumulativeRecipe5Ratio;
                }
            }
        }

        public SummonSheet() : base(nameof(SummonSheet))
        {
        }
    }
}
