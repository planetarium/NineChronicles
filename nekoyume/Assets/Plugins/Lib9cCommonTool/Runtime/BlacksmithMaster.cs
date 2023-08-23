#nullable enable

using System;
using System.Linq;
using Lib9c.DevExtensions;
using Libplanet.Action;
using Nekoyume.Action;
using Nekoyume.Game;
using Nekoyume.Model.Item;
using Nekoyume.Model.Skill;
using Nekoyume.Model.Stat;

namespace Lib9cCommonTool.Runtime
{
    public static class BlacksmithMaster
    {
        public static Equipment? CraftEquipment(
            int equipmentId,
            Guid? nonFungibleItemId = null,
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
                nonFungibleItemId ?? random.GenerateRandomGuid(),
                blockIndex);
            if (equipment.Grade == 0)
            {
                return equipment;
            }

            var recipeRow = tableSheets.EquipmentItemRecipeSheet.OrderedList!
                .First(e => e.ResultEquipmentId == equipmentId);
            var subRecipeId = recipeRow.SubRecipeIds[subRecipeIndex];
            var subRecipeRow = tableSheets.EquipmentItemSubRecipeSheetV2[subRecipeId];
            var skillSheet = tableSheets.SkillSheet;
            var enhancementCostSheetV3 = tableSheets.EnhancementCostSheetV3;
            var options = subRecipeRow.Options
                .Select(option => tableSheets.EquipmentItemOptionSheet[option.Id])
                .ToArray();
            foreach (var option in options)
            {
                if (option.StatType == StatType.NONE)
                {
                    var skillRow = skillSheet[option.SkillId];
                    var skill = SkillFactory.Get(
                        skillRow,
                        option.SkillDamageMax,
                        option.SkillChanceMax,
                        option.StatDamageRatioMax,
                        option.ReferencedStatType);
                    equipment.Skills.Add(skill);
                    equipment.optionCountFromCombination++;

                    continue;
                }

                equipment.StatsMap.AddStatAdditionalValue(option.StatType, option.StatMax);
                equipment.optionCountFromCombination++;
            }

            if (level > 0 &&
                ItemEnhancement.TryGetRow(
                    equipment,
                    enhancementCostSheetV3,
                    out var enhancementCostRow))
            {
                equipment.SetLevel(random, level, enhancementCostSheetV3);
            }

            return equipment;
        }
    }
}
