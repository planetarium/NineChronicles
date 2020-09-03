namespace Lib9c.Tests.Action
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class CombinationEquipmentTest
    {
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly Address _slotAddress;
        private readonly Dictionary<string, string> _sheets;
        private readonly TableSheets _tableSheets;
        private readonly IRandom _random;

        public CombinationEquipmentTest()
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
            _sheets = TableSheetsImporter.ImportSheets();
            _random = new ItemEnhancementTest.TestRandom();
            _tableSheets = new TableSheets(_sheets);
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
                _tableSheets.WorldSheet,
                _tableSheets.QuestSheet,
                _tableSheets.QuestRewardSheet,
                _tableSheets.QuestItemRewardSheet,
                _tableSheets.EquipmentItemRecipeSheet,
                _tableSheets.EquipmentItemSubRecipeSheet,
                gameConfigState
            );
            var row = _tableSheets.EquipmentItemRecipeSheet.Values.First();
            var materialRow = _tableSheets.MaterialItemSheet[row.MaterialId];
            var material = ItemFactory.CreateItem(materialRow);
            avatarState.inventory.AddItem(material, row.MaterialCount);

            const int requiredStage = GameConfig.RequireClearedStageLevel.CombinationEquipmentAction;
            for (var i = 1; i < requiredStage + 1; i++)
            {
                avatarState.worldInformation.ClearStage(1, i, 0, _tableSheets.WorldSheet, _tableSheets.WorldUnlockSheet);
            }

            var gold = new GoldCurrencyState(new Currency("NCG", 2, minter: null));

            var initialState = new State()
                .SetState(_agentAddress, agentState.Serialize())
                .SetState(_avatarAddress, avatarState.Serialize())
                .SetState(_slotAddress, new CombinationSlotState(_slotAddress, requiredStage).Serialize())
                .SetState(GoldCurrencyState.Address, gold.Serialize())
                .MintAsset(GoldCurrencyState.Address, gold.Currency * 100000000000);

            foreach (var (key, value) in _sheets)
            {
                initialState =
                    initialState.SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            var action = new CombinationEquipment()
            {
                AvatarAddress = _avatarAddress,
                RecipeId = row.Id,
                SlotIndex = 0,
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
            Assert.NotNull(slotState.Result.itemUsable);
        }

        [Fact]
        public void ExecuteWithSubRecipe()
        {
            var agentState = new AgentState(_agentAddress);
            agentState.avatarAddresses[0] = _avatarAddress;

            var gameConfigState = new GameConfigState();
            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                1,
                _tableSheets.WorldSheet,
                _tableSheets.QuestSheet,
                _tableSheets.QuestRewardSheet,
                _tableSheets.QuestItemRewardSheet,
                _tableSheets.EquipmentItemRecipeSheet,
                _tableSheets.EquipmentItemSubRecipeSheet,
                gameConfigState
            );
            var row = _tableSheets.EquipmentItemRecipeSheet.Values.First(r => r.SubRecipeIds.Any());
            var subRecipeId = row.SubRecipeIds.First();
            var materialRow = _tableSheets.MaterialItemSheet[row.MaterialId];
            var material = ItemFactory.CreateItem(materialRow);
            avatarState.inventory.AddItem(material, row.MaterialCount);

            var subRecipeRow = _tableSheets.EquipmentItemSubRecipeSheet.Values.First(r => r.Id == subRecipeId);
            foreach (var materialInfo in subRecipeRow.Materials)
            {
                materialRow = _tableSheets.MaterialItemSheet[materialInfo.Id];
                material = ItemFactory.CreateItem(materialRow);
                avatarState.inventory.AddItem(material, materialInfo.Count);
            }

            for (var i = 1; i < row.UnlockStage + 1; i++)
            {
                avatarState.worldInformation.ClearStage(1, i, 0, _tableSheets.WorldSheet, _tableSheets.WorldUnlockSheet);
            }

            var gold = new GoldCurrencyState(new Currency("NCG", 2, minter: null));

            var initialState = new State()
                .SetState(_agentAddress, agentState.Serialize())
                .SetState(_avatarAddress, avatarState.Serialize())
                .SetState(_slotAddress, new CombinationSlotState(_slotAddress, 1).Serialize())
                .SetState(GoldCurrencyState.Address, gold.Serialize())
                .MintAsset(GoldCurrencyState.Address, gold.Currency * 100000000000);

            foreach (var (key, value) in _sheets)
            {
                initialState =
                    initialState.SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            var action = new CombinationEquipment()
            {
                AvatarAddress = _avatarAddress,
                RecipeId = row.Id,
                SubRecipeId = subRecipeId,
                SlotIndex = 0,
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
            Assert.NotNull(slotState.Result.itemUsable);
        }
    }
}
