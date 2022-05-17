using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Libplanet.Assets;
using Nekoyume.Model.Item;
using Nekoyume.TableData;
using Nekoyume.TableData.Crystal;

namespace Nekoyume.Helper
{
    public static class CrystalCalculator
    {
        public static readonly Currency CRYSTAL = new Currency("CRYSTAL", 18, minters: null);

        public static FungibleAssetValue CalculateRecipeUnlockCost(IEnumerable<int> recipeIds, EquipmentItemRecipeSheet equipmentItemRecipeSheet)
        {
            var cost = 0 * CRYSTAL;

            return recipeIds
                .Select(id => equipmentItemRecipeSheet[id])
                .Aggregate(cost, (current, row) => current + row.CRYSTAL * CRYSTAL);
        }

        public static FungibleAssetValue CalculateWorldUnlockCost(IEnumerable<int> worldIds, WorldUnlockSheet worldUnlockSheet)
        {
            var cost = 0 * CRYSTAL;

            return worldIds
                .Select(id => worldUnlockSheet.OrderedList.First(r => r.WorldIdToUnlock == id))
                .Aggregate(cost, (current, row) => current + row.CRYSTAL * CRYSTAL);
        }

        public static FungibleAssetValue CalculateCrystal(
            IEnumerable<Equipment> equipmentList,
            CrystalEquipmentGrindingSheet crystalEquipmentGrindingSheet,
            int monsterCollectionLevel,
            CrystalMonsterCollectionMultiplierSheet crystalMonsterCollectionMultiplierSheet,
            bool enhancementFailed
        )
        {
            FungibleAssetValue crystal = 0 * CRYSTAL;
            foreach (var equipment in equipmentList)
            {
                CrystalEquipmentGrindingSheet.Row grindingRow = crystalEquipmentGrindingSheet[equipment.Id];
                int level = Math.Max(0, equipment.level - 1);
                crystal += BigInteger.Pow(2, level) * grindingRow.CRYSTAL * CRYSTAL;
            }

            // Divide Reward when itemEnhancement failed.
            if (enhancementFailed)
            {
                crystal = crystal.DivRem(2, out _);
            }

            CrystalMonsterCollectionMultiplierSheet.Row multiplierRow =
                crystalMonsterCollectionMultiplierSheet[monsterCollectionLevel];
            var extra = crystal.DivRem(100, out _) * multiplierRow.Multiplier;
            return crystal + extra;
        }

        /// <param name="materials"> Key : id of material, Value : count of material </param>
        public static FungibleAssetValue CalculateMaterialCost(
            Dictionary<int, int> materials,
            CrystalMaterialCostSheet crystalMaterialCostsheet)
        {
            FungibleAssetValue crystal = 0 * CRYSTAL;
            foreach (var material in materials)
            {
                crystal += CalculateMaterialCost(material.Key, material.Value, crystalMaterialCostsheet);
            }

            return crystal;
        }

        public static FungibleAssetValue CalculateMaterialCost(
            int materialId,
            int materialCount,
            CrystalMaterialCostSheet crystalMaterialCostsheet)
        {
            if (!crystalMaterialCostsheet.TryGetValue(materialId, out var costRow))
            {
                throw new ArgumentException($"This material is not replacable with crystal. id : {materialId}");
            }

            return costRow.CRYSTAL * materialCount * CRYSTAL;
        }
    }
}
