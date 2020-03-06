using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Battle;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.Skill;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Serilog;

namespace Nekoyume.Action
{
    [ActionType("combination_equipment")]
    public class CombinationEquipment : GameAction
    {
        public Address AvatarAddress;
        public int RecipeId;
        public int SlotIndex;
        public int? SubRecipeId;

        public override IAccountStateDelta Execute(IActionContext context)
        {
            IActionContext ctx = context;
            var states = ctx.PreviousStates;
            var slotAddress = AvatarAddress.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CombinationSlotState.DeriveFormat,
                    SlotIndex
                )
            );
            if (ctx.Rehearsal)
            {
                return states
                    .SetState(AvatarAddress, MarkChanged)
                    .SetState(slotAddress, MarkChanged)
                    .SetState(ctx.Signer, MarkChanged);

            }

            if (!states.TryGetAgentAvatarStates(ctx.Signer, AvatarAddress, out var agentState,
                out var avatarState))
            {
                return states;
            }

            var slotState = states.GetCombinationSlotState(AvatarAddress, SlotIndex);
            if (slotState is null || !(slotState.Validate(avatarState, ctx.BlockIndex)))
            {
                return states;
            }

            var tableSheets = TableSheets.FromActionContext(ctx);
            var recipeSheet = tableSheets.EquipmentItemRecipeSheet;
            var materialSheet = tableSheets.MaterialItemSheet;
            var materials = new Dictionary<Material, int>();

            // 레시피 검증
            if (!recipeSheet.TryGetValue(RecipeId, out var recipe))
            {
                return states;
            }

            if (!(SubRecipeId is null))
            {
                if (!recipe.SubRecipeIds.Contains((int) SubRecipeId))
                {
                    return states;
                }
            }

            if (!avatarState.worldInformation.IsStageCleared(recipe.UnlockStage))
            {
                return states;
            }

            if (!materialSheet.TryGetValue(recipe.MaterialId, out var material))
            {
                return states;
            }

            if (!avatarState.inventory.RemoveFungibleItem(material.ItemId, recipe.MaterialCount))
            {
                return states;
            }

            var equipmentMaterial = ItemFactory.CreateMaterial(materialSheet, material.Id);
            materials[equipmentMaterial] = recipe.MaterialCount;

            var requiredGold = recipe.RequiredGold;
            var requiredActionPoint = recipe.RequiredActionPoint;

            // 장비 제작
            if (!tableSheets.EquipmentItemSheet.TryGetValue(recipe.ResultEquipmentId, out var equipRow))
            {
                return states;
            }

            var equipment = (Equipment) ItemFactory.CreateItemUsable(
                equipRow, ctx.Random.GenerateRandomGuid(), ctx.BlockIndex + recipe.RequiredBlockIndex);


            // 보조 레시피 검증
            HashSet<int> optionIds = null;
            if (!(SubRecipeId is null))
            {
                var subSheet = tableSheets.EquipmentItemSubRecipeSheet;
                if (!subSheet.TryGetValue((int) SubRecipeId, out var subRecipe))
                {
                    return states;
                }

                if (!avatarState.worldInformation.IsStageCleared(subRecipe.UnlockStage))
                {
                    return states;
                }

                foreach (var materialInfo in subRecipe.Materials)
                {
                    if (!materialSheet.TryGetValue(materialInfo.Id, out var subMaterialRow))
                    {
                        return states;
                    }

                    if (!avatarState.inventory.RemoveFungibleItem(subMaterialRow.ItemId, materialInfo.Count))
                    {
                        return states;
                    }

                    var subMaterial = ItemFactory.CreateMaterial(materialSheet, materialInfo.Id);
                    materials[subMaterial] = materialInfo.Count;

                    requiredGold += subRecipe.RequiredGold;
                    requiredActionPoint += subRecipe.RequiredActionPoint;
                }

                optionIds = SelectOption(tableSheets, subRecipe, ctx.Random, equipment);
            }

            // 자원 검증
            if (agentState.gold < requiredGold || avatarState.actionPoint < requiredActionPoint)
            {
                return states;
            }

            avatarState.actionPoint -= requiredActionPoint;
            agentState.gold -= requiredGold;
            if (!(optionIds is null))
            {
                foreach (var id in optionIds)
                {
                    agentState.unlockedOptions.Add(id);
                }
            }

            var result = new Combination.ResultModel
            {
                actionPoint = requiredActionPoint,
                gold = requiredGold,
                materials = materials,
                itemUsable = equipment,
            };
            slotState.Update(result, ctx.BlockIndex + recipe.RequiredBlockIndex);
            var mail = new CombinationMail(result, ctx.BlockIndex, ctx.Random.GenerateRandomGuid()) {New = false};
            result.id = mail.id;
            avatarState.Update(mail);
            avatarState.UpdateFromCombination(equipment);
            return states
                .SetState(AvatarAddress, avatarState.Serialize())
                .SetState(slotAddress, slotState.Serialize())
                .SetState(ctx.Signer, agentState.Serialize());
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["avatarAddress"] = AvatarAddress.Serialize(),
                ["recipeId"] = RecipeId.Serialize(),
                ["subRecipeId"] = SubRecipeId.Serialize(),
                ["slotIndex"] = SlotIndex.Serialize(),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            AvatarAddress = plainValue["avatarAddress"].ToAddress();
            RecipeId = plainValue["recipeId"].ToInteger();
            SubRecipeId = plainValue["subRecipeId"].ToNullableInteger();
            SlotIndex = plainValue["slotIndex"].ToInteger();
        }

        private static StatMap GetStat(EquipmentItemOptionSheet.Row row, IRandom random)
        {
            var value = random.Next(row.StatMin, row.StatMax + 1);
            return new StatMap(row.StatType, value);
        }

        private static Skill GetSkill(EquipmentItemOptionSheet.Row row, TableSheets tableSheets, IRandom random)
        {
            try
            {
                var skillRow =
                    tableSheets.SkillSheet.OrderedList.First(r => r.Id == row.SkillId);
                var dmg = random.Next(row.SkillDamageMin, row.SkillDamageMax + 1);
                var chance = random.Next(row.SkillChanceMin, row.SkillChanceMax + 1);
                var skill = SkillFactory.Get(skillRow, dmg, chance);
                return skill;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        public static HashSet<int> SelectOption(
            TableSheets tableSheets,
            EquipmentItemSubRecipeSheet.Row subRecipe,
            IRandom random,
            Equipment equipment
        )
        {
            var optionSheet = tableSheets.EquipmentItemOptionSheet;
            var optionSelector = new WeightedSelector<EquipmentItemOptionSheet.Row>(random);
            var optionIds = new HashSet<int>();

            if (subRecipe.MaxOptionLimit <= 0)
            {
                return optionIds;
            }

            while (!optionIds.Any())
            {

                if (optionSelector.Count == 0)
                {
                    foreach (var optionInfo in subRecipe.Options)
                    {
                        if (!optionSheet.TryGetValue(optionInfo.Id, out var optionRow) || optionInfo.Ratio <= 0m)
                        {
                            continue;
                        }

                        optionSelector.Add(optionRow, optionInfo.Ratio);
                    }
                }

                if (optionSelector.Count == 0)
                {
                    break;
                }

                for (var i = 0; i < subRecipe.MaxOptionLimit; i++)
                {
                    try
                    {
                        var optionRow = optionSelector.Pop();
                        if (optionRow.StatType != StatType.NONE)
                        {
                            var statMap = GetStat(optionRow, random);
                            equipment.StatsMap.AddStatAdditionalValue(statMap.StatType, statMap.Value);
                        }
                        else
                        {
                            var skill = GetSkill(optionRow, tableSheets, random);
                            if (!(skill is null))
                            {
                                equipment.Skills.Add(skill);
                            }
                        }
                        optionIds.Add(optionRow.Id);
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        // 확률굴림에 실패
                        Log.Debug("option select failed.");
                    }
                }
            }

            return optionIds;
        }
    }
}
