namespace Lib9c.Tests.ScenarioTests
{
    using System;
    using System.Globalization;
    using System.Linq;
    using Bencodex.Types;
    using Lib9c.Tests.Action;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;
    using static SerializeKeys;

    public class CombinationAndRapidCombinationTest
    {
        private readonly IAccountStateDelta _initialState;
        private readonly TableSheets _tableSheets;
        private Address _agentAddress;
        private Address _avatarAddress;
        private Address _inventoryAddress;
        private Address _worldInformationAddress;
        private Address _questListAddress;
        private Address _slot0Address;

        public CombinationAndRapidCombinationTest(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();

            var sheets = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(sheets);

            var gold = new GoldCurrencyState(new Currency("NCG", 2, minter: null));
            var gameConfigState = new GameConfigState(sheets[nameof(GameConfigSheet)]);

            _agentAddress = new PrivateKey().ToAddress();
            _avatarAddress = _agentAddress.Derive("avatar");
            _slot0Address = _avatarAddress.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CombinationSlotState.DeriveFormat,
                    0
                )
            );
            var slot0State = new CombinationSlotState(
                _slot0Address,
                GameConfig.RequireClearedStageLevel.CombinationEquipmentAction);

            var agentState = new AgentState(_agentAddress);
            agentState.avatarAddresses[0] = _avatarAddress;

            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                1,
                _tableSheets.GetAvatarSheets(),
                gameConfigState,
                default
            )
            {
                worldInformation = new WorldInformation(
                    0,
                    _tableSheets.WorldSheet,
                    GameConfig.RequireClearedStageLevel.CombinationEquipmentAction),
            };

            _inventoryAddress = _avatarAddress.Derive(LegacyInventoryKey);
            _worldInformationAddress = _avatarAddress.Derive(LegacyWorldInformationKey);
            _questListAddress = _avatarAddress.Derive(LegacyQuestListKey);

            _initialState = new Tests.Action.State()
                .SetState(GoldCurrencyState.Address, gold.Serialize())
                .SetState(gameConfigState.address, gameConfigState.Serialize())
                .SetState(_agentAddress, agentState.Serialize())
                .SetState(_avatarAddress, avatarState.SerializeV2())
                .SetState(_inventoryAddress, avatarState.inventory.Serialize())
                .SetState(_worldInformationAddress, avatarState.worldInformation.Serialize())
                .SetState(_questListAddress, avatarState.questList.Serialize())
                .SetState(_slot0Address, slot0State.Serialize());

            foreach (var (key, value) in sheets)
            {
                _initialState = _initialState
                    .SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(12)]
        [InlineData(2)]
        [InlineData(9)]
        [InlineData(17)]
        [InlineData(13)]
        [InlineData(180)]
        [InlineData(160)]
        public void Case(int randomSeed)
        {
            var gameConfigState = _initialState.GetGameConfigState();
            Assert.NotNull(gameConfigState);

            var recipeRow = _tableSheets.EquipmentItemRecipeSheet.OrderedList.First(e => e.SubRecipeIds.Any());
            var subRecipeRow = _tableSheets.EquipmentItemSubRecipeSheetV2[recipeRow.SubRecipeIds.First()];
            var combinationEquipmentAction = new CombinationEquipment
            {
                avatarAddress = _avatarAddress,
                slotIndex = 0,
                recipeId = recipeRow.Id,
                subRecipeId = subRecipeRow.Id,
            };

            var inventoryValue = _initialState.GetState(_inventoryAddress);
            Assert.NotNull(inventoryValue);
            var inventoryState = new Inventory((List)inventoryValue);
            inventoryState.AddFungibleItem(
                ItemFactory.CreateMaterial(_tableSheets.MaterialItemSheet, recipeRow.MaterialId),
                recipeRow.MaterialCount);
            foreach (var materialInfo in subRecipeRow.Materials)
            {
                inventoryState.AddFungibleItem(
                    ItemFactory.CreateMaterial(_tableSheets.MaterialItemSheet, materialInfo.Id),
                    materialInfo.Count);
            }

            var worldInformation = new WorldInformation(
                0,
                _tableSheets.WorldSheet,
                recipeRow.UnlockStage);

            var nextState = _initialState
                .SetState(_inventoryAddress, inventoryState.Serialize())
                .SetState(_worldInformationAddress, worldInformation.Serialize());

            var random = new TestRandom(randomSeed);
            nextState = combinationEquipmentAction.Execute(new ActionContext
            {
                PreviousStates = nextState,
                BlockIndex = 0,
                Random = random,
                Signer = _agentAddress,
            });

            var slot0Value = nextState.GetState(_slot0Address);
            Assert.NotNull(slot0Value);
            var slot0State = new CombinationSlotState((Dictionary)slot0Value);
            Assert.NotNull(slot0State.Result.itemUsable);
        }
    }
}
