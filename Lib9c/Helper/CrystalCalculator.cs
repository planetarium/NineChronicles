using System.Collections.Generic;
using Libplanet.Assets;
using Nekoyume.TableData;

namespace Nekoyume.Helper
{
    public static class CrystalCalculator
    {
        public static readonly Currency CRYSTAL = new Currency("CRYSTAL", 18, minters: null);

        public static FungibleAssetValue CalculateCost(IEnumerable<int> recipeIds, EquipmentItemRecipeSheet equipmentItemRecipeSheet)
        {
            var cost = 0 * CRYSTAL;
            foreach (var id in recipeIds)
            {
                EquipmentItemRecipeSheet.Row row = equipmentItemRecipeSheet[id];
                cost += row.CRYSTAL * CRYSTAL;
            }

            return cost;
        }
    }
}
