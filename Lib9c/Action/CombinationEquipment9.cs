using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("combination_equipment9")]
    public class CombinationEquipment9 : GameAction, ICombinationEquipmentV1
    {
        public static readonly Address BlacksmithAddress = ItemEnhancement9.BlacksmithAddress;

        public const string AvatarAddressKey = "a";
        public Address avatarAddress;

        public const string SlotIndexKey = "s";
        public int slotIndex;

        public const string RecipeIdKey = "r";
        public int recipeId;

        public const string SubRecipeIdKey = "i";
        public int? subRecipeId;

        Address ICombinationEquipmentV1.AvatarAddress => avatarAddress;
        int ICombinationEquipmentV1.RecipeId => recipeId;
        int ICombinationEquipmentV1.SlotIndex => slotIndex;
        int? ICombinationEquipmentV1.SubRecipeId => subRecipeId;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                [AvatarAddressKey] = avatarAddress.Serialize(),
                [SlotIndexKey] = slotIndex.Serialize(),
                [RecipeIdKey] = recipeId.Serialize(),
                [SubRecipeIdKey] = subRecipeId.Serialize(),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            avatarAddress = plainValue[AvatarAddressKey].ToAddress();
            slotIndex = plainValue[SlotIndexKey].ToInteger();
            recipeId = plainValue[RecipeIdKey].ToInteger();
            subRecipeId = plainValue[SubRecipeIdKey].ToNullableInteger();
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            var slotAddress = avatarAddress.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CombinationSlotState.DeriveFormat,
                    slotIndex
                )
            );
            var inventoryAddress = avatarAddress.Derive(LegacyInventoryKey);
            var worldInformationAddress = avatarAddress.Derive(LegacyWorldInformationKey);
            var questListAddress = avatarAddress.Derive(LegacyQuestListKey);
            if (context.Rehearsal)
            {
                return states
                    .SetState(avatarAddress, MarkChanged)
                    .SetState(slotAddress, MarkChanged)
                    .SetState(context.Signer, MarkChanged)
                    .SetState(inventoryAddress, MarkChanged)
                    .SetState(worldInformationAddress, MarkChanged)
                    .SetState(questListAddress, MarkChanged)
                    .MarkBalanceChanged(GoldCurrencyMock, context.Signer, BlacksmithAddress);
            }

            var addressesHex = GetSignerAndOtherAddressesHex(context, avatarAddress);

            if (!states.TryGetAgentAvatarStatesV2(context.Signer, avatarAddress, out var agentState,
                out var avatarState, out _))
            {
                throw new FailedLoadStateException(
                    $"{addressesHex}Aborted as the avatar state of the signer was failed to load.");
            }

            // Validate Required Cleared Stage
            if (!avatarState.worldInformation.IsStageCleared(
                GameConfig.RequireClearedStageLevel.CombinationEquipmentAction))
            {
                avatarState.worldInformation.TryGetLastClearedStageId(out var current);
                throw new NotEnoughClearedStageLevelException(
                    addressesHex,
                    GameConfig.RequireClearedStageLevel.CombinationEquipmentAction,
                    current);
            }
            // ~Validate Required Cleared Stage

            // Validate SlotIndex
            var slotState = states.GetCombinationSlotState(avatarAddress, slotIndex);
            if (slotState is null)
            {
                throw new FailedLoadStateException(
                    $"{addressesHex}Aborted as the slot state is failed to load: # {slotIndex}");
            }

            if (!slotState.Validate(avatarState, context.BlockIndex))
            {
                throw new CombinationSlotUnlockException(
                    $"{addressesHex}Aborted as the slot state is invalid: {slotState} @ {slotIndex}");
            }
            // ~Validate SlotIndex

            // Validate Work
            var costActionPoint = 0;
            var costNCG = 0L;
            var endBlockIndex = context.BlockIndex;
            var requiredFungibleItems = new Dictionary<int, int>();

            // Validate RecipeId
            var equipmentItemRecipeSheet = states.GetSheet<EquipmentItemRecipeSheet>();
            if (!equipmentItemRecipeSheet.TryGetValue(recipeId, out var recipeRow))
            {
                throw new SheetRowNotFoundException(
                    addressesHex,
                    nameof(EquipmentItemRecipeSheet),
                    recipeId);
            }
            // ~Validate RecipeId

            // Validate Recipe Unlocked.
            if (!avatarState.worldInformation.IsStageCleared(recipeRow.UnlockStage))
            {
                avatarState.worldInformation.TryGetLastClearedStageId(out var current);
                throw new NotEnoughClearedStageLevelException(
                    addressesHex,
                    recipeRow.UnlockStage,
                    current);
            }
            // ~Validate Recipe Unlocked

            // Validate Recipe ResultEquipmentId
            var equipmentItemSheet = states.GetSheet<EquipmentItemSheet>();
            if (!equipmentItemSheet.TryGetValue(recipeRow.ResultEquipmentId, out var equipmentRow))
            {
                throw new SheetRowNotFoundException(
                    addressesHex,
                    nameof(equipmentItemSheet),
                    recipeRow.ResultEquipmentId);
            }
            // ~Validate Recipe ResultEquipmentId

            // Validate Recipe Material
            var materialItemSheet = states.GetSheet<MaterialItemSheet>();
            if (!materialItemSheet.TryGetValue(recipeRow.MaterialId, out var materialRow))
            {
                throw new SheetRowNotFoundException(
                    addressesHex,
                    nameof(MaterialItemSheet),
                    recipeRow.MaterialId);
            }

            if (requiredFungibleItems.ContainsKey(materialRow.Id))
            {
                requiredFungibleItems[materialRow.Id] += recipeRow.MaterialCount;
            }
            else
            {
                requiredFungibleItems[materialRow.Id] = recipeRow.MaterialCount;
            }
            // ~Validate Recipe Material

            // Validate SubRecipeId
            EquipmentItemSubRecipeSheetV2.Row subRecipeRow = null;
            if (subRecipeId.HasValue)
            {
                if (!recipeRow.SubRecipeIds.Contains(subRecipeId.Value))
                {
                    throw new SheetRowColumnException(
                        $"{addressesHex}Aborted as the sub recipe {subRecipeId.Value} was failed to load from the sheet."
                    );
                }

                var equipmentItemSubRecipeSheetV2 = states.GetSheet<EquipmentItemSubRecipeSheetV2>();
                if (!equipmentItemSubRecipeSheetV2.TryGetValue(subRecipeId.Value, out subRecipeRow))
                {
                    throw new SheetRowNotFoundException(
                        addressesHex,
                        nameof(EquipmentItemSubRecipeSheetV2),
                        subRecipeId.Value);
                }

                // Validate SubRecipe Material
                for (var i = subRecipeRow.Materials.Count; i > 0; i--)
                {
                    var materialInfo = subRecipeRow.Materials[i - 1];
                    if (!materialItemSheet.TryGetValue(materialInfo.Id, out materialRow))
                    {
                        throw new SheetRowNotFoundException(
                            addressesHex,
                            nameof(MaterialItemSheet),
                            materialInfo.Id);
                    }

                    if (requiredFungibleItems.ContainsKey(materialRow.Id))
                    {
                        requiredFungibleItems[materialRow.Id] += materialInfo.Count;
                    }
                    else
                    {
                        requiredFungibleItems[materialRow.Id] = materialInfo.Count;
                    }
                }
                // ~Validate SubRecipe Material

                costActionPoint += subRecipeRow.RequiredActionPoint;
                costNCG += subRecipeRow.RequiredGold;
                endBlockIndex += subRecipeRow.RequiredBlockIndex;
            }
            // ~Validate SubRecipeId

            costActionPoint += recipeRow.RequiredActionPoint;
            costNCG += recipeRow.RequiredGold;
            endBlockIndex += recipeRow.RequiredBlockIndex;
            // ~Validate Work

            // Remove Required Materials
            var inventory = avatarState.inventory;
            foreach (var pair in requiredFungibleItems.OrderBy(pair => pair.Key))
            {
                if (!materialItemSheet.TryGetValue(pair.Key, out materialRow) ||
                    !inventory.RemoveFungibleItem(materialRow.ItemId, context.BlockIndex, pair.Value))
                {
                    throw new NotEnoughMaterialException(
                        $"{addressesHex}Aborted as the player has no enough material ({pair.Key} * {pair.Value})");
                }
            }
            // ~Remove Required Materials

            // Subtract Required ActionPoint
            if (costActionPoint > 0)
            {
                if (avatarState.actionPoint < costActionPoint)
                {
                    throw new NotEnoughActionPointException(
                        $"{addressesHex}Aborted due to insufficient action point: {avatarState.actionPoint} < {costActionPoint}"
                    );
                }

                avatarState.actionPoint -= costActionPoint;
            }
            // ~Subtract Required ActionPoint

            // Transfer Required NCG
            if (costNCG > 0L)
            {
                states = states.TransferAsset(
                    context.Signer,
                    BlacksmithAddress,
                    states.GetGoldCurrency() * costNCG
                );
            }
            // ~Transfer Required NCG

            // Create Equipment
            var equipment = (Equipment) ItemFactory.CreateItemUsable(
                equipmentRow,
                context.Random.GenerateRandomGuid(),
                endBlockIndex);

            if (!(subRecipeRow is null))
            {
                AddAndUnlockOption(
                    agentState,
                    equipment,
                    context.Random,
                    subRecipeRow,
                    states.GetSheet<EquipmentItemOptionSheet>(),
                    states.GetSheet<SkillSheet>()
                );
                endBlockIndex = equipment.RequiredBlockIndex;
            }
            // ~Create Equipment

            // Add or Update Equipment
            avatarState.blockIndex = context.BlockIndex;
            avatarState.updatedAt = context.BlockIndex;
            avatarState.questList.UpdateCombinationEquipmentQuest(recipeId);
            avatarState.UpdateFromCombination(equipment);
            avatarState.UpdateQuestRewards(materialItemSheet);
            // ~Add or Update Equipment

            // Update Slot
            var mailId = context.Random.GenerateRandomGuid();
            var attachmentResult = new CombinationConsumable5.ResultModel
            {
                id = mailId,
                actionPoint = costActionPoint,
                gold = costNCG,
                materials = requiredFungibleItems.ToDictionary(
                    e => ItemFactory.CreateMaterial(materialItemSheet, e.Key),
                    e => e.Value),
                itemUsable = equipment,
                recipeId = recipeId,
                subRecipeId = subRecipeId,
            };
            slotState.Update(attachmentResult, context.BlockIndex, endBlockIndex);
            // ~Update Slot

            // Create Mail
            var mail = new CombinationMail(
                attachmentResult,
                context.BlockIndex,
                mailId,
                endBlockIndex);
            avatarState.Update(mail);
            // ~Create Mail

            return states
                .SetState(avatarAddress, avatarState.SerializeV2())
                .SetState(inventoryAddress, avatarState.inventory.Serialize())
                .SetState(worldInformationAddress, avatarState.worldInformation.Serialize())
                .SetState(questListAddress, avatarState.questList.Serialize())
                .SetState(slotAddress, slotState.Serialize())
                .SetState(context.Signer, agentState.Serialize());
        }

        public static void AddAndUnlockOption(
            AgentState agentState,
            Equipment equipment,
            IRandom random,
            EquipmentItemSubRecipeSheetV2.Row subRecipe,
            EquipmentItemOptionSheet optionSheet,
            SkillSheet skillSheet
        )
        {
            foreach (var optionInfo in subRecipe.Options
                .OrderByDescending(e => e.Ratio)
                .ThenBy(e => e.RequiredBlockIndex)
                .ThenBy(e => e.Id))
            {
                if (!optionSheet.TryGetValue(optionInfo.Id, out var optionRow))
                {
                    continue;
                }

                var value = random.Next(1, GameConfig.MaximumProbability + 1);
                if (value > optionInfo.Ratio)
                {
                    continue;
                }

                if (optionRow.StatType != StatType.NONE)
                {
                    var statMap = CombinationEquipment5.GetStat(optionRow, random);
                    equipment.StatsMap.AddStatAdditionalValue(statMap.StatType, statMap.Value);
                    equipment.Update(equipment.RequiredBlockIndex + optionInfo.RequiredBlockIndex);
                    equipment.optionCountFromCombination++;
                    agentState.unlockedOptions.Add(optionRow.Id);
                }
                else
                {
                    var skill = CombinationEquipment5.GetSkill(optionRow, skillSheet, random);
                    if (!(skill is null))
                    {
                        equipment.Skills.Add(skill);
                        equipment.Update(equipment.RequiredBlockIndex + optionInfo.RequiredBlockIndex);
                        equipment.optionCountFromCombination++;
                        agentState.unlockedOptions.Add(optionRow.Id);
                    }
                }
            }
        }
    }
}
