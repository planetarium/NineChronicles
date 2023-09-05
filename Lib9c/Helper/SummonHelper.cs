using Libplanet.Action;
using Nekoyume.TableData.Summon;

namespace Nekoyume.Helper
{
    public class SummonHelper
    {
        public static int PickAuraSummonRecipe(SummonSheet.Row summonRow, IRandom random)
        {
            var recipeValue = random.Next(1, summonRow.CumulativeRecipe6Ratio + 1);
            if (recipeValue <= summonRow.CumulativeRecipe1Ratio)
            {
                return summonRow.Recipe1;
            }
            else if (recipeValue <= summonRow.CumulativeRecipe2Ratio)
            {
                return summonRow.Recipe2;
            }
            else if (recipeValue <= summonRow.CumulativeRecipe3Ratio)
            {
                return summonRow.Recipe3;
            }
            else if (recipeValue <= summonRow.CumulativeRecipe4Ratio)
            {
                return summonRow.Recipe4;
            }
            else if (recipeValue <= summonRow.CumulativeRecipe5Ratio)
            {
                return summonRow.Recipe5;
            }
            else
            {
                return summonRow.Recipe6;
            }
        }
    }
}
