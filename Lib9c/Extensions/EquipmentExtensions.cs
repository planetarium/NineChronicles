using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Nekoyume.TableData;

namespace Nekoyume.Extensions
{
    public static class EquipmentExtensions
    {
        public static bool IsMadeWithMimisbrunnrRecipe(
            this Equipment equipment,
            EquipmentItemRecipeSheet recipeSheet,
            EquipmentItemSubRecipeSheetV2 subRecipeSheet,
            EquipmentItemOptionSheet itemOptionSheet)
        {
            if (equipment.MadeWithMimisbrunnrRecipe)
            {
                return true;
            }

            var itemOptionInfo = new ItemOptionInfo(equipment);

            var recipeRow = recipeSheet.OrderedList.FirstOrDefault(row =>
                row.ResultEquipmentId == equipment.Id);
            if (recipeRow == null)
            {
                return false;
            }

            if (recipeRow.SubRecipeIds.Count < 3)
            {
                return false;
            }

            var mimisSubRecipeId = recipeRow.SubRecipeIds[2];

            if (!subRecipeSheet.TryGetValue(mimisSubRecipeId, out var subRecipeRow))
            {
                throw new SheetRowNotFoundException("EquipmentItemSubRecipeSheetV2", mimisSubRecipeId);
            }

            EquipmentItemOptionSheet.Row[] optionRows;

            try
            {
                optionRows = subRecipeRow.Options
                    .Select(option => itemOptionSheet[option.Id])
                    .Where(optionRow => optionRow.StatType == equipment.UniqueStatType)
                    .ToArray();
            }
            catch (KeyNotFoundException e)
            {
                throw new SheetRowNotFoundException("EquipmentItemOptionSheet", e.Message);
            }

            (StatType type, int value, int count) uniqueStatOption;

            try
            {
                uniqueStatOption = itemOptionInfo.StatOptions
                    .First(statOption => statOption.type == equipment.UniqueStatType);
            }
            catch
            {
                return false;
            }

            if (optionRows.Length < uniqueStatOption.count)
            {
                throw new Exception($"optionRows.Length({optionRows.Length}) less than uniqueStatOption.count({uniqueStatOption.count})");
            }

            switch (uniqueStatOption.count)
            {
                case 1:
                    return uniqueStatOption.value >= optionRows[0].StatMin;
                case 2:
                    return uniqueStatOption.value >= optionRows[0].StatMin + optionRows[1].StatMin;
                default:
                    throw new Exception($"Unexpected uniqueStatOption.count({uniqueStatOption.count})");
            }
        }
    }
}
