using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Battle;
using Nekoyume.Exceptions;
using Nekoyume.Extensions;
using Nekoyume.Model.Event;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Nekoyume.TableData.Event;
using Serilog;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    /// <summary>
    /// Introduced at https://github.com/planetarium/lib9c/pull/1218
    /// </summary>
    [Serializable]
    [ActionType(ActionTypeText)]
    public class EventDungeonBattle : GameAction
    {
        private const string ActionTypeText = "event_dungeon_battle";
        public const int PlayCount = 1;

        public Address avatarAddress;
        public int eventScheduleId;
        public int eventDungeonId;
        public int eventDungeonStageId;
        public List<Guid> equipments;
        public List<Guid> costumes;
        public List<Guid> foods;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal
        {
            get
            {
                var list = Bencodex.Types.List.Empty
                    .Add(avatarAddress.Serialize())
                    .Add(eventScheduleId.Serialize())
                    .Add(eventDungeonId.Serialize())
                    .Add(eventDungeonStageId.Serialize())
                    .Add(new Bencodex.Types.List(
                        equipments
                            .OrderBy(e => e)
                            .Select(e => e.Serialize())))
                    .Add(new Bencodex.Types.List(
                        costumes
                            .OrderBy(e => e)
                            .Select(e => e.Serialize())))
                    .Add(new Bencodex.Types.List(
                        foods
                            .OrderBy(e => e)
                            .Select(e => e.Serialize())));

                return new Dictionary<string, IValue>
                {
                    { "l", list },
                }.ToImmutableDictionary();
            }
        }

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            if (!plainValue.TryGetValue("l", out var serialized))
            {
                throw new ArgumentException("plainValue must contain 'l'");
            }

            if (!(serialized is Bencodex.Types.List list))
            {
                throw new ArgumentException("'l' must be a bencodex list");
            }

            if (list.Count < 7)
            {
                throw new ArgumentException("'l' must contain at least 7 items");
            }

            avatarAddress = list[0].ToAddress();
            eventScheduleId = list[1].ToInteger();
            eventDungeonId = list[2].ToInteger();
            eventDungeonStageId = list[3].ToInteger();
            equipments = ((List)list[4]).ToList(StateExtensions.ToGuid);
            costumes = ((List)list[5]).ToList(StateExtensions.ToGuid);
            foods = ((List)list[6]).ToList(StateExtensions.ToGuid);
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            if (context.Rehearsal)
            {
                return states;
            }

            var addressesHex = GetSignerAndOtherAddressesHex(context, avatarAddress);
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
                    avatarAddress,
                    out var avatarState,
                    out var migrationRequired))
            {
                throw new FailedLoadStateException(
                    ActionTypeText,
                    typeof(AvatarState),
                    avatarAddress,
                    addressesHex);
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
                containEventDungeonSimulatorSheets: true,
                containValidateItemRequirementSheets: true,
                sheetTypes: new[]
                {
                    typeof(EventScheduleSheet),
                    typeof(EventDungeonSheet),
                });
            sw.Stop();
            Log.Verbose(
                "[{ActionTypeString}][{AddressesHex}] Get sheets: {Elapsed}",
                ActionTypeText,
                addressesHex,
                sw.Elapsed);
            // ~Get sheets

            // Validate fields.
            var scheduleSheet = sheets.GetSheet<EventScheduleSheet>();
            if (!scheduleSheet.TryGetValue(eventScheduleId, out var scheduleRow))
            {
                throw new InvalidActionFieldException(
                    ActionTypeText,
                    addressesHex,
                    nameof(eventScheduleId),
                    eventScheduleId.ToString(CultureInfo.InvariantCulture),
                    new SheetRowNotFoundException(
                        addressesHex,
                        scheduleSheet.Name,
                        eventScheduleId));
            }

            if (context.BlockIndex < scheduleRow.StartBlockIndex ||
                context.BlockIndex > scheduleRow.DungeonEndBlockIndex)
            {
                throw new InvalidActionFieldException(
                    ActionTypeText,
                    addressesHex,
                    nameof(eventScheduleId),
                    $"Aborted as the block index({context.BlockIndex}) is" +
                    " out of the range of the event schedule" +
                    $"({scheduleRow.StartBlockIndex} ~ {scheduleRow.DungeonEndBlockIndex}).");
            }

            try
            {
                var eventScheduleIdDerivedFromEventDungeonId =
                    eventDungeonId.ToEventScheduleId();
                if (eventScheduleIdDerivedFromEventDungeonId != eventScheduleId)
                {
                    throw new InvalidActionFieldException(
                        ActionTypeText,
                        addressesHex,
                        nameof(eventDungeonId),
                        $"Aborted as the derive event schedule" +
                        $" id({eventScheduleIdDerivedFromEventDungeonId}) from event dungeon" +
                        $" id({eventDungeonId}) is not matched with the event schedule" +
                        $" id({eventScheduleId}).");
                }
            }
            catch (ArgumentException e)
            {
                throw new InvalidActionFieldException(
                    ActionTypeText,
                    addressesHex,
                    nameof(eventDungeonId),
                    eventDungeonId.ToString(CultureInfo.InvariantCulture),
                    e);
            }

            var dungeonSheet = sheets.GetSheet<EventDungeonSheet>();
            if (!dungeonSheet.TryGetValue(eventDungeonId, out var dungeonRow))
            {
                throw new InvalidActionFieldException(
                    ActionTypeText,
                    addressesHex,
                    nameof(eventScheduleId),
                    " Aborted because the event dungeon is not found.",
                    new SheetRowNotFoundException(
                        addressesHex,
                        dungeonSheet.Name,
                        eventDungeonId));
            }

            if (eventDungeonStageId < dungeonRow.StageBegin ||
                eventDungeonStageId > dungeonRow.StageEnd)
            {
                throw new InvalidActionFieldException(
                    ActionTypeText,
                    addressesHex,
                    nameof(eventDungeonStageId),
                    $"Aborted as the event dungeon stage id({eventDungeonStageId})" +
                    " is out of the range of the event dungeon" +
                    $"({dungeonRow.StageBegin} ~ {dungeonRow.StageEnd}).");
            }

            var stageSheet = sheets.GetSheet<EventDungeonStageSheet>();
            if (!stageSheet.TryGetValue(eventDungeonStageId, out _))
            {
                throw new InvalidActionFieldException(
                    ActionTypeText,
                    addressesHex,
                    nameof(eventDungeonStageId),
                    eventDungeonStageId.ToString(CultureInfo.InvariantCulture),
                    new SheetRowNotFoundException(
                        addressesHex,
                        stageSheet.Name,
                        eventDungeonStageId));
            }

            var equipmentList = avatarState.ValidateEquipmentsV2(equipments, context.BlockIndex);
            var costumeIds = avatarState.ValidateCostume(costumes);
            var foodIds = avatarState.ValidateConsumable(foods, context.BlockIndex);
            var equipmentAndCostumes = equipments.Concat(costumes);
            avatarState.EquipItems(equipmentAndCostumes);
            avatarState.ValidateItemRequirement(
                costumeIds.Concat(foodIds).ToList(),
                equipmentList,
                sheets.GetSheet<ItemRequirementSheet>(),
                sheets.GetSheet<EquipmentItemRecipeSheet>(),
                sheets.GetSheet<EquipmentItemSubRecipeSheetV2>(),
                sheets.GetSheet<EquipmentItemOptionSheet>(),
                addressesHex);

            sw.Stop();
            Log.Verbose(
                "[{ActionTypeString}][{AddressesHex}] Validate fields: {Elapsed}",
                ActionTypeText,
                addressesHex,
                sw.Elapsed);
            // ~Validate fields.

            // Validate avatar's event dungeon info.
            sw.Restart();
            var eventDungeonInfoAddr = EventDungeonInfo.DeriveAddress(
                avatarAddress,
                eventDungeonId);
            var eventDungeonInfo = states.GetState(eventDungeonInfoAddr)
                is Bencodex.Types.List serializedEventDungeonInfoList
                ? new EventDungeonInfo(serializedEventDungeonInfoList)
                : new EventDungeonInfo(remainingTickets: scheduleRow.DungeonTicketsMax);

            // Update tickets.
            {
                var blockRange = context.BlockIndex - scheduleRow.StartBlockIndex;
                if (blockRange > 0)
                {
                    var interval =
                        (int)(blockRange / scheduleRow.DungeonTicketsResetIntervalBlockRange);
                    if (interval > eventDungeonInfo.ResetTicketsInterval)
                    {
                        eventDungeonInfo.ResetTickets(
                            interval,
                            scheduleRow.DungeonTicketsMax);
                    }
                }
            }
            // ~Update tickets.

            if (!eventDungeonInfo.TryUseTickets(PlayCount))
            {
                throw new NotEnoughEventDungeonTicketsException(
                    ActionTypeText,
                    addressesHex,
                    PlayCount,
                    eventDungeonInfo.RemainingTickets);
            }

            if (eventDungeonStageId != dungeonRow.StageBegin &&
                !eventDungeonInfo.IsCleared(eventDungeonStageId - 1))
            {
                throw new StageNotClearedException(
                    ActionTypeText,
                    addressesHex,
                    eventDungeonStageId - 1,
                    eventDungeonInfo.ClearedStageId);
            }

            sw.Stop();
            Log.Verbose(
                "[{ActionTypeString}][{AddressesHex}] Validate fields: {Elapsed}",
                ActionTypeText,
                addressesHex,
                sw.Elapsed);
            // ~Validate avatar's event dungeon info.

            // Simulate
            sw.Restart();
            // NOTE: This is a temporary solution. The formula is not yet decided.
            var exp = scheduleRow.DungeonExpSeedValue;
            var simulator = new EventDungeonBattleSimulator(
                context.Random,
                avatarState,
                foods,
                eventDungeonId,
                eventDungeonStageId,
                sheets.GetEventDungeonBattleSimulatorSheets(),
                PlayCount,
                eventDungeonInfo.IsCleared(eventDungeonStageId),
                exp);
            simulator.Simulate(PlayCount);
            sw.Stop();
            Log.Verbose(
                "[{ActionTypeString}][{AddressesHex}] Simulate: {Elapsed}",
                ActionTypeText,
                addressesHex,
                sw.Elapsed);
            // ~Simulate

            // Update avatar's event dungeon info.
            if (simulator.Log.IsClear)
            {
                sw.Restart();
                eventDungeonInfo.ClearStage(eventDungeonStageId);
                sw.Stop();
                Log.Verbose(
                    "[{ActionTypeString}][{AddressesHex}] Update event dungeon info: {Elapsed}",
                    ActionTypeText,
                    addressesHex,
                    sw.Elapsed);
            }
            // ~Update avatar's event dungeon info.

            // Apply player to avatar state
            sw.Restart();
            avatarState.Apply(simulator.Player, context.BlockIndex);
            sw.Stop();
            Log.Verbose(
                "[{ActionTypeString}][{AddressesHex}] Apply player to avatar state: {Elapsed}",
                ActionTypeText,
                addressesHex,
                sw.Elapsed);
            // ~Apply player to avatar state

            // Set states
            sw.Restart();
            if (migrationRequired)
            {
                states = states
                    .SetState(avatarAddress, avatarState.SerializeV2())
                    .SetState(
                        avatarAddress.Derive(LegacyInventoryKey),
                        avatarState.inventory.Serialize())
                    .SetState(
                        avatarAddress.Derive(LegacyWorldInformationKey),
                        avatarState.worldInformation.Serialize())
                    .SetState(
                        avatarAddress.Derive(LegacyQuestListKey),
                        avatarState.questList.Serialize())
                    .SetState(eventDungeonInfoAddr, eventDungeonInfo.Serialize());
            }
            else
            {
                states = states
                    .SetState(avatarAddress, avatarState.SerializeV2())
                    .SetState(
                        avatarAddress.Derive(LegacyInventoryKey),
                        avatarState.inventory.Serialize())
                    .SetState(eventDungeonInfoAddr, eventDungeonInfo.Serialize());
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
