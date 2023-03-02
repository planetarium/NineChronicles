using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.Skill;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionObsolete(ActionObsoleteConfig.V100080ObsoleteIndex)]
    [ActionType("combination_equipment7")]
    public class CombinationEquipment7 : GameAction, ICombinationEquipmentV1
    {
        public static readonly Address BlacksmithAddress = ItemEnhancement9.BlacksmithAddress;

        public Address AvatarAddress;
        public int RecipeId;
        public int SlotIndex;
        public int? SubRecipeId;

        Address ICombinationEquipmentV1.AvatarAddress => AvatarAddress;
        int ICombinationEquipmentV1.RecipeId => RecipeId;
        int ICombinationEquipmentV1.SlotIndex => SlotIndex;
        int? ICombinationEquipmentV1.SubRecipeId => SubRecipeId;

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
            var inventoryAddress = AvatarAddress.Derive(LegacyInventoryKey);
            var worldInformationAddress = AvatarAddress.Derive(LegacyWorldInformationKey);
            var questListAddress = AvatarAddress.Derive(LegacyQuestListKey);
            if (ctx.Rehearsal)
            {
                return states
                    .SetState(AvatarAddress, MarkChanged)
                    .SetState(slotAddress, MarkChanged)
                    .SetState(ctx.Signer, MarkChanged)
                    .SetState(inventoryAddress, MarkChanged)
                    .SetState(worldInformationAddress, MarkChanged)
                    .SetState(questListAddress, MarkChanged)
                    .MarkBalanceChanged(GoldCurrencyMock, ctx.Signer, BlacksmithAddress);
            }

            CheckObsolete(ActionObsoleteConfig.V100080ObsoleteIndex, context);

            var addressesHex = GetSignerAndOtherAddressesHex(context, AvatarAddress);

            if (!states.TryGetAgentAvatarStatesV2(ctx.Signer, AvatarAddress, out var agentState,
                out var avatarState, out _))
            {
                throw new FailedLoadStateException($"{addressesHex}Aborted as the avatar state of the signer was failed to load.");
            }

            var slotState = states.GetCombinationSlotState(AvatarAddress, SlotIndex);
            if (slotState is null)
            {
                throw new FailedLoadStateException($"{addressesHex}Aborted as the slot state is failed to load");
            }

            if (!slotState.Validate(avatarState, ctx.BlockIndex))
            {
                throw new CombinationSlotUnlockException(
                    $"{addressesHex}Aborted as the slot state is invalid: {slotState} @ {SlotIndex}");
            }

            var recipeSheet = states.GetSheet<EquipmentItemRecipeSheet>();
            var materialSheet = states.GetSheet<MaterialItemSheet>();
            var materials = new Dictionary<Material, int>();

            // Validate recipe.
            if (!recipeSheet.TryGetValue(RecipeId, out var recipe))
            {
                throw new SheetRowNotFoundException(addressesHex, nameof(EquipmentItemRecipeSheet), RecipeId);
            }

            if (!(SubRecipeId is null))
            {
                if (!recipe.SubRecipeIds.Contains((int) SubRecipeId))
                {
                    throw new SheetRowColumnException(
                        $"{addressesHex}Aborted as the sub recipe {SubRecipeId} was failed to load from the sheet."
                    );
                }
            }

            // Validate main recipe is unlocked.
            if (!avatarState.worldInformation.IsStageCleared(recipe.UnlockStage))
            {
                avatarState.worldInformation.TryGetLastClearedStageId(out var current);
                throw new NotEnoughClearedStageLevelException(addressesHex, recipe.UnlockStage, current);
            }

            if (!materialSheet.TryGetValue(recipe.MaterialId, out var material))
            {
                throw new SheetRowNotFoundException(addressesHex, nameof(MaterialItemSheet), recipe.MaterialId);
            }

            if (!avatarState.inventory.RemoveFungibleItem(material.ItemId, context.BlockIndex, recipe.MaterialCount))
            {
                throw new NotEnoughMaterialException(
                    $"{addressesHex}Aborted as the player has no enough material ({material} * {recipe.MaterialCount}). BlockIndex({context.BlockIndex})"
                );
            }

            var equipmentMaterial = ItemFactory.CreateMaterial(materialSheet, material.Id);
            materials[equipmentMaterial] = recipe.MaterialCount;

            BigInteger requiredGold = recipe.RequiredGold;
            var requiredActionPoint = recipe.RequiredActionPoint;
            var equipmentItemSheet = states.GetSheet<EquipmentItemSheet>();

            // Validate equipment id.
            if (!equipmentItemSheet.TryGetValue(recipe.ResultEquipmentId, out var equipRow))
            {
                throw new SheetRowNotFoundException(addressesHex, nameof(equipmentItemSheet), recipe.ResultEquipmentId);
            }

            var requiredBlockIndex = ctx.BlockIndex + recipe.RequiredBlockIndex;
            var equipment = (Equipment) ItemFactory.CreateItemUsable(
                equipRow,
                ctx.Random.GenerateRandomGuid(),
                requiredBlockIndex
            );

            // Validate sub recipe.
            HashSet<int> optionIds = null;
            if (SubRecipeId.HasValue)
            {
                var subSheet = states.GetSheet<EquipmentItemSubRecipeSheet>();
                var subId = (int) SubRecipeId;
                if (!subSheet.TryGetValue(subId, out var subRecipe))
                {
                    throw new SheetRowNotFoundException(addressesHex, nameof(EquipmentItemSubRecipeSheet), subId);
                }

                requiredBlockIndex += subRecipe.RequiredBlockIndex;
                requiredGold += subRecipe.RequiredGold;
                requiredActionPoint += subRecipe.RequiredActionPoint;

                foreach (var materialInfo in subRecipe.Materials)
                {
                    if (!materialSheet.TryGetValue(materialInfo.Id, out var subMaterialRow))
                    {
                        throw new SheetRowNotFoundException(addressesHex, nameof(MaterialItemSheet), materialInfo.Id);
                    }

                    if (!avatarState.inventory.RemoveFungibleItem(
                        subMaterialRow.ItemId,
                        context.BlockIndex,
                        materialInfo.Count))
                    {
                        throw new NotEnoughMaterialException(
                            $"{addressesHex}Aborted as the player has no enough material ({subMaterialRow} * {materialInfo.Count}). BlockIndex({context.BlockIndex})"
                        );
                    }

                    var subMaterial = ItemFactory.CreateMaterial(materialSheet, materialInfo.Id);
                    materials[subMaterial] = materialInfo.Count;
                }

                optionIds = CombinationEquipment4.SelectOption(states.GetSheet<EquipmentItemOptionSheet>(), states.GetSheet<SkillSheet>(),
                    subRecipe, ctx.Random, equipment);
                equipment.Update(requiredBlockIndex);
            }

            // Validate NCG.
            FungibleAssetValue agentBalance = states.GetBalance(ctx.Signer, states.GetGoldCurrency());
            if (agentBalance < states.GetGoldCurrency() * requiredGold)
            {
                throw new InsufficientBalanceException(
                    $"{addressesHex}Aborted as the agent ({ctx.Signer}) has no sufficient gold: {agentBalance} < {requiredGold}",
                    ctx.Signer,
                    agentBalance
                );
            }

            if (avatarState.actionPoint < requiredActionPoint)
            {
                throw new NotEnoughActionPointException(
                    $"{addressesHex}Aborted due to insufficient action point: {avatarState.actionPoint} < {requiredActionPoint}"
                );
            }

            avatarState.actionPoint -= requiredActionPoint;
            if (!(optionIds is null))
            {
                foreach (var id in optionIds.OrderBy(id => id))
                {
                    agentState.unlockedOptions.Add(id);
                }
            }

            // FIXME: BlacksmithAddress just accumulate NCG. we need plan how to circulate this.
            if (requiredGold > 0)
            {
                states = states.TransferAsset(
                    ctx.Signer,
                    BlacksmithAddress,
                    states.GetGoldCurrency() * requiredGold
                );
            }

            var result = new CombinationConsumable5.ResultModel
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
            avatarState.UpdateQuestRewards(materialSheet);
            return states
                .SetState(AvatarAddress, avatarState.SerializeV2())
                .SetState(inventoryAddress, avatarState.inventory.Serialize())
                .SetState(worldInformationAddress, avatarState.worldInformation.Serialize())
                .SetState(questListAddress, avatarState.questList.Serialize())
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

        public static StatMap GetStat(EquipmentItemOptionSheet.Row row, IRandom random)
        {
            var value = random.Next(row.StatMin, row.StatMax + 1);
            return new StatMap(row.StatType, value);
        }

        public static Skill GetSkill(EquipmentItemOptionSheet.Row row, SkillSheet skillSheet,
            IRandom random)
        {
            try
            {
                var skillRow = skillSheet.OrderedList.First(r => r.Id == row.SkillId);
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
    }
}
