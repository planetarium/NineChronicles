namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Exceptions;
    using Nekoyume.Extensions;
    using Nekoyume.Model.Event;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Nekoyume.TableData.Event;
    using Xunit;
    using static Lib9c.SerializeKeys;

    public class EventDungeonBattleTest
    {
        private readonly IAccountStateDelta _initialStates;
        private readonly TableSheets _tableSheets;

        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;

        public EventDungeonBattleTest()
        {
            _initialStates = new State();
            var sheets = TableSheetsImporter.ImportSheets();
            foreach (var (key, value) in sheets)
            {
                _initialStates = _initialStates
                    .SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            _tableSheets = new TableSheets(sheets);

            _agentAddress = new PrivateKey().ToAddress();
            _avatarAddress = _agentAddress.Derive("avatar");
            var inventoryAddr = _avatarAddress.Derive(LegacyInventoryKey);
            var worldInformationAddr = _avatarAddress.Derive(LegacyWorldInformationKey);
            var questListAddr = _avatarAddress.Derive(LegacyQuestListKey);

            var agentState = new AgentState(_agentAddress);
            agentState.avatarAddresses.Add(0, _avatarAddress);

            var gameConfigState = new GameConfigState(sheets[nameof(GameConfigSheet)]);
            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                gameConfigState,
                new PrivateKey().ToAddress()
            )
            {
                level = 100,
            };

            _initialStates = _initialStates
                .SetState(_agentAddress, agentState.Serialize())
                .SetState(_avatarAddress, avatarState.SerializeV2())
                .SetState(inventoryAddr, avatarState.inventory.Serialize())
                .SetState(worldInformationAddr, avatarState.worldInformation.Serialize())
                .SetState(questListAddr, avatarState.questList.Serialize())
                .SetState(gameConfigState.address, gameConfigState.Serialize());
        }

        [Theory]
        [InlineData(1001, 10010001, 10010001)]
        public void Execute_Success(
            int eventScheduleId,
            int eventDungeonId,
            int eventDungeonStageId)
        {
            Assert.True(_tableSheets.EventScheduleSheet
                .TryGetValue(eventScheduleId, out var scheduleRow));
            var contextBlockIndex = scheduleRow.StartBlockIndex;
            Execute(
                _initialStates,
                eventScheduleId,
                eventDungeonId,
                eventDungeonStageId,
                contextBlockIndex);
            contextBlockIndex = scheduleRow.DungeonEndBlockIndex;
            Execute(
                _initialStates,
                eventScheduleId,
                eventDungeonId,
                eventDungeonStageId,
                contextBlockIndex);
        }

        [Theory]
        [InlineData(10000001, 10010001, 10010001)]
        [InlineData(10010001, 10010001, 10010001)]
        public void Execute_Throw_InvalidActionFieldException_By_EventScheduleId(
            int eventScheduleId,
            int eventDungeonId,
            int eventDungeonStageId) =>
            Assert.Throws<InvalidActionFieldException>(() =>
                Execute(
                    _initialStates,
                    eventScheduleId,
                    eventDungeonId,
                    eventDungeonStageId));

        [Theory]
        [InlineData(1001, 10010001, 10010001)]
        public void Execute_Throw_InvalidActionFieldException_By_ContextBlockIndex(
            int eventScheduleId,
            int eventDungeonId,
            int eventDungeonStageId)
        {
            Assert.True(_tableSheets.EventScheduleSheet
                .TryGetValue(eventScheduleId, out var scheduleRow));
            var contextBlockIndex = scheduleRow.StartBlockIndex - 1;
            Assert.Throws<InvalidActionFieldException>(() =>
                Execute(
                    _initialStates,
                    eventScheduleId,
                    eventDungeonId,
                    eventDungeonStageId,
                    contextBlockIndex));
            contextBlockIndex = scheduleRow.DungeonEndBlockIndex + 1;
            Assert.Throws<InvalidActionFieldException>(() =>
                Execute(
                    _initialStates,
                    eventScheduleId,
                    eventDungeonId,
                    eventDungeonStageId,
                    contextBlockIndex));
        }

        [Theory]
        [InlineData(1001, 10020001, 10010001)]
        [InlineData(1001, 1001, 10010001)]
        public void Execute_Throw_InvalidActionFieldException_By_EventDungeonId(
            int eventScheduleId,
            int eventDungeonId,
            int eventDungeonStageId)
        {
            Assert.True(_tableSheets.EventScheduleSheet
                .TryGetValue(eventScheduleId, out var scheduleRow));
            Assert.Throws<InvalidActionFieldException>(() =>
                Execute(
                    _initialStates,
                    eventScheduleId,
                    eventDungeonId,
                    eventDungeonStageId,
                    scheduleRow.StartBlockIndex));
        }

        [Theory]
        [InlineData(1001, 10010001, 10020001)]
        [InlineData(1001, 10010001, 1001)]
        public void Execute_Throw_InvalidActionFieldException_By_EventDungeonStageId(
            int eventScheduleId,
            int eventDungeonId,
            int eventDungeonStageId)
        {
            Assert.True(_tableSheets.EventScheduleSheet
                .TryGetValue(eventScheduleId, out var scheduleRow));
            Assert.Throws<InvalidActionFieldException>(() =>
                Execute(
                    _initialStates,
                    eventScheduleId,
                    eventDungeonId,
                    eventDungeonStageId,
                    scheduleRow.StartBlockIndex));
        }

        [Theory]
        [InlineData(1001, 10010001, 10010001)]
        public void Execute_Throw_NotEnoughEventDungeonTicketsException(
            int eventScheduleId,
            int eventDungeonId,
            int eventDungeonStageId)
        {
            var previousState = _initialStates;
            var eventDungeonInfoAddr =
                EventDungeonInfo.DeriveAddress(_avatarAddress, eventDungeonId);
            var eventDungeonInfo = new EventDungeonInfo(0, 0);
            previousState = previousState
                .SetState(eventDungeonInfoAddr, eventDungeonInfo.Serialize());
            Assert.True(_tableSheets.EventScheduleSheet
                .TryGetValue(eventScheduleId, out var scheduleRow));
            Assert.Throws<NotEnoughEventDungeonTicketsException>(() =>
                Execute(
                    previousState,
                    eventScheduleId,
                    eventDungeonId,
                    eventDungeonStageId,
                    scheduleRow.StartBlockIndex));
        }

        [Theory]
        [InlineData(1001, 10010001, 10010002)]
        public void Execute_Throw_StageNotClearedException(
            int eventScheduleId,
            int eventDungeonId,
            int eventDungeonStageId)
        {
            Assert.True(_tableSheets.EventScheduleSheet
                .TryGetValue(eventScheduleId, out var scheduleRow));
            Assert.Throws<StageNotClearedException>(() =>
                Execute(
                    _initialStates,
                    eventScheduleId,
                    eventDungeonId,
                    eventDungeonStageId,
                    scheduleRow.StartBlockIndex));
        }

        private void Execute(
            IAccountStateDelta previousStates,
            int eventScheduleId,
            int eventDungeonId,
            int eventDungeonStageId,
            long blockIndex = 0)
        {
            var previousAvatarState = previousStates.GetAvatarStateV2(_avatarAddress);
            var equipments =
                Doomfist.GetAllParts(_tableSheets, previousAvatarState.level);
            foreach (var equipment in equipments)
            {
                previousAvatarState.inventory.AddItem(equipment, iLock: null);
            }

            var action = new EventDungeonBattle
            {
                AvatarAddress = _avatarAddress,
                EventScheduleId = eventScheduleId,
                EventDungeonId = eventDungeonId,
                EventDungeonStageId = eventDungeonStageId,
                Equipments = equipments
                    .Select(e => e.NonFungibleId)
                    .ToList(),
                Costumes = new List<Guid>(),
                Foods = new List<Guid>(),
            };

            var nextStates = action.Execute(new ActionContext
            {
                PreviousStates = previousStates,
                Signer = _agentAddress,
                Random = new TestRandom(),
                Rehearsal = false,
                BlockIndex = blockIndex,
            });

            Assert.True(nextStates.GetSheet<EventScheduleSheet>().TryGetValue(
                eventScheduleId,
                out var scheduleRow));
            var nextAvatarState = nextStates.GetAvatarStateV2(_avatarAddress);
            var expectExp = scheduleRow.GetStageExp(
                eventDungeonStageId.ToEventDungeonStageNumber(),
                EventDungeonBattle.PlayCount);
            Assert.Equal(
                previousAvatarState.exp + expectExp,
                nextAvatarState.exp);
            var eventDungeonInfoAddr =
                EventDungeonInfo.DeriveAddress(_avatarAddress, eventDungeonId);
            var eventDungeonInfo =
                new EventDungeonInfo(nextStates.GetState(eventDungeonInfoAddr));
            Assert.Equal(
                scheduleRow.DungeonTicketsMax - 1,
                eventDungeonInfo.RemainingTickets);
        }
    }
}
