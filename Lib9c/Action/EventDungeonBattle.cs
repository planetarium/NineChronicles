using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
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
    /// Introduced at https://github.com/planetarium/lib9c/pull/
    /// </summary>
    [Serializable]
    [ActionType(ActionTypeString)]
    public class EventDungeonBattle : GameAction
    {
        private const string ActionTypeString = "event_dungeon_battle";

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

            if (list.Count < 8)
            {
                throw new ArgumentException("'l' must contain at least 8 items");
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
            Log.Verbose("{AddressesHex}HAS exec started", addressesHex);

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
                    $"{addressesHex}Aborted as the avatar state of the signer was failed to load.");
            }

            sw.Stop();
            Log.Verbose(
                "[{ActionTypeString}] TryGetAvatarStateV2: {Elapsed} [{AddressesHex}]",
                ActionTypeString,
                sw.Elapsed,
                addressesHex);
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
                "[{ActionTypeString}] Get sheets: {Elapsed} [{AddressesHex}]",
                ActionTypeString,
                sw.Elapsed,
                addressesHex);
            // ~Get sheets

            // Validate fields.
            var eventSheet = sheets.GetSheet<EventScheduleSheet>();
            if (!eventSheet.TryGetValue(eventScheduleId, out var eventRow))
            {
                throw new SheetRowNotFoundException(
                    addressesHex,
                    eventSheet.Name,
                    eventScheduleId);
            }

            if (context.BlockIndex < eventRow.StartBlockIndex ||
                context.BlockIndex > eventRow.DungeonEndBlockIndex)
            {
                throw new InvalidActionFieldException(
                    ActionTypeString,
                    nameof(eventDungeonId),
                    addressesHex,
                    "Aborted as the block index is out of the range of the event dungeon." +
                    $"current({context.BlockIndex}), start({eventRow.StartBlockIndex}), end({eventRow.DungeonEndBlockIndex})");
            }

            if (eventDungeonId.ToEventScheduleId() != eventScheduleId)
            {
                throw new InvalidActionFieldException(
                    ActionTypeString,
                    nameof(eventDungeonId),
                    addressesHex,
                    "Aborted as the event dungeon id is not matched with the event schedule id." +
                    $"event dungeon id: {eventDungeonId}, event schedule id: {eventScheduleId}");
            }

            var dungeonSheet = sheets.GetSheet<EventDungeonSheet>();
            if (!dungeonSheet.TryGetValue(eventDungeonId, out var dungeonRow))
            {
                throw new SheetRowNotFoundException(
                    addressesHex,
                    dungeonSheet.Name,
                    eventDungeonId);
            }

            if (eventDungeonStageId < dungeonRow.StageBegin ||
                eventDungeonStageId > dungeonRow.StageEnd)
            {
                throw new InvalidActionFieldException(
                    ActionTypeString,
                    nameof(eventDungeonStageId),
                    addressesHex,
                    "Aborted as the event dungeon stage id is out of the range of the event dungeon." +
                    $"stage id: {eventDungeonStageId}, stage begin: {dungeonRow.StageBegin}, stage end: {dungeonRow.StageEnd}");
            }

            var stageSheet = sheets.GetSheet<EventDungeonStageSheet>();
            if (!stageSheet.TryGetValue(eventDungeonStageId, out var stageRow))
            {
                throw new SheetRowNotFoundException(
                    addressesHex,
                    stageSheet.Name,
                    eventDungeonStageId);
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

            var eventDungeonInfoAddr = EventDungeonInfo.DeriveAddress(
                avatarAddress,
                eventDungeonId);
            var eventDungeonInfo =
                new EventDungeonInfo(states.GetState(eventDungeonInfoAddr));
            if (!eventDungeonInfo.TryUseTickets(1))
            {
                throw new NotEnoughEventDungeonTicketsException(
                    ActionTypeString,
                    addressesHex,
                    1,
                    eventDungeonInfo.RemainingTickets);
            }

            if (eventDungeonStageId != dungeonRow.StageBegin &&
                !eventDungeonInfo.IsCleared(eventDungeonStageId - 1))
            {
                throw new InvalidActionFieldException(
                    ActionTypeString,
                    nameof(eventDungeonStageId),
                    addressesHex,
                    $"Aborted as the eventDungeonStageId({eventDungeonStageId}) cannot play before the previous stage({eventDungeonStageId - 1}) is cleared.");
            }

            sw.Stop();
            Log.Verbose(
                "[{ActionTypeString}] Validate fields: {Elapsed} [{AddressesHex}]",
                ActionTypeString,
                sw.Elapsed,
                addressesHex);
            // ~Validate fields.

            // Simulate
            sw.Restart();
            const int playCount = 1;
            var simulator = new EventDungeonBattleSimulator(
                context.Random,
                avatarState,
                foods,
                eventDungeonId,
                eventDungeonStageId,
                sheets.GetEventDungeonBattleSimulatorSheets(),
                StageSimulator.ConstructorVersionV100080,
                eventDungeonInfo.IsCleared(eventDungeonStageId),
                0,
                playCount);
            simulator.Simulate(playCount);
            sw.Stop();
            Log.Verbose(
                "[{ActionTypeString}] Simulate: {Elapsed} [{AddressesHex}]",
                ActionTypeString,
                sw.Elapsed,
                addressesHex);
            // ~Simulate

            // Update avatar's event dungeon info.
            if (simulator.Log.IsClear)
            {
                sw.Restart();
                eventDungeonInfo.ClearStage(eventDungeonStageId);
                sw.Stop();
                Log.Verbose(
                    "[{ActionTypeString}] Update event dungeon info: {Elapsed} [{AddressesHex}]",
                    ActionTypeString,
                    sw.Elapsed,
                    addressesHex);
            }
            // ~Update avatar's event dungeon info.

            // Apply player to avatar state
            sw.Restart();
            avatarState.Apply(simulator.Player, context.BlockIndex);
            sw.Stop();
            Log.Verbose(
                "[{ActionTypeString}] Apply player to avatar state: {Elapsed} [{AddressesHex}]",
                ActionTypeString,
                sw.Elapsed,
                addressesHex);
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
                "[{ActionTypeString}] Set states: {Elapsed} [{AddressesHex}]",
                ActionTypeString,
                sw.Elapsed,
                addressesHex);
            // ~Set states

            Log.Verbose(
                "[{ActionTypeString}] Total elapsed: {Elapsed} [{AddressesHex}]",
                ActionTypeString,
                DateTimeOffset.UtcNow - started,
                addressesHex);
            return states;
        }
    }
}
