using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Serilog;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    /// <summary>
    /// Hard forked at https://github.com/planetarium/lib9c/pull/637
    /// Updated at https://github.com/planetarium/lib9c/pull/861
    /// Updated at https://github.com/planetarium/lib9c/pull/957
    /// </summary>
    [Serializable]
    [ActionType("combination_consumable8")]
    public class CombinationConsumable : GameAction, ICombinationConsumableV1
    {
        public const string AvatarAddressKey = "a";
        public Address avatarAddress;

        public const string SlotIndexKey = "s";
        public int slotIndex;

        public const string RecipeIdKey = "r";
        public int recipeId;

        Address ICombinationConsumableV1.AvatarAddress => avatarAddress;
        int ICombinationConsumableV1.RecipeId => recipeId;
        int ICombinationConsumableV1.SlotIndex => slotIndex;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                [AvatarAddressKey] = avatarAddress.Serialize(),
                [SlotIndexKey] = slotIndex.Serialize(),
                [RecipeIdKey] = recipeId.Serialize(),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            avatarAddress = plainValue[AvatarAddressKey].ToAddress();
            slotIndex = plainValue[SlotIndexKey].ToInteger();
            recipeId = plainValue[RecipeIdKey].ToInteger();
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
                    .SetState(context.Signer, MarkChanged)
                    .SetState(inventoryAddress, MarkChanged)
                    .SetState(worldInformationAddress, MarkChanged)
                    .SetState(questListAddress, MarkChanged)
                    .SetState(slotAddress, MarkChanged);
            }

            var addressesHex = GetSignerAndOtherAddressesHex(context, avatarAddress);
            var started = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}Combination exec started", addressesHex);

            if (!states.TryGetAvatarStateV2(context.Signer, avatarAddress, out var avatarState, out _))
            {
                throw new FailedLoadStateException(
                    $"{addressesHex}Aborted as the avatar state of the signer was failed to load.");
            }

            // Validate Required Cleared Stage
            if (!avatarState.worldInformation.IsStageCleared(
                GameConfig.RequireClearedStageLevel.CombinationConsumableAction))
            {
                avatarState.worldInformation.TryGetLastClearedStageId(out var current);
                throw new NotEnoughClearedStageLevelException(
                    addressesHex,
                    GameConfig.RequireClearedStageLevel.CombinationConsumableAction,
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
            var endBlockIndex = context.BlockIndex;
            var requiredFungibleItems = new Dictionary<int, int>();

            // Validate RecipeId
            var consumableItemRecipeSheet = states.GetSheet<ConsumableItemRecipeSheet>();
            if (!consumableItemRecipeSheet.TryGetValue(recipeId, out var recipeRow))
            {
                throw new SheetRowNotFoundException(
                    addressesHex,
                    nameof(ConsumableItemRecipeSheet),
                    recipeId);
            }
            // ~Validate RecipeId

            // Validate Recipe ResultEquipmentId
            var consumableItemSheet = states.GetSheet<ConsumableItemSheet>();
            if (!consumableItemSheet.TryGetValue(recipeRow.ResultConsumableItemId, out var consumableRow))
            {
                throw new SheetRowNotFoundException(
                    addressesHex,
                    nameof(consumableItemSheet),
                    recipeRow.ResultConsumableItemId);
            }
            // ~Validate Recipe ResultEquipmentId

            // Validate Recipe Material
            var materialItemSheet = states.GetSheet<MaterialItemSheet>();
            for (var i = recipeRow.Materials.Count; i > 0; i--)
            {
                var materialInfo = recipeRow.Materials[i - 1];
                if (!materialItemSheet.TryGetValue(materialInfo.Id, out var materialRow))
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
            // ~Validate Recipe Material

            costActionPoint += recipeRow.RequiredActionPoint;
            endBlockIndex += recipeRow.RequiredBlockIndex;
            // ~Validate Work

            // Remove Required Materials
            var inventory = avatarState.inventory;
            foreach (var pair in requiredFungibleItems.OrderBy(pair => pair.Key))
            {
                if (!materialItemSheet.TryGetValue(pair.Key, out var materialRow) ||
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

            // Create Consumable
            var consumable = (Consumable) ItemFactory.CreateItemUsable(
                consumableRow,
                context.Random.GenerateRandomGuid(),
                endBlockIndex
            );
            // ~Create Consumable

            // Add or Update Consumable
            avatarState.blockIndex = context.BlockIndex;
            avatarState.updatedAt = context.BlockIndex;
            avatarState.UpdateFromCombination(consumable);
            avatarState.UpdateQuestRewards(materialItemSheet);
            // ~Add or Update Consumable

            // Update Slot
            var mailId = context.Random.GenerateRandomGuid();
            var attachmentResult = new CombinationConsumable5.ResultModel
            {
                id = mailId,
                actionPoint = costActionPoint,
                materials = requiredFungibleItems.ToDictionary(
                    e => ItemFactory.CreateMaterial(materialItemSheet, e.Key),
                    e => e.Value),
                itemUsable = consumable,
                recipeId = recipeId,
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

            var ended = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}Combination Total Executed Time: {Elapsed}", addressesHex, ended - started);
            return states
                .SetState(avatarAddress, avatarState.SerializeV2())
                .SetState(inventoryAddress, avatarState.inventory.Serialize())
                .SetState(worldInformationAddress, avatarState.worldInformation.Serialize())
                .SetState(questListAddress, avatarState.questList.Serialize())
                .SetState(slotAddress, slotState.Serialize());
        }
    }
}
