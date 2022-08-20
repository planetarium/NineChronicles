namespace Lib9c.Tests.Action
{
    using System.Linq;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Mail;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Nekoyume.TableData.Event;
    using Xunit;
    using static Lib9c.SerializeKeys;

    public class EventConsumableItemCraftsTest
    {
        private readonly IAccountStateDelta _initialStates;
        private readonly TableSheets _tableSheets;

        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;

        public EventConsumableItemCraftsTest()
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

            for (var i = 0; i < GameConfig.SlotCount; i++)
            {
                var addr = CombinationSlotState.DeriveAddress(_avatarAddress, i);
                const int unlock = GameConfig.RequireClearedStageLevel.CombinationEquipmentAction;
                _initialStates = _initialStates.SetState(
                    addr,
                    new CombinationSlotState(addr, unlock).Serialize());
            }
        }

        [Theory]
        [InlineData(1001, 10010001, 0)]
        public void Execute_Success(
            int eventScheduleId,
            int eventConsumableItemRecipeId,
            int slotIndex)
        {
            Assert.True(_tableSheets.EventScheduleSheet
                .TryGetValue(eventScheduleId, out var scheduleRow));
            var contextBlockIndex = scheduleRow.StartBlockIndex;
            Execute(
                _initialStates,
                eventScheduleId,
                eventConsumableItemRecipeId,
                slotIndex,
                contextBlockIndex);
            contextBlockIndex = scheduleRow.RecipeEndBlockIndex;
            Execute(
                _initialStates,
                eventScheduleId,
                eventConsumableItemRecipeId,
                slotIndex,
                contextBlockIndex);
        }

        private void Execute(
            IAccountStateDelta previousStates,
            int eventScheduleId,
            int eventConsumableItemRecipeId,
            int slotIndex,
            long blockIndex = 0)
        {
            var previousAvatarState = previousStates.GetAvatarStateV2(_avatarAddress);

            var recipeSheet = previousStates.GetSheet<EventConsumableItemRecipeSheet>();
            Assert.True(recipeSheet.TryGetValue(
                eventConsumableItemRecipeId,
                out var recipeRow));
            var materialItemSheet = previousStates.GetSheet<MaterialItemSheet>();
            foreach (var materialInfo in recipeRow.Materials)
            {
                Assert.True(materialItemSheet.TryGetValue(
                    materialInfo.Id,
                    out var materialRow));
                var material =
                    ItemFactory.CreateItem(materialRow, new TestRandom());
                previousAvatarState.inventory.AddItem(material, materialInfo.Count);
            }

            var worldSheet = previousStates.GetSheet<WorldSheet>();
            previousAvatarState.worldInformation = new WorldInformation(
                blockIndex,
                worldSheet,
                GameConfig.RequireClearedStageLevel.CombinationConsumableAction);

            previousStates = previousStates
                .SetState(
                    _avatarAddress.Derive(LegacyInventoryKey),
                    previousAvatarState.inventory.Serialize())
                .SetState(
                    _avatarAddress.Derive(LegacyWorldInformationKey),
                    previousAvatarState.worldInformation.Serialize());

            var previousActionPoint = previousAvatarState.actionPoint;
            var previousResultConsumableCount =
                previousAvatarState.inventory.Equipments
                    .Count(e => e.Id == recipeRow.ResultConsumableItemId);
            var previousMailCount = previousAvatarState.mailBox.Count;

            var action = new EventConsumableItemCrafts
            {
                AvatarAddress = _avatarAddress,
                EventScheduleId = eventScheduleId,
                EventConsumableItemRecipeId = eventConsumableItemRecipeId,
                SlotIndex = slotIndex,
            };

            var nextStates = action.Execute(new ActionContext
            {
                PreviousStates = previousStates,
                Signer = _agentAddress,
                Random = new TestRandom(),
                Rehearsal = false,
                BlockIndex = blockIndex,
            });

            var slotState = nextStates.GetCombinationSlotState(_avatarAddress, slotIndex);
            Assert.NotNull(slotState.Result);
            Assert.NotNull(slotState.Result.itemUsable);

            var consumable = (Consumable)slotState.Result.itemUsable;
            Assert.NotNull(consumable);

            var nextAvatarState = nextStates.GetAvatarStateV2(_avatarAddress);
            Assert.Equal(
                previousActionPoint - recipeRow.RequiredActionPoint,
                nextAvatarState.actionPoint);
            Assert.Equal(
                previousMailCount + 1,
                nextAvatarState.mailBox.Count);
            Assert.IsType<CombinationMail>(nextAvatarState.mailBox.First());
            Assert.Equal(
                previousResultConsumableCount + 1,
                nextAvatarState.inventory.Consumables
                    .Count(e => e.Id == recipeRow.ResultConsumableItemId));
        }
    }
}
