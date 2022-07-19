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
    using Nekoyume.Model.Event;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;
    using static Lib9c.SerializeKeys;

    public class EventDungeonBattleTest
    {
        private readonly IAccountStateDelta _initialState;
        private readonly TableSheets _tableSheets;

        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;

        public EventDungeonBattleTest()
        {
            _initialState = new State();
            var sheets = TableSheetsImporter.ImportSheets();
            foreach (var (key, value) in sheets)
            {
                _initialState = _initialState
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

            _initialState = _initialState
                .SetState(_agentAddress, agentState.Serialize())
                .SetState(_avatarAddress, avatarState.SerializeV2())
                .SetState(inventoryAddr, avatarState.inventory.Serialize())
                .SetState(worldInformationAddr, avatarState.worldInformation.Serialize())
                .SetState(questListAddr, avatarState.questList.Serialize())
                .SetState(gameConfigState.address, gameConfigState.Serialize());
        }

        [Theory]
        [InlineData(1001, 10010001, 10010001)]
        public void Execute(
            int eventScheduleId,
            int eventDungeonId,
            int eventDungeonStageId)
        {
            Assert.True(_tableSheets.EventDungeonSheet.TryGetValue(eventDungeonId, out var eventDungeonRow));
            Assert.True(eventDungeonStageId >= eventDungeonRow.StageBegin);
            Assert.True(eventDungeonStageId <= eventDungeonRow.StageEnd);
            Assert.True(_tableSheets.EventDungeonStageSheet.TryGetValue(eventDungeonStageId, out _));

            var previousAvatarState = _initialState.GetAvatarStateV2(_avatarAddress);

            var equipments = Doomfist.GetAllParts(_tableSheets, previousAvatarState.level);
            foreach (var equipment in equipments)
            {
                previousAvatarState.inventory.AddItem(equipment, iLock: null);
            }

            var inventoryAddr = _avatarAddress.Derive(LegacyInventoryKey);
            var eventDungeonInfoAddr = EventDungeonInfo.DeriveAddress(_avatarAddress, 1001);
            var previousStates = _initialState
                .SetState(_avatarAddress, previousAvatarState.SerializeV2())
                .SetState(inventoryAddr, previousAvatarState.inventory.Serialize())
                .SetState(eventDungeonInfoAddr, new EventDungeonInfo().Serialize());

            ////
            var action = new EventDungeonBattle
            {
                avatarAddress = _avatarAddress,
                eventScheduleId = eventScheduleId,
                eventDungeonId = eventDungeonId,
                eventDungeonStageId = eventDungeonStageId,
                equipments = equipments.Select(e => e.NonFungibleId).ToList(),
                costumes = new List<Guid>(),
                foods = new List<Guid>(),
            };

            var nextState = action.Execute(new ActionContext
            {
                PreviousStates = previousStates,
                Signer = _agentAddress,
                Random = new TestRandom(),
                Rehearsal = false,
                BlockIndex = 1,
            });

            var nextAvatarState = nextState.GetAvatarStateV2(_avatarAddress);
            // 경험치..
            // EventDungeonInfo..
        }
    }
}
