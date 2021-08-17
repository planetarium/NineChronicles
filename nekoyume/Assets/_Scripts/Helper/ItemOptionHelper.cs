using System.Collections.Generic;
using System.Linq;
using Nekoyume.Battle;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Nekoyume.UI;

namespace Nekoyume.Helper
{
    public class ItemOptionInfo
    {
        public readonly int OptionCountFromCombination;

        public readonly (StatType type, int baseValue, int totalValue) MainStat;

        public readonly List<(StatType type, int value, int count)> StatOptions
            = new List<(StatType type, int value, int count)>();

        public readonly List<(string name, int power, int chance)> SkillOptions
            = new List<(string name, int power, int chance)>();

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
                    skill.SkillRow.GetLocalizedName(),
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
    }
}
