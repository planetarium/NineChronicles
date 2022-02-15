using System.Collections.Generic;
using Nekoyume.Battle;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Nekoyume.TableData;

namespace Nekoyume.Helper
{
    using System.Linq;
    public class ItemOptionInfo
    {
        public readonly int OptionCountFromCombination;

        public readonly (StatType type, int baseValue, int totalValue) MainStat;

        public readonly List<(StatType type, int value, int count)> StatOptions
            = new List<(StatType type, int value, int count)>();

        public readonly List<(SkillSheet.Row skillRow, int power, int chance)> SkillOptions
            = new List<(SkillSheet.Row skillRow, int power, int chance)>();

        public readonly int CP;

        public ItemOptionInfo(Equipment equipment)
        {
            var additionalStats = equipment.StatsMap.GetAdditionalStats(true).ToList();

            OptionCountFromCombination = equipment.optionCountFromCombination > 0
                ? equipment.optionCountFromCombination
                : additionalStats.Count + equipment.Skills.Count;

            MainStat = (
                equipment.UniqueStatType,
                equipment.StatsMap.GetStat(equipment.UniqueStatType, true),
                equipment.StatsMap.GetStat(equipment.UniqueStatType));

            var optionCountDiff = OptionCountFromCombination - (additionalStats.Count + equipment.Skills.Count);
            foreach (var (statType, additionalValue) in additionalStats)
            {
                if (statType == MainStat.type &&
                    optionCountDiff > 0)
                {
                    StatOptions.Add((statType, additionalValue, 1 + optionCountDiff));
                    continue;
                }

                StatOptions.Add((statType, additionalValue, 1));
            }

            foreach (var skill in equipment.Skills)
            {
                SkillOptions.Add((
                    skill.SkillRow,
                    skill.Power,
                    skill.Chance));
            }

            CP = CPHelper.GetCP(equipment);
        }

        public ItemOptionInfo(ItemUsable itemUsable)
        {
            MainStat = (StatType.NONE, 0, 0);

            var stats = itemUsable.StatsMap.GetStats(true).ToList();
            for (var i = 0; i < stats.Count; i++)
            {
                var (statType, value) = stats[i];
                StatOptions.Add((statType, value, 1));
            }

            CP = CPHelper.GetCP(itemUsable);

            OptionCountFromCombination = stats.Count;
        }
    }

    public static class ItemOptionHelper
    {
        public static bool TryGet(ItemUsable itemUsable, out ItemOptionInfo itemOptionInfo)
        {
            switch (itemUsable)
            {
                default:
                    itemOptionInfo = null;
                    break;
                case Equipment equipment:
                    itemOptionInfo = new ItemOptionInfo(equipment);
                    break;
                case Consumable consumable:
                    itemOptionInfo = new ItemOptionInfo(consumable);
                    break;
            }

            return itemOptionInfo != null;
        }

        public static List<EquipmentItemOptionSheet.Row> GetStatOptionRows(
            EquipmentItemSubRecipeSheetV2.Row subRecipeRow,
            EquipmentItemOptionSheet itemOptionSheet,
            ItemUsable itemUsable)
        {
            var recipeOptionTuples = subRecipeRow.Options
                .Select(info =>
                {
                    var optionRow = itemOptionSheet
                        .OrderedList
                        .FirstOrDefault(firstOptionRow => firstOptionRow.Id == info.Id);
                    return (optionRow, info.RequiredBlockIndex);
                })
                .ToList();
            if (recipeOptionTuples.Count != subRecipeRow.Options.Count)
            {
                // Debug.LogError(
                //     $"Failed to create optionRows with subRecipeRow.Options. Sub recipe id: {subRecipeId}");
                return new List<EquipmentItemOptionSheet.Row>();
            }

            if (!(itemUsable is Equipment equipment))
            {
                return recipeOptionTuples
                    .Select(tuple => tuple.optionRow)
                    .Where(row => row.StatType != StatType.NONE)
                    .ToList();
            }

            var result = new List<EquipmentItemOptionSheet.Row>();
            var optionInfo = new ItemOptionInfo(equipment);
            foreach (var (statType, value, count) in optionInfo.StatOptions)
            {
                if (statType == optionInfo.MainStat.type && count > 1)
                {
                    var mainStatOptions = recipeOptionTuples
                        .Where(tuple => tuple.optionRow.StatType == statType)
                        .ToList();
                    if (mainStatOptions.Count != count)
                    {
                        // Debug.LogError(
                        //     $"[{nameof(ItemOptionHelper)}]Unexpected case. mainStatOptions.Count({mainStatOptions.Count}) != count({count})");
                    }

                    foreach (var (optionRow, _) in mainStatOptions)
                    {
                        result.Add(optionRow);
                    }

                    continue;
                }

                foreach (var (optionRow, _) in recipeOptionTuples)
                {
                    if (optionRow.StatType != statType ||
                        optionRow.StatMin > value ||
                        optionRow.StatMax < value)
                    {
                        continue;
                    }

                    result.Add(optionRow);

                    break;
                }
            }

            return result;
        }

        public static List<EquipmentItemOptionSheet.Row> GetStatOptionRows(
            int subRecipeId,
            ItemUsable itemUsable,
            EquipmentItemSubRecipeSheetV2 subRecipeSheet,
            EquipmentItemOptionSheet itemOptionSheet)
        {
            var subRecipeRow = subRecipeSheet.OrderedList
                .FirstOrDefault(e => e.Id == subRecipeId);
            if (subRecipeRow is null)
            {
                //Debug.LogError($"subRecipeRow is null. {subRecipeId}");
                return new List<EquipmentItemOptionSheet.Row>();
            }

            return GetStatOptionRows(subRecipeRow, itemOptionSheet, itemUsable);
        }
    }
}
