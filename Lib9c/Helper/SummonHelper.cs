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
                return summonRow.Recipes[0].Item1;
            }

            if (recipeValue <= summonRow.CumulativeRecipe2Ratio)
            {
                return summonRow.Recipes[1].Item1;
            }

            if (recipeValue <= summonRow.CumulativeRecipe3Ratio)
            {
                return summonRow.Recipes[2].Item1;
            }

            if (recipeValue <= summonRow.CumulativeRecipe4Ratio)
            {
                return summonRow.Recipes[3].Item1;
            }

            if (recipeValue <= summonRow.CumulativeRecipe5Ratio)
            {
                return summonRow.Recipes[4].Item1;
            }

            return summonRow.Recipes[5].Item1;
        }
    }
}
