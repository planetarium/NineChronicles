using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.Skill;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.Action
{
    [ActionType("combination_equipment")]
    public class CombinationEquipment : GameAction
    {
        public Address AvatarAddress;
        public Address ResultAddress;
        public int RecipeId;
        public int? SubRecipeId;

        public override IAccountStateDelta Execute(IActionContext ctx)
        {
            var states = ctx.PreviousStates;
            if (ctx.Rehearsal)
            {
                return states
                    .SetState(AvatarAddress, MarkChanged)
                    .SetState(ResultAddress, MarkChanged)
                    .SetState(ctx.Signer, MarkChanged);

            }

            if (!states.TryGetAgentAvatarStates(ctx.Signer, AvatarAddress, out var agentState,
                out var avatarState))
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

            if (!avatarState.worldInformation.IsClearedStage(recipe.UnlockStage))
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

            var equipmentMaterial = (Material) ItemFactory.CreateMaterial(materialSheet, material.Id);
            materials[equipmentMaterial] = recipe.MaterialCount;

            var requiredGold = recipe.RequiredGold;
            var requiredActionPoint = recipe.RequiredActionPoint;

            // 장비 제작
            if (!tableSheets.EquipmentItemSheet.TryGetValue(recipe.ResultEquipmentId, out var equipRow))
            {
                return states;
            }

            var equipment = (Equipment) ItemFactory.Create(equipRow, ctx.Random.GenerateRandomGuid());


            // 보조 레시피 검증
            if (!(SubRecipeId is null))
            {
                var subSheet = tableSheets.EquipmentItemSubRecipeSheet;
                if (!subSheet.TryGetValue((int) SubRecipeId, out var subRecipe))
                {
                    return states;
                }

                if (!avatarState.worldInformation.IsClearedStage(subRecipe.UnlockStage))
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

                    var subMaterial = (Material) ItemFactory.CreateMaterial(materialSheet, materialInfo.Id);
                    materials[subMaterial] = materialInfo.Count;

                    requiredGold += subRecipe.RequiredGold;
                    requiredActionPoint += subRecipe.RequiredActionPoint;
                }

                var optionSheet = tableSheets.EquipmentItemOptionSheet;
                foreach (var optionInfo in subRecipe.Options)
                {
                    if (!optionSheet.TryGetValue(optionInfo.Id, out var optionRow))
                    {
                        return states;
                    }

                    if (optionRow.StatType != StatType.NONE)
                    {
                        var statMap = GetStat(optionRow);
                        equipment.StatsMap.AddStatAdditionalValue(statMap.StatType, statMap.Value);
                    }
                    else
                    {
                        var skill = GetSkill(optionRow, tableSheets);
                        if (!(skill is null))
                        {
                            equipment.Skills.Add(skill);
                        }
                    }
                }
            }

            // 자원 검증
            if (agentState.gold < requiredGold || avatarState.actionPoint < requiredActionPoint)
            {
                return states;
            }

            avatarState.actionPoint -= requiredActionPoint;
            agentState.gold -= requiredGold;

            var result = new Combination.ResultModel
            {
                actionPoint = requiredActionPoint,
                gold = requiredGold,
                materials = materials,
                itemUsable = equipment,
            };
            var mail = new CombinationMail(result, ctx.BlockIndex, ctx.Random.GenerateRandomGuid()) {New = false};
            result.id = mail.id;
            avatarState.Update(mail);
            avatarState.UpdateFromCombination(equipment);
            return states
                .SetState(AvatarAddress, avatarState.Serialize())
                .SetState(ResultAddress, result.Serialize())
                .SetState(ctx.Signer, agentState.Serialize());
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["avatarAddress"] = AvatarAddress.Serialize(),
                ["recipeId"] = RecipeId.Serialize(),
                ["subRecipeId"] = SubRecipeId.Serialize(),
                ["resultAddress"] = ResultAddress.Serialize(),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            AvatarAddress = plainValue["avatarAddress"].ToAddress();
            ResultAddress = plainValue["resultAddress"].ToAddress();
            RecipeId = plainValue["recipeId"].ToInteger();
            SubRecipeId = plainValue["subRecipeId"].ToNullableInteger();
        }

        private StatMap GetStat(EquipmentItemOptionSheet.Row row)
        {
            // TODO 랜덤범위 계산 적용
            return new StatMap(row.StatType, row.StatMin);
        }

        private Skill GetSkill(EquipmentItemOptionSheet.Row row, TableSheets tableSheets)
        {
            try
            {
                var skillRow =
                    tableSheets.SkillSheet.OrderedList.First(r => r.Id == row.SkillId);
                var skill = SkillFactory.Get(skillRow, row.SkillChanceMin, row.SkillDamageMin);
                return skill;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }
    }
}
