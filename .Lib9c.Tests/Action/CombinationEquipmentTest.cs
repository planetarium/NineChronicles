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
        }

        [Fact]
        public void Execute()
        {
            var agentState = new AgentState(_agentAddress);
            agentState.avatarAddresses[0] = _avatarAddress;

            var worldSheet = new WorldSheet();
            worldSheet.Set(_sheets[nameof(WorldSheet)]);
            var questRewardSheet = new QuestRewardSheet();
            questRewardSheet.Set(_sheets[nameof(QuestRewardSheet)]);
            var questItemRewardSheet = new QuestItemRewardSheet();
            questItemRewardSheet.Set(_sheets[nameof(QuestItemRewardSheet)]);
            var equipmentItemRecipeSheet = new EquipmentItemRecipeSheet();
            equipmentItemRecipeSheet.Set(_sheets[nameof(EquipmentItemRecipeSheet)]);
            var equipmentItemSubRecipeSheet = new EquipmentItemSubRecipeSheet();
            equipmentItemSubRecipeSheet.Set(_sheets[nameof(EquipmentItemSubRecipeSheet)]);
            var questSheet = new QuestSheet();
            questSheet.Set(_sheets[nameof(GeneralQuestSheet)]);
            var characterSheet = new CharacterSheet();
            characterSheet.Set(_sheets[nameof(CharacterSheet)]);
            var recipeSheet = new EquipmentItemRecipeSheet();
            recipeSheet.Set(_sheets[nameof(EquipmentItemRecipeSheet)]);
            var materialItemSheet = new MaterialItemSheet();
            materialItemSheet.Set(_sheets[nameof(MaterialItemSheet)]);
            var worldUnlockSheet = new WorldUnlockSheet();
            worldUnlockSheet.Set(_sheets[nameof(WorldUnlockSheet)]);

            var gameConfigState = new GameConfigState();
            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                1,
                worldSheet,
                questSheet,
                questRewardSheet,
                questItemRewardSheet,
                equipmentItemRecipeSheet,
                equipmentItemSubRecipeSheet,
                gameConfigState
            );
            var row = recipeSheet.Values.First();
            var materialRow = materialItemSheet[row.MaterialId];
            var material = ItemFactory.CreateItem(materialRow);
            avatarState.inventory.AddItem(material, row.MaterialCount);

            const int requiredStage = GameConfig.RequireClearedStageLevel.CombinationEquipmentAction;
            for (var i = 1; i < requiredStage + 1; i++)
            {
                avatarState.worldInformation.ClearStage(1, i, 0, worldSheet, worldUnlockSheet);
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
                    initialState.SetState(Addresses.TableSheet.Derive(key), Dictionary.Empty.Add("csv", value));
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

            var worldSheet = new WorldSheet();
            worldSheet.Set(_sheets[nameof(WorldSheet)]);
            var questRewardSheet = new QuestRewardSheet();
            questRewardSheet.Set(_sheets[nameof(QuestRewardSheet)]);
            var questItemRewardSheet = new QuestItemRewardSheet();
            questItemRewardSheet.Set(_sheets[nameof(QuestItemRewardSheet)]);
            var equipmentItemRecipeSheet = new EquipmentItemRecipeSheet();
            equipmentItemRecipeSheet.Set(_sheets[nameof(EquipmentItemRecipeSheet)]);
            var equipmentItemSubRecipeSheet = new EquipmentItemSubRecipeSheet();
            equipmentItemSubRecipeSheet.Set(_sheets[nameof(EquipmentItemSubRecipeSheet)]);
            var questSheet = new QuestSheet();
            questSheet.Set(_sheets[nameof(GeneralQuestSheet)]);
            var characterSheet = new CharacterSheet();
            characterSheet.Set(_sheets[nameof(CharacterSheet)]);
            var recipeSheet = new EquipmentItemRecipeSheet();
            recipeSheet.Set(_sheets[nameof(EquipmentItemRecipeSheet)]);
            var materialItemSheet = new MaterialItemSheet();
            materialItemSheet.Set(_sheets[nameof(MaterialItemSheet)]);
            var worldUnlockSheet = new WorldUnlockSheet();
            worldUnlockSheet.Set(_sheets[nameof(WorldUnlockSheet)]);

            var gameConfigState = new GameConfigState();
            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                1,
                worldSheet,
                questSheet,
                questRewardSheet,
                questItemRewardSheet,
                equipmentItemRecipeSheet,
                equipmentItemSubRecipeSheet,
                gameConfigState
            );
            var row = recipeSheet.Values.First(r => r.SubRecipeIds.Any());
            var subRecipeId = row.SubRecipeIds.First();
            var materialRow = materialItemSheet[row.MaterialId];
            var material = ItemFactory.CreateItem(materialRow);
            avatarState.inventory.AddItem(material, row.MaterialCount);

            var subRecipeRow = equipmentItemSubRecipeSheet.Values.First(r => r.Id == subRecipeId);
            foreach (var materialInfo in subRecipeRow.Materials)
            {
                materialRow = materialItemSheet[materialInfo.Id];
                material = ItemFactory.CreateItem(materialRow);
                avatarState.inventory.AddItem(material, materialInfo.Count);
            }

            for (var i = 1; i < row.UnlockStage + 1; i++)
            {
                avatarState.worldInformation.ClearStage(1, i, 0, worldSheet, worldUnlockSheet);
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
                    initialState.SetState(Addresses.TableSheet.Derive(key), Dictionary.Empty.Add("csv", value));
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
