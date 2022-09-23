using System.Collections.Generic;
using System.Linq;
using Nekoyume.Battle;
using Nekoyume.Helper;
using Nekoyume.Model.Elemental;
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
            if (equipment.ElementalType != ElementalType.Fire)
            {
                return false;
            }

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
            if (!optionRows.Any())
            {
                return IsMadeWithSpecificMimisbrunnrRecipe(equipment, itemOptionInfo, false);
            }

            (StatType type, int value, int count) uniqueStatOption;
            try
            {
                uniqueStatOption = itemOptionInfo.StatOptions
                    .First(statOption => statOption.type == equipment.UniqueStatType);
            }
            catch
            {
                return IsMadeWithSpecificMimisbrunnrRecipe(equipment, itemOptionInfo, true);
            }

            if (optionRows.Length < uniqueStatOption.count)
            {
                return IsMadeWithSpecificMimisbrunnrRecipe(equipment, itemOptionInfo, true);
            }

            switch (uniqueStatOption.count)
            {
                case 1 when uniqueStatOption.value >= optionRows[0].StatMin:
                case 2 when uniqueStatOption.value >= optionRows[0].StatMin + optionRows[1].StatMin:
                    return true;
            }

            return IsMadeWithSpecificMimisbrunnrRecipe(equipment, itemOptionInfo, true);
        }

        /// <summary>
        /// Unfortunately we cannot throw any exception here. Sheet data has changed for a long times and will be.
        /// And here return false but `equipment` could be made with mimisbrunner recipe.
        /// old: Old mimisbrunnr: Combined by the CombinationEquipment action before release the NineChronicles with this
        /// PR: https://github.com/planetarium/NineChronicles/pull/542
        /// And this PR is not only one which should consider.
        /// </summary>
        private static bool IsMadeWithSpecificMimisbrunnrRecipe(
            Equipment equipment,
            ItemOptionInfo itemOptionInfo,
            bool maybeOld)
        {
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
                // current.
                case 10510000 when itemOptionInfo.StatOptions.Any(e => e.type == StatType.HIT):
                    return true;
                case 10511000:
                    // old.
                    if (maybeOld && itemOptionInfo.StatOptions.Any(e => e.type == StatType.DEF))
                    {
                        return true;
                    }

                    // current.
                    if (itemOptionInfo.StatOptions.Any(e => e.type == StatType.ATK))
                    {
                        var value = itemOptionInfo.StatOptions
                            .Where(e => e.type == StatType.ATK)
                            .Sum(e => e.value);
                        return value >= 179; // 179: stat_min of `EquipmentItemOptionSheet(id: 1152)`
                    }

                    // old or current.
                    if (itemOptionInfo.SkillOptions.Any(e => e.skillRow.Id == 110005))
                    {
                        var tuple = itemOptionInfo.SkillOptions.First(e => e.skillRow.Id == 110005);
                        return tuple.power >= 5080; // 5080: skill_damage_min of `EquipmentItemOptionSheet(id: 1155)`
                    }

                    return false;
                case 10513000:
                    // current.
                    if (itemOptionInfo.StatOptions.Any(e => e.type == StatType.HP))
                    {
                        var value = itemOptionInfo.StatOptions
                            .Where(e => e.type == StatType.HP)
                            .Sum(e => e.value);
                        return value >= 2657; // 2657: stat_min of `EquipmentItemOptionSheet(id: 1174)`
                    }

                    return false;
                case 10514000:
                    // current.
                    if (itemOptionInfo.StatOptions.Any(e => e.type == StatType.SPD))
                    {
                        var value = itemOptionInfo.StatOptions
                            .Where(e => e.type == StatType.SPD)
                            .Sum(e => e.value);
                        return value >= 1503; // 1503: stat_min of `EquipmentItemOptionSheet(id: 1185)`
                    }

                    return false;
                case 10520000:
                    // current.
                    if (itemOptionInfo.StatOptions.Any(e => e.type == StatType.HIT))
                    {
                        var value = itemOptionInfo.StatOptions
                            .Where(e => e.type == StatType.HIT)
                            .Sum(e => e.value);
                        return value >= 348; // 348: stat_min of `EquipmentItemOptionSheet(id: 1691)`
                    }

                    return false;
                case 10521000:
                    // current.
                    if (itemOptionInfo.StatOptions.Any(e => e.type == StatType.ATK))
                    {
                        var value = itemOptionInfo.StatOptions
                            .Where(e => e.type == StatType.ATK)
                            .Sum(e => e.value);
                        return value >= 97; // 97: stat_min of `EquipmentItemOptionSheet(id: 1196)`
                    }

                    return false;
                case 10523000:
                    // current.
                    if (itemOptionInfo.StatOptions.Any(e => e.type == StatType.HP))
                    {
                        var value = itemOptionInfo.StatOptions
                            .Where(e => e.type == StatType.HP)
                            .Sum(e => e.value);
                        return value >= 7373; // 7373: stat_min of `EquipmentItemOptionSheet(id: 1218)`
                    }

                    return false;
                case 10524000:
                    // current.
                    if (itemOptionInfo.StatOptions.Any(e => e.type == StatType.SPD))
                    {
                        var value = itemOptionInfo.StatOptions
                            .Where(e => e.type == StatType.SPD)
                            .Sum(e => e.value);
                        return value >= 2662; // 2662: stat_min of `EquipmentItemOptionSheet(id: 1229)`
                    }

                    return false;
                case 10530000:
                    // current.
                    if (itemOptionInfo.StatOptions.Any(e => e.type == StatType.HIT))
                    {
                        var value = itemOptionInfo.StatOptions
                            .Where(e => e.type == StatType.HIT)
                            .Sum(e => e.value);
                        return value >= 955; // 955: stat_min of `EquipmentItemOptionSheet(id: 1702)`
                    }

                    return false;
                case 10531000:
                    // current.
                    if (itemOptionInfo.StatOptions.Any(e => e.type == StatType.ATK))
                    {
                        var value = itemOptionInfo.StatOptions
                            .Where(e => e.type == StatType.ATK)
                            .Sum(e => e.value);
                        return value >= 260; // 260: stat_min of `EquipmentItemOptionSheet(id: 1240)`
                    }

                    return false;
                case 10533000:
                    // current.
                    if (itemOptionInfo.StatOptions.Any(e => e.type == StatType.HP))
                    {
                        var value = itemOptionInfo.StatOptions
                            .Where(e => e.type == StatType.HP)
                            .Sum(e => e.value);
                        return value >= 15132; // 15132: stat_min of `EquipmentItemOptionSheet(id: 1262)`
                    }

                    return false;
                case 10534000:
                    // current.
                    if (itemOptionInfo.StatOptions.Any(e => e.type == StatType.SPD))
                    {
                        var value = itemOptionInfo.StatOptions
                            .Where(e => e.type == StatType.SPD)
                            .Sum(e => e.value);
                        return value >= 3801; // 3801: stat_min of `EquipmentItemOptionSheet(id: 1273)`
                    }

                    return false;
                default:
                    return false;
            }
        }

        public static int GetRequirementLevel(this Equipment equipment,
            ItemRequirementSheet requirementSheet,
            EquipmentItemRecipeSheet recipeSheet,
            EquipmentItemSubRecipeSheetV2 subRecipeSheet,
            EquipmentItemOptionSheet itemOptionSheet)
        {
            if (!requirementSheet.TryGetValue(equipment.Id, out var row))
            {
                throw new SheetRowNotFoundException(nameof(ItemRequirementSheet), equipment.Id);
            }

            var isMadeWithMimisbrunnrRecipe = equipment.IsMadeWithMimisbrunnrRecipe(
                recipeSheet,
                subRecipeSheet,
                itemOptionSheet
            );
            
            return isMadeWithMimisbrunnrRecipe ? row.MimisLevel : row.Level;
        }
    }
}
