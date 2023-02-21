namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.BlockChain.Policy;
    using Nekoyume.Exceptions;
    using Nekoyume.Extensions;
    using Nekoyume.Model.Event;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Nekoyume.TableData.Event;
    using Xunit;
    using static Lib9c.SerializeKeys;

    public class EventDungeonBattleV1Test
    {
        private readonly Currency _ncgCurrency;
        private readonly TableSheets _tableSheets;

        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private IAccountStateDelta _initialStates;

        public EventDungeonBattleV1Test()
        {
            _initialStates = new State();

#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            _ncgCurrency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
            _initialStates = _initialStates.SetState(
                GoldCurrencyState.Address,
                new GoldCurrencyState(_ncgCurrency).Serialize());
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
        public void Execute_Success_Within_Event_Period(
            int eventScheduleId,
            int eventDungeonId,
            int eventDungeonStageId)
        {
            Assert.True(_tableSheets.EventScheduleSheet
                .TryGetValue(eventScheduleId, out var scheduleRow));
            var contextBlockIndex = scheduleRow.StartBlockIndex;
            var nextStates = Execute(
                _initialStates,
                eventScheduleId,
                eventDungeonId,
                eventDungeonStageId,
                blockIndex: contextBlockIndex);
            var eventDungeonInfoAddr =
                EventDungeonInfo.DeriveAddress(_avatarAddress, eventDungeonId);
            var eventDungeonInfo =
                new EventDungeonInfo(nextStates.GetState(eventDungeonInfoAddr));
            Assert.Equal(
                scheduleRow.DungeonTicketsMax - 1,
                eventDungeonInfo.RemainingTickets);

            contextBlockIndex = scheduleRow.DungeonEndBlockIndex;
            nextStates = Execute(
                _initialStates,
                eventScheduleId,
                eventDungeonId,
                eventDungeonStageId,
                blockIndex: contextBlockIndex);
            eventDungeonInfo =
                new EventDungeonInfo(nextStates.GetState(eventDungeonInfoAddr));
            Assert.Equal(
                scheduleRow.DungeonTicketsMax - 1,
                eventDungeonInfo.RemainingTickets);
        }

        [Theory]
        [InlineData(1001, 10010001, 10010001, 0, 0, 0)]
        [InlineData(1001, 10010001, 10010001, 1, 1, 1)]
        [InlineData(1001, 10010001, 10010001, int.MaxValue, int.MaxValue, int.MaxValue - 1)]
        public void Execute_Success_With_Ticket_Purchase(
            int eventScheduleId,
            int eventDungeonId,
            int eventDungeonStageId,
            int dungeonTicketPrice,
            int dungeonTicketAdditionalPrice,
            int numberOfTicketPurchases)
        {
            var previousStates = _initialStates;
            var scheduleSheet = _tableSheets.EventScheduleSheet;
            Assert.True(scheduleSheet.TryGetValue(eventScheduleId, out var scheduleRow));
            var sb = new StringBuilder();
            sb.AppendLine(
                "id,_name,start_block_index,dungeon_end_block_index,dungeon_tickets_max,dungeon_tickets_reset_interval_block_range,dungeon_exp_seed_value,recipe_end_block_index,dungeon_ticket_price,dungeon_ticket_additional_price");
            sb.AppendLine(
                $"{eventScheduleId}" +
                $",\"2022 Summer Event\"" +
                $",{scheduleRow.StartBlockIndex}" +
                $",{scheduleRow.DungeonEndBlockIndex}" +
                $",{scheduleRow.DungeonTicketsMax}" +
                $",{scheduleRow.DungeonTicketsResetIntervalBlockRange}" +
                $",{dungeonTicketPrice}" +
                $",{dungeonTicketAdditionalPrice}" +
                $",{scheduleRow.DungeonExpSeedValue}" +
                $",{scheduleRow.RecipeEndBlockIndex}");
            previousStates = previousStates.SetState(
                Addresses.GetSheetAddress<EventScheduleSheet>(),
                sb.ToString().Serialize());

            var eventDungeonInfoAddr =
                EventDungeonInfo.DeriveAddress(_avatarAddress, eventDungeonId);
            var eventDungeonInfo = new EventDungeonInfo(
                remainingTickets: 0,
                numberOfTicketPurchases: numberOfTicketPurchases);
            previousStates = previousStates.SetState(
                eventDungeonInfoAddr,
                eventDungeonInfo.Serialize());

            Assert.True(previousStates.GetSheet<EventScheduleSheet>()
                .TryGetValue(eventScheduleId, out var newScheduleRow));
            var ncgHas = newScheduleRow.GetDungeonTicketCostV1(numberOfTicketPurchases);
            previousStates = previousStates.MintAsset(_agentAddress, ncgHas * _ncgCurrency);

            var nextStates = Execute(
                previousStates,
                eventScheduleId,
                eventDungeonId,
                eventDungeonStageId,
                buyTicketIfNeeded: true,
                blockIndex: scheduleRow.StartBlockIndex);
            var nextEventDungeonInfoList =
                (Bencodex.Types.List)nextStates.GetState(eventDungeonInfoAddr)!;
            Assert.Equal(
                numberOfTicketPurchases + 1,
                nextEventDungeonInfoList[2].ToInteger());
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
                    blockIndex: contextBlockIndex));
            contextBlockIndex = scheduleRow.DungeonEndBlockIndex + 1;
            Assert.Throws<InvalidActionFieldException>(() =>
                Execute(
                    _initialStates,
                    eventScheduleId,
                    eventDungeonId,
                    eventDungeonStageId,
                    blockIndex: contextBlockIndex));
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
                    blockIndex: scheduleRow.StartBlockIndex));
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
                    blockIndex: scheduleRow.StartBlockIndex));
        }

        [Theory]
        [InlineData(1001, 10010001, 10010001)]
        public void Execute_Throw_NotEnoughEventDungeonTicketsException(
            int eventScheduleId,
            int eventDungeonId,
            int eventDungeonStageId)
        {
            var previousStates = _initialStates;
            var eventDungeonInfoAddr =
                EventDungeonInfo.DeriveAddress(_avatarAddress, eventDungeonId);
            var eventDungeonInfo = new EventDungeonInfo();
            previousStates = previousStates
                .SetState(eventDungeonInfoAddr, eventDungeonInfo.Serialize());
            Assert.True(_tableSheets.EventScheduleSheet
                .TryGetValue(eventScheduleId, out var scheduleRow));
            Assert.Throws<NotEnoughEventDungeonTicketsException>(() =>
                Execute(
                    previousStates,
                    eventScheduleId,
                    eventDungeonId,
                    eventDungeonStageId,
                    blockIndex: scheduleRow.StartBlockIndex));
        }

        [Theory]
        [InlineData(1001, 10010001, 10010001, 0)]
        [InlineData(1001, 10010001, 10010001, int.MaxValue - 1)]
        public void Execute_Throw_InsufficientBalanceException(
            int eventScheduleId,
            int eventDungeonId,
            int eventDungeonStageId,
            int numberOfTicketPurchases)
        {
            var previousStates = _initialStates;
            var eventDungeonInfoAddr =
                EventDungeonInfo.DeriveAddress(_avatarAddress, eventDungeonId);
            var eventDungeonInfo = new EventDungeonInfo(
                remainingTickets: 0,
                numberOfTicketPurchases: numberOfTicketPurchases);
            previousStates = previousStates
                .SetState(eventDungeonInfoAddr, eventDungeonInfo.Serialize());

            Assert.True(_tableSheets.EventScheduleSheet
                .TryGetValue(eventScheduleId, out var scheduleRow));
            var ncgHas = scheduleRow.GetDungeonTicketCostV1(numberOfTicketPurchases) - 1;
            previousStates = previousStates.MintAsset(_agentAddress, ncgHas * _ncgCurrency);

            Assert.Throws<InsufficientBalanceException>(() =>
                Execute(
                    previousStates,
                    eventScheduleId,
                    eventDungeonId,
                    eventDungeonStageId,
                    buyTicketIfNeeded: true,
                    blockIndex: scheduleRow.StartBlockIndex));
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
                    blockIndex: scheduleRow.StartBlockIndex));
        }

        [Fact]
        public void Execute_V100301()
        {
            int eventScheduleId = 1001;
            int eventDungeonId = 10010001;
            int eventDungeonStageId = 10010001;
            var csv = $@"id,_name,start_block_index,dungeon_end_block_index,dungeon_tickets_max,dungeon_tickets_reset_interval_block_range,dungeon_ticket_price,dungeon_ticket_additional_price,dungeon_exp_seed_value,recipe_end_block_index
            1001,2022 Summer Event,{ActionObsoleteConfig.V100301ExecutedBlockIndex},{ActionObsoleteConfig.V100301ExecutedBlockIndex + 100},5,7200,5,2,1,5018000";
            _initialStates =
                _initialStates.SetState(
                    Addresses.GetSheetAddress<EventScheduleSheet>(),
                    csv.Serialize());
            var sheet = new EventScheduleSheet();
            sheet.Set(csv);
            Assert.True(sheet.TryGetValue(eventScheduleId, out var scheduleRow));
            var contextBlockIndex = scheduleRow.StartBlockIndex;
            var nextStates = Execute(
                _initialStates,
                eventScheduleId,
                eventDungeonId,
                eventDungeonStageId,
                blockIndex: contextBlockIndex);
            var eventDungeonInfoAddr =
                EventDungeonInfo.DeriveAddress(_avatarAddress, eventDungeonId);
            var eventDungeonInfo =
                new EventDungeonInfo(nextStates.GetState(eventDungeonInfoAddr));
            Assert.Equal(
                scheduleRow.DungeonTicketsMax - 1,
                eventDungeonInfo.RemainingTickets);

            contextBlockIndex = scheduleRow.DungeonEndBlockIndex;
            nextStates = Execute(
                _initialStates,
                eventScheduleId,
                eventDungeonId,
                eventDungeonStageId,
                blockIndex: contextBlockIndex);
            eventDungeonInfo =
                new EventDungeonInfo(nextStates.GetState(eventDungeonInfoAddr));
            Assert.Equal(
                scheduleRow.DungeonTicketsMax - 1,
                eventDungeonInfo.RemainingTickets);
        }

        private IAccountStateDelta Execute(
            IAccountStateDelta previousStates,
            int eventScheduleId,
            int eventDungeonId,
            int eventDungeonStageId,
            bool buyTicketIfNeeded = false,
            long blockIndex = 0)
        {
            var previousAvatarState = previousStates.GetAvatarStateV2(_avatarAddress);
            var equipments =
                Doomfist.GetAllParts(_tableSheets, previousAvatarState.level);
            foreach (var equipment in equipments)
            {
                previousAvatarState.inventory.AddItem(equipment, iLock: null);
            }

            var action = new EventDungeonBattleV1
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
                BuyTicketIfNeeded = buyTicketIfNeeded,
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
                eventDungeonStageId.ToEventDungeonStageNumber());
            Assert.Equal(
                previousAvatarState.exp + expectExp,
                nextAvatarState.exp);

            return nextStates;
        }
    }
}
