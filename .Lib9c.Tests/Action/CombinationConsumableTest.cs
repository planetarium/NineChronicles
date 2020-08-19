namespace Lib9c.Tests.Action
{
    using System.Globalization;
    using System.Linq;
    using Libplanet;
    using Libplanet.Action;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class CombinationConsumableTest
    {
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly Address _slotAddress;
        private readonly TableSheetsState _tableSheetsState;
        private readonly TableSheets _tableSheets;
        private readonly IRandom _random;

        public CombinationConsumableTest()
        {
            _agentAddress = default;
            _avatarAddress = _agentAddress.Derive("avatar");
            _slotAddress = _avatarAddress.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CombinationSlotState.DeriveFormat,
                    0
                )
            );
            _tableSheetsState = TableSheetsImporter.ImportTableSheets();
            _tableSheets = TableSheets.FromTableSheetsState(_tableSheetsState);
            _random = new ItemEnhancementTest.TestRandom();
        }

        [Fact]
        public void Execute()
        {
            var agentState = new AgentState(_agentAddress);
            agentState.avatarAddresses[0] = _avatarAddress;

            var gameConfigState = new GameConfigState();
            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                1,
                _tableSheets,
                gameConfigState
            );
            var row = _tableSheets.ConsumableItemRecipeSheet.Values.First();
            foreach (var materialInfo in row.Materials)
            {
                var materialRow = _tableSheets.MaterialItemSheet[materialInfo.Id];
                var material = ItemFactory.CreateItem(materialRow);
                avatarState.inventory.AddItem(material, materialInfo.Count);
            }

            const int requiredStage = GameConfig.RequireClearedStageLevel.CombinationConsumableAction;
            for (var i = 1; i < requiredStage + 1; i++)
            {
                avatarState.worldInformation.ClearStage(1, i, 0, _tableSheets.WorldSheet, _tableSheets.WorldUnlockSheet);
            }

            var initialState = new State()
                .SetState(_agentAddress, agentState.Serialize())
                .SetState(_avatarAddress, avatarState.Serialize())
                .SetState(_slotAddress, new CombinationSlotState(_slotAddress, requiredStage).Serialize())
                .SetState(TableSheetsState.Address, _tableSheetsState.Serialize());

            var action = new CombinationConsumable()
            {
                AvatarAddress = _avatarAddress,
                recipeId = row.Id,
                slotIndex = 0,
            };

            var nextState = action.Execute(new ActionContext()
            {
                PreviousStates = initialState,
                Signer = _agentAddress,
                BlockIndex = 1,
                Random = _random,
            });

            var slotState = nextState.GetCombinationSlotState(_avatarAddress, 0);

            Assert.NotNull(slotState.Result);

            var consumable = (Consumable)slotState.Result.itemUsable;
            Assert.NotNull(consumable);
        }
    }
}
