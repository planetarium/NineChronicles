#nullable enable

using System.Linq;
using Lib9c.DevExtensions;
using Libplanet.Action;
using Nekoyume.Action;
using Nekoyume.Game;
using Nekoyume.Model.Item;
using Nekoyume.Model.Skill;
using Nekoyume.Model.Stat;

namespace StateViewer.Runtime
{
    public static class BlacksmithMaster
    {
        public static Equipment? CraftEquipment(
            int equipmentId,
            int level = 0,
            int subRecipeIndex = 1,
            TableSheets? tableSheets = null,
            IRandom? random = null,
            long blockIndex = 0L)
        {
            if (equipmentId < 0 ||
                level < 0 ||
                tableSheets is null)
            {
                return null;
            }

            random ??= new RandomImpl();
            var equipmentRow = tableSheets.EquipmentItemSheet[equipmentId];
            var equipment = (Equipment)ItemFactory.CreateItemUsable(
                equipmentRow,
                random.GenerateRandomGuid(),
                blockIndex);
            if (equipment.Grade == 0)
            {
                return equipment;
            }

            var recipeRow = tableSheets.EquipmentItemRecipeSheet.OrderedList!
                .First(e => e.ResultEquipmentId == equipmentId);
            var subRecipeId = recipeRow.SubRecipeIds[subRecipeIndex];
            var subRecipeRow = tableSheets.EquipmentItemSubRecipeSheetV2[subRecipeId];
            var options = subRecipeRow.Options
                .Select(option => tableSheets.EquipmentItemOptionSheet[option.Id])
                .ToArray();
            foreach (var option in options)
            {
                if (option.StatType == StatType.NONE)
                {
                    var skillRow = tableSheets.SkillSheet[option.SkillId];
                    var skill = SkillFactory.Get(
                        skillRow,
                        option.SkillDamageMax,
                        option.SkillChanceMax);
                    equipment.Skills.Add(skill);

                    continue;
                }

                equipment.StatsMap.AddStatAdditionalValue(option.StatType, option.StatMax);
            }

            if (level > 0 &&
                ItemEnhancement.TryGetRow(
                    equipment,
                    tableSheets.EnhancementCostSheetV2,
                    out var enhancementCostRow))
            {
                for (var i = 0; i < level; i++)
                {
                    equipment.LevelUpV2(random, enhancementCostRow, true);
                }
            }

            return equipment;
        }
    }
}
