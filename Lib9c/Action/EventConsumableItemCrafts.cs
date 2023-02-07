using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Extensions;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Nekoyume.TableData.Event;
using Serilog;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType(ActionTypeText)]
    public class EventConsumableItemCrafts : GameAction, IEventConsumableItemCraftsV1
    {
        private const string ActionTypeText = "event_consumable_item_crafts";

        public Address AvatarAddress;
        public int EventScheduleId;
        public int EventConsumableItemRecipeId;
        public int SlotIndex;

        Address IEventConsumableItemCraftsV1.AvatarAddress => AvatarAddress;
        int IEventConsumableItemCraftsV1.EventScheduleId => EventScheduleId;
        int IEventConsumableItemCraftsV1.EventConsumableItemRecipeId => EventConsumableItemRecipeId;
        int IEventConsumableItemCraftsV1.SlotIndex => SlotIndex;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal
        {
            get
            {
                var list = Bencodex.Types.List.Empty
                    .Add(AvatarAddress.Serialize())
                    .Add(EventScheduleId.Serialize())
                    .Add(EventConsumableItemRecipeId.Serialize())
                    .Add(SlotIndex.Serialize());

                return new Dictionary<string, IValue>
                {
                    { "l", list },
                }.ToImmutableDictionary();
            }
        }

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            if (!plainValue.TryGetValue("l", out var serialized))
            {
                throw new ArgumentException("plainValue must contain 'l'");
            }

            if (!(serialized is Bencodex.Types.List list))
            {
                throw new ArgumentException("'l' must be a bencodex list");
            }

            if (list.Count < 4)
            {
                throw new ArgumentException("'l' must contain at least 4 items");
            }

            AvatarAddress = list[0].ToAddress();
            EventScheduleId = list[1].ToInteger();
            EventConsumableItemRecipeId = list[2].ToInteger();
            SlotIndex = list[3].ToInteger();
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            if (context.Rehearsal)
            {
                return states;
            }

            var addressesHex = GetSignerAndOtherAddressesHex(context, AvatarAddress);
            var started = DateTimeOffset.UtcNow;
            Log.Verbose(
                "[{ActionTypeString}][{AddressesHex}] Execute() start",
                ActionTypeText,
                addressesHex);

            var sw = new Stopwatch();
            // Get AvatarState
            sw.Start();
            if (!states.TryGetAvatarStateV2(
                    context.Signer,
                    AvatarAddress,
                    out var avatarState,
                    out var migrationRequired))
            {
                throw new FailedLoadStateException(
                    ActionTypeText,
                    addressesHex,
                    typeof(AvatarState),
                    AvatarAddress);
            }

            sw.Stop();
            Log.Verbose(
                "[{ActionTypeString}][{AddressesHex}] TryGetAvatarStateV2: {Elapsed}",
                ActionTypeText,
                addressesHex,
                sw.Elapsed);
            // ~Get AvatarState

            // Get sheets
            sw.Restart();
            var sheets = states.GetSheets(
                sheetTypes: new[]
                {
                    typeof(EventScheduleSheet),
                    typeof(EventConsumableItemRecipeSheet),
                });
            sw.Stop();
            Log.Verbose(
                "[{ActionTypeString}][{AddressesHex}] Get sheets: {Elapsed}",
                ActionTypeText,
                addressesHex,
                sw.Elapsed);
            // ~Get sheets

            // Validate Requirements.
            sw.Restart();
            avatarState.worldInformation.ValidateFromAction(
                GameConfig.RequireClearedStageLevel.CombinationConsumableAction,
                ActionTypeText,
                addressesHex);
            sw.Stop();
            Log.Verbose(
                "[{ActionTypeString}][{AddressesHex}] Validate requirements: {Elapsed}",
                ActionTypeText,
                addressesHex,
                sw.Elapsed);
            // ~Validate Requirements.

            // Validate fields.
            sw.Restart();
            var scheduleSheet = sheets.GetSheet<EventScheduleSheet>();
            scheduleSheet.ValidateFromActionForRecipe(
                context.BlockIndex,
                EventScheduleId,
                EventConsumableItemRecipeId,
                ActionTypeText,
                addressesHex);

            var recipeSheet = sheets.GetSheet<EventConsumableItemRecipeSheet>();
            var recipeRow = recipeSheet.ValidateFromAction(
                EventConsumableItemRecipeId,
                ActionTypeText,
                addressesHex);

            var slotState = states.GetCombinationSlotState(AvatarAddress, SlotIndex);
            if (slotState is null)
            {
                throw new FailedLoadStateException(
                    $"{addressesHex}Aborted as the slot state is failed to load: # {SlotIndex}");
            }

            if (!slotState.Validate(avatarState, context.BlockIndex))
            {
                throw new CombinationSlotUnlockException(
                    $"{addressesHex}Aborted as the slot state is invalid: {slotState} @ {SlotIndex}");
            }

            sw.Stop();
            Log.Verbose(
                "[{ActionTypeString}][{AddressesHex}] Validate fields: {Elapsed}",
                ActionTypeText,
                addressesHex,
                sw.Elapsed);
            // ~Validate fields.

            // Validate Work
            sw.Restart();
            var costActionPoint = 0;
            var endBlockIndex = context.BlockIndex;
            var requiredFungibleItems = new Dictionary<int, int>();

            // Validate Recipe ResultEquipmentId
            var consumableItemSheet = states.GetSheet<ConsumableItemSheet>();
            if (!consumableItemSheet.TryGetValue(
                    recipeRow.ResultConsumableItemId,
                    out var consumableRow))
            {
                throw new SheetRowNotFoundException(
                    addressesHex,
                    nameof(consumableItemSheet),
                    recipeRow.ResultConsumableItemId);
            }
            // ~Validate Recipe ResultEquipmentId

            // Validate Recipe Material
            var materialItemSheet = states.GetSheet<MaterialItemSheet>();
            materialItemSheet.ValidateFromAction(
                recipeRow.Materials,
                requiredFungibleItems,
                addressesHex);
            // ~Validate Recipe Material

            costActionPoint += recipeRow.RequiredActionPoint;
            endBlockIndex += recipeRow.RequiredBlockIndex;
            sw.Stop();
            Log.Verbose(
                "[{ActionTypeString}][{AddressesHex}] Validate work: {Elapsed}",
                ActionTypeText,
                addressesHex,
                sw.Elapsed);
            // ~Validate Work

            // Remove Required Materials
            var inventory = avatarState.inventory;
#pragma warning disable LAA1002
            foreach (var pair in requiredFungibleItems)
#pragma warning restore LAA1002
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

            // Create and Add Consumable
            var consumable = ItemFactory.CreateItemUsable(
                consumableRow,
                context.Random.GenerateRandomGuid(),
                endBlockIndex
            );
            avatarState.inventory.AddItem(consumable);
            // ~Create and Add Consumable

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
                recipeId = EventConsumableItemRecipeId,
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

            // Set states
            if (migrationRequired)
            {
                states = states
                    .SetState(AvatarAddress, avatarState.SerializeV2())
                    .SetState(
                        AvatarAddress.Derive(LegacyInventoryKey),
                        avatarState.inventory.Serialize())
                    .SetState(
                        AvatarAddress.Derive(LegacyWorldInformationKey),
                        avatarState.worldInformation.Serialize())
                    .SetState(
                        AvatarAddress.Derive(LegacyQuestListKey),
                        avatarState.questList.Serialize())
                    .SetState(
                        CombinationSlotState.DeriveAddress(AvatarAddress, SlotIndex),
                        slotState.Serialize());
            }
            else
            {
                states = states
                    .SetState(AvatarAddress, avatarState.SerializeV2())
                    .SetState(
                        AvatarAddress.Derive(LegacyInventoryKey),
                        avatarState.inventory.Serialize())
                    .SetState(
                        CombinationSlotState.DeriveAddress(AvatarAddress, SlotIndex),
                        slotState.Serialize());
            }

            sw.Stop();
            Log.Verbose(
                "[{ActionTypeString}][{AddressesHex}] Set states: {Elapsed}",
                ActionTypeText,
                addressesHex,
                sw.Elapsed);
            // ~Set states

            Log.Verbose(
                "[{ActionTypeString}][{AddressesHex}] Total elapsed: {Elapsed}",
                ActionTypeText,
                addressesHex,
                DateTimeOffset.UtcNow - started);

            return states;
        }
    }
}
