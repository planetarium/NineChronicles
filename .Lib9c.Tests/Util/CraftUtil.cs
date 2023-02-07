namespace Lib9c.Tests.Util
{
    using System.Collections.Generic;
    using System.Linq;
    using Lib9c.Tests.Action;
    using Libplanet.Action;
    using Nekoyume.Model.Item;
    using Nekoyume.TableData;

    public static class CraftUtil
    {
        public static Dictionary<ItemBase, int> GetMaterialsFromCraftInfo(
            TableSheets tableSheets,
            IEnumerable<(int, int?, int, long)> targetRecipeIdList,
            IRandom random = null
        )
        {
            random ??= new TestRandom();
            var materialDict = new Dictionary<ItemBase, int>();

            foreach (var (recipeId, subRecipeId, _, _) in targetRecipeIdList)
            {
                var itemRecipeRow = tableSheets.EquipmentItemRecipeSheet.OrderedList.First(e =>
                    e.Id == recipeId);
                var materialRow = tableSheets.MaterialItemSheet[itemRecipeRow.MaterialId];
                var material = ItemFactory.CreateItem(materialRow, random);
                if (materialDict.ContainsKey(material))
                {
                    materialDict[material] += itemRecipeRow.MaterialCount;
                }
                else
                {
                    materialDict[material] = itemRecipeRow.MaterialCount;
                }

                if (!(subRecipeId is null))
                {
                    var subRow =
                        tableSheets.EquipmentItemSubRecipeSheetV2.OrderedList.First(e =>
                            e.Id == (int)subRecipeId!);
                    foreach (var materialInfo in subRow.Materials)
                    {
                        var subMaterial = ItemFactory.CreateItem(
                            tableSheets.MaterialItemSheet[materialInfo.Id], random);

                        if (materialDict.ContainsKey(subMaterial))
                        {
                            materialDict[subMaterial] += materialInfo.Count;
                        }
                        else
                        {
                            materialDict[subMaterial] = materialInfo.Count;
                        }
                    }
                }
            }

            return materialDict;
        }

        public static List<(int, int?, int, long)> GetEquipmentCraftInfoFromItemId(
            TableSheets tableSheets,
            int targetItemId,
            IRandom random = null)
        {
            return GetEquipmentCraftInfoFromItemId(tableSheets, new[] { targetItemId }, random);
        }

        public static List<(int, int?, int, long)> GetEquipmentCraftInfoFromItemId(
            TableSheets tableSheets,
            IEnumerable<int> targetItemIdList,
            IRandom random = null
        )
        {
            random ??= new TestRandom();
            var equipmentItemSheet = tableSheets.EquipmentItemSheet;

            var recipeIdList = new List<(int, int?, int, long)>();
            foreach (var itemId in targetItemIdList)
            {
                ItemSheet.Row itemRow = equipmentItemSheet.First(e => e.Value.Id == itemId).Value;

                var itemRecipeRow = tableSheets.EquipmentItemRecipeSheet.First(e =>
                    e.Value.ResultEquipmentId == itemRow.Id).Value;
                recipeIdList.Add((itemRecipeRow.Id, itemRecipeRow.SubRecipeIds?[0],
                    itemRecipeRow.UnlockStage, itemRecipeRow.RequiredBlockIndex));
            }

            return recipeIdList;
        }
    }
}
