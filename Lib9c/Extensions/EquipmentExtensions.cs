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

            var itemOptionInfo = new ItemOptionInfo(equipment);

            // Check old mimisbrunnr
            // Old mimisbrunnr: Combined by the CombinationEquipment action before release the NineChronicles with this
            // PR: https://github.com/planetarium/NineChronicles/pull/542
            // And this PR is not only one which should consider.
            switch (equipment.Id)
            {
                case 10111000 when itemOptionInfo.SkillOptions.Any(e =>
                    e.skillRow.Id == 110001 ||
                    e.skillRow.Id == 110005):
                case 10211000 when itemOptionInfo.StatOptions.Any(e => e.type == StatType.SPD):
                case 10321000 when itemOptionInfo.StatOptions.Any(e =>
                    e.type == StatType.ATK ||
                    e.type == StatType.CRI):
                case 10411000 when itemOptionInfo.StatOptions.Any(e => e.type == StatType.ATK):
                case 10511000 when itemOptionInfo.StatOptions.Any(e => e.type == StatType.DEF):
                    return true;
            }
            // ~Check old mimisbrunnr

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
                // NOTE: Unfortunately we cannot throw any exception here. Sheet data has changed for a long times and will be.
                // And here return false but `equipment` could be `mimisbrunner`.
                return false;
            }

            switch (uniqueStatOption.count)
            {
                case 1 when uniqueStatOption.value >= optionRows[0].StatMin:
                case 2 when uniqueStatOption.value >= optionRows[0].StatMin + optionRows[1].StatMin:
                    return true;
            }

            return false;
        }
    }
}
