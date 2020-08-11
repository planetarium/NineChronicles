using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Numerics;
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

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("combination_equipment")]
    public class CombinationEquipment : GameAction
    {
        public static readonly Address BlacksmithAddress = ItemEnhancement.BlacksmithAddress;

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
                    .SetState(ctx.Signer, MarkChanged)
                    .MarkBalanceChanged(GoldCurrencyMock, ctx.Signer, BlacksmithAddress);
            }

            if (!states.TryGetAgentAvatarStates(ctx.Signer, AvatarAddress, out var agentState,
                out var avatarState))
            {
                return LogError(context, "Aborted as the avatar state of the signer was failed to load.");
            }

            var slotState = states.GetCombinationSlotState(AvatarAddress, SlotIndex);
            if (slotState is null || !(slotState.Validate(avatarState, ctx.BlockIndex)))
            {
                return LogError(
                    context,
                    "Aborted as the slot state is failed to load or invalid: {@SlotState} @ {SlotIndex}",
                    slotState,
                    SlotIndex
                );
            }

            var tableSheets = TableSheets.FromActionContext(ctx);
            var recipeSheet = tableSheets.EquipmentItemRecipeSheet;
            var materialSheet = tableSheets.MaterialItemSheet;
            var materials = new Dictionary<Material, int>();

            // 레시피 검증
            if (!recipeSheet.TryGetValue(RecipeId, out var recipe))
            {
                return LogError(
                    context,
                    "Aborted as the recipe {RecipeId} was failed to load from the sheet.",
                    RecipeId
                );
            }

            if (!(SubRecipeId is null))
            {
                if (!recipe.SubRecipeIds.Contains((int) SubRecipeId))
                {
                    return LogError(
                        context,
                        "Aborted as the subrecipe {SubRecipeId} was failed to load from the sheet.",
                        SubRecipeId
                    );
                }
            }

            // 메인 레시피 해금 검사.
            if (!avatarState.worldInformation.IsStageCleared(recipe.UnlockStage))
            {
                return LogError(
                    context,
                    "Aborted as the signer is not cleared the minimum stage level required to use the recipe {@Recipe} yet.",
                    recipe
                );
            }

            if (!materialSheet.TryGetValue(recipe.MaterialId, out var material))
            {
                return LogError(
                    context,
                    "Aborted as the material {MaterialId} was failed to load from the sheet.",
                    recipe.MaterialId
                );
            }

            if (!avatarState.inventory.RemoveMaterial(material.ItemId, recipe.MaterialCount))
            {
                return LogError(
                    context,
                    "Aborted as the player has no enough material ({Material} * {Quantity})",
                    material,
                    recipe.MaterialCount
                );
            }

            var equipmentMaterial = ItemFactory.CreateMaterial(materialSheet, material.Id);
            materials[equipmentMaterial] = recipe.MaterialCount;

            BigInteger requiredGold = recipe.RequiredGold;
            var requiredActionPoint = recipe.RequiredActionPoint;

            // 장비 제작
            if (!tableSheets.EquipmentItemSheet.TryGetValue(recipe.ResultEquipmentId, out var equipRow))
            {
                return LogError(
                    context,
                    "Aborted as the equipment item {EquipmentId} was failed to load from the sheet.",
                    recipe.ResultEquipmentId
                );
            }

            var requiredBlockIndex = ctx.BlockIndex + recipe.RequiredBlockIndex;
            var equipment = (Equipment) ItemFactory.CreateItemUsable(
                equipRow,
                ctx.Random.GenerateRandomGuid(),
                requiredBlockIndex
            );

            // 서브 레시피 검증
            HashSet<int> optionIds = null;
            if (SubRecipeId.HasValue)
            {
                var subSheet = tableSheets.EquipmentItemSubRecipeSheet;
                if (!subSheet.TryGetValue((int) SubRecipeId, out var subRecipe))
                {
                    return LogError(
                        context,
                        "Aborted as the subrecipe {SubRecipeId} was failed to load from the subsheet.",
                        SubRecipeId
                    );
                }

                requiredBlockIndex += subRecipe.RequiredBlockIndex;
                requiredGold += subRecipe.RequiredGold;
                requiredActionPoint += subRecipe.RequiredActionPoint;

                foreach (var materialInfo in subRecipe.Materials)
                {
                    if (!materialSheet.TryGetValue(materialInfo.Id, out var subMaterialRow))
                    {
                        return LogError(
                            context,
                            "Aborted as the meterial info {MaterialInfoId} was failed to load from the submaterial sheet.",
                            materialInfo.Id
                        );
                    }

                    if (!avatarState.inventory.RemoveMaterial(subMaterialRow.ItemId,
                        materialInfo.Count))
                    {
                        return LogError(
                            context,
                            "Aborted as the player has no enough material ({Material} * {Quantity})",
                            subMaterialRow,
                            materialInfo.Count
                        );
                    }

                    var subMaterial = ItemFactory.CreateMaterial(materialSheet, materialInfo.Id);
                    materials[subMaterial] = materialInfo.Count;
                }

                optionIds = SelectOption(tableSheets, subRecipe, ctx.Random, equipment);
                equipment.Update(requiredBlockIndex);
            }

            // 자원 검증
            BigInteger agentBalance = states.GetBalance(ctx.Signer, states.GetGoldCurrency());
            if (agentBalance < requiredGold || avatarState.actionPoint < requiredActionPoint)
            {
                return LogError(
                    context,
                    "Aborted due to insufficient action point: {ActionPointBalance} < {ActionCost}",
                    avatarState.actionPoint,
                    requiredActionPoint
                );
            }

            avatarState.actionPoint -= requiredActionPoint;
            if (!(optionIds is null))
            {
                foreach (var id in optionIds)
                {
                    agentState.unlockedOptions.Add(id);
                }
            }

            // FIXME: BlacksmithAddress 계좌로 돈이 쌓이기만 하는데 이걸 어떻게 순환시킬지 기획이 필요.
            if (requiredGold > 0)
            {
                states = states.TransferAsset(ctx.Signer, BlacksmithAddress, states.GetGoldCurrency(), requiredGold);
            }

            var result = new CombinationConsumable.ResultModel
            {
                actionPoint = requiredActionPoint,
                gold = requiredGold,
                materials = materials,
                itemUsable = equipment,
                recipeId = RecipeId,
                subRecipeId = SubRecipeId,
                itemType = ItemType.Equipment,
            };
            slotState.Update(result, ctx.BlockIndex, requiredBlockIndex);
            var mail = new CombinationMail(result, ctx.BlockIndex, ctx.Random.GenerateRandomGuid(),
                requiredBlockIndex);
            result.id = mail.id;
            avatarState.Update(mail);
            avatarState.questList.UpdateCombinationEquipmentQuest(RecipeId);
            avatarState.UpdateFromCombination(equipment);
            avatarState.UpdateQuestRewards(ctx);
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

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
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

        private static Skill GetSkill(EquipmentItemOptionSheet.Row row, TableSheets tableSheets,
            IRandom random)
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

            foreach (var optionInfo in subRecipe.Options)
            {
                if (!optionSheet.TryGetValue(optionInfo.Id, out var optionRow))
                {
                    continue;
                }

                optionSelector.Add(optionRow, optionInfo.Ratio);
            }

            IEnumerable<EquipmentItemOptionSheet.Row> optionRows =
                new EquipmentItemOptionSheet.Row[0];
            try
            {
                optionRows = optionSelector.Select(subRecipe.MaxOptionLimit);
            }
            catch (Exception e) when (
                e is InvalidCountException ||
                e is ListEmptyException
            )
            {
                return optionIds;
            }
            finally
            {
                foreach (var optionRow in optionRows)
                {
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
            }

            return optionIds;
        }
    }
}
