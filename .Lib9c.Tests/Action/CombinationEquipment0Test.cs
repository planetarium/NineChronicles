namespace Lib9c.Tests.Action
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class CombinationEquipment0Test
    {
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly Address _slotAddress;
        private readonly Dictionary<string, string> _sheets;
        private readonly TableSheets _tableSheets;
        private readonly IRandom _random;
        private readonly AvatarState _avatarState;
        private IAccountStateDelta _initialState;

        public CombinationEquipment0Test()
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
            _random = new TestRandom();
            _tableSheets = new TableSheets(_sheets);
            var agentState = new AgentState(_agentAddress);
            agentState.avatarAddresses[0] = _avatarAddress;

            var gameConfigState = new GameConfigState();
            _avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                1,
                _tableSheets.GetAvatarSheets(),
                gameConfigState,
                default
            );
#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            var gold = new GoldCurrencyState(Currency.Legacy("NCG", 2, null));
#pragma warning restore CS0618

            _initialState = new State()
                .SetState(_agentAddress, agentState.Serialize())
                .SetState(_avatarAddress, _avatarState.Serialize())
                .SetState(
                    _slotAddress,
                    new CombinationSlotState(
                        _slotAddress,
                        GameConfig.RequireClearedStageLevel.CombinationEquipmentAction
                    ).Serialize())
                .SetState(GoldCurrencyState.Address, gold.Serialize())
                .MintAsset(GoldCurrencyState.Address, gold.Currency * 100000000000);

            foreach (var (key, value) in _sheets)
            {
                _initialState =
                    _initialState.SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }
        }

        [Fact]
        public void Execute()
        {
            var row = _tableSheets.EquipmentItemRecipeSheet.Values.First();
            var materialRow = _tableSheets.MaterialItemSheet[row.MaterialId];
            var material = ItemFactory.CreateItem(materialRow, _random);
            _avatarState.inventory.AddItem2(material, count: row.MaterialCount);

            const int requiredStage = GameConfig.RequireClearedStageLevel.CombinationEquipmentAction;
            for (var i = 1; i < requiredStage + 1; i++)
            {
                _avatarState.worldInformation.ClearStage(
                    1,
                    i,
                    0,
                    _tableSheets.WorldSheet,
                    _tableSheets.WorldUnlockSheet
                );
            }

            _initialState = _initialState.SetState(_avatarAddress, _avatarState.Serialize());

            var action = new CombinationEquipment0()
            {
                AvatarAddress = _avatarAddress,
                RecipeId = row.Id,
                SlotIndex = 0,
            };

            var nextState = action.Execute(new ActionContext()
            {
                PreviousStates = _initialState,
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
            var rowList = _tableSheets.EquipmentItemRecipeSheet.Values.ToList();
            var row = rowList[1];
            var subRecipeId = row.SubRecipeIds.First();
            var materialRow = _tableSheets.MaterialItemSheet[row.MaterialId];
            var material = ItemFactory.CreateItem(materialRow, _random);
            _avatarState.inventory.AddItem2(material, count: row.MaterialCount);

            var subRecipeRow = _tableSheets.EquipmentItemSubRecipeSheet.Values.First(r => r.Id == subRecipeId);
            foreach (var materialInfo in subRecipeRow.Materials)
            {
                materialRow = _tableSheets.MaterialItemSheet[materialInfo.Id];
                material = ItemFactory.CreateItem(materialRow, _random);
                _avatarState.inventory.AddItem2(material, count: materialInfo.Count);
            }

            for (var i = 1; i < row.UnlockStage + 1; i++)
            {
                _avatarState.worldInformation.ClearStage(
                    1,
                    i,
                    0,
                    _tableSheets.WorldSheet,
                    _tableSheets.WorldUnlockSheet
                );
            }

            _initialState = _initialState.SetState(_avatarAddress, _avatarState.Serialize());

            var action = new CombinationEquipment0()
            {
                AvatarAddress = _avatarAddress,
                RecipeId = row.Id,
                SubRecipeId = subRecipeId,
                SlotIndex = 0,
            };

            var nextState = action.Execute(new ActionContext()
            {
                PreviousStates = _initialState,
                Signer = _agentAddress,
                BlockIndex = 1,
                Random = _random,
            });

            var slotState = nextState.GetCombinationSlotState(_avatarAddress, 0);

            Assert.NotNull(slotState.Result);
            Assert.NotNull(slotState.Result.itemUsable);
        }

        [Fact]
        public void ExecuteThrowFailedLoadStateException()
        {
            var action = new CombinationEquipment0()
            {
                AvatarAddress = _avatarAddress,
                RecipeId = 1,
                SubRecipeId = 1,
                SlotIndex = 0,
            };

            Assert.Throws<FailedLoadStateException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = new State(),
                Signer = _agentAddress,
                Random = new TestRandom(),
            }));
        }

        [Fact]
        public void ExecuteThrowCombinationSlotUnlockException()
        {
            var row = _tableSheets.EquipmentItemRecipeSheet.Values.First(r => r.SubRecipeIds.Any());
            var subRecipeId = row.SubRecipeIds.First();
            var materialRow = _tableSheets.MaterialItemSheet[row.MaterialId];
            var material = ItemFactory.CreateItem(materialRow, _random);
            _avatarState.inventory.AddItem2(material, count: row.MaterialCount);

            var subRecipeRow = _tableSheets.EquipmentItemSubRecipeSheetV2.Values.First(r => r.Id == subRecipeId);
            foreach (var materialInfo in subRecipeRow.Materials)
            {
                materialRow = _tableSheets.MaterialItemSheet[materialInfo.Id];
                material = ItemFactory.CreateItem(materialRow, _random);
                _avatarState.inventory.AddItem2(material, count: materialInfo.Count);
            }

            for (var i = 1; i < row.UnlockStage + 1; i++)
            {
                _avatarState.worldInformation.ClearStage(
                    1,
                    i,
                    0,
                    _tableSheets.WorldSheet,
                    _tableSheets.WorldUnlockSheet
                );
            }

            _initialState = _initialState
                .SetState(_avatarAddress, _avatarState.Serialize())
                .SetState(
                    _slotAddress,
                    new CombinationSlotState(_slotAddress, row.UnlockStage + 10).Serialize()
                );

            var action = new CombinationEquipment0()
            {
                AvatarAddress = _avatarAddress,
                RecipeId = row.Id,
                SubRecipeId = subRecipeId,
                SlotIndex = 0,
            };

            Assert.Throws<CombinationSlotUnlockException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = _initialState,
                Signer = _agentAddress,
                Random = new TestRandom(),
            }));
        }

        [Fact]
        public void ExecuteThrowSheetRowNotFoundException()
        {
            var row = _tableSheets.EquipmentItemRecipeSheet.Values.First();
            var materialRow = _tableSheets.MaterialItemSheet[row.MaterialId];
            var material = ItemFactory.CreateItem(materialRow, _random);
            _avatarState.inventory.AddItem2(material, count: row.MaterialCount);

            const int requiredStage = GameConfig.RequireClearedStageLevel.CombinationEquipmentAction;
            for (var i = 1; i < requiredStage + 1; i++)
            {
                _avatarState.worldInformation.ClearStage(
                    1,
                    i,
                    0,
                    _tableSheets.WorldSheet,
                    _tableSheets.WorldUnlockSheet
                );
            }

            _initialState = _initialState.SetState(_avatarAddress, _avatarState.Serialize());

            var action = new CombinationEquipment0()
            {
                AvatarAddress = _avatarAddress,
                RecipeId = 999,
                SlotIndex = 0,
            };

            Assert.Throws<SheetRowNotFoundException>(() => action.Execute(new ActionContext()
                {
                    PreviousStates = _initialState,
                    Signer = _agentAddress,
                    BlockIndex = 1,
                    Random = _random,
                })
            );
        }

        [Fact]
        public void ExecuteThrowSheetRowColumnException()
        {
            var row = _tableSheets.EquipmentItemRecipeSheet.Values.First(r => r.SubRecipeIds.Any());
            var subRecipeId = row.SubRecipeIds.First();
            var materialRow = _tableSheets.MaterialItemSheet[row.MaterialId];
            var material = ItemFactory.CreateItem(materialRow, _random);
            _avatarState.inventory.AddItem2(material, count: row.MaterialCount);

            var subRecipeRow = _tableSheets.EquipmentItemSubRecipeSheetV2.Values.First(r => r.Id == subRecipeId);
            foreach (var materialInfo in subRecipeRow.Materials)
            {
                materialRow = _tableSheets.MaterialItemSheet[materialInfo.Id];
                material = ItemFactory.CreateItem(materialRow, _random);
                _avatarState.inventory.AddItem2(material, count: materialInfo.Count);
            }

            for (var i = 1; i < row.UnlockStage + 1; i++)
            {
                _avatarState.worldInformation.ClearStage(
                    1,
                    i,
                    0,
                    _tableSheets.WorldSheet,
                    _tableSheets.WorldUnlockSheet
                );
            }

            _initialState = _initialState.SetState(_avatarAddress, _avatarState.Serialize());

            var action = new CombinationEquipment0()
            {
                AvatarAddress = _avatarAddress,
                RecipeId = row.Id,
                SubRecipeId = 100,
                SlotIndex = 0,
            };

            Assert.Throws<SheetRowColumnException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = _initialState,
                Signer = _agentAddress,
                Random = new TestRandom(),
            }));
        }

        [Fact]
        public void ExecuteThrowNotEnoughClearedStageLevelException()
        {
            const int requiredStage = GameConfig.RequireClearedStageLevel.CombinationEquipmentAction;
            var row = _tableSheets.EquipmentItemRecipeSheet.Values.First(r => r.UnlockStage > requiredStage);
            var materialRow = _tableSheets.MaterialItemSheet[row.MaterialId];
            var material = ItemFactory.CreateItem(materialRow, _random);
            _avatarState.inventory.AddItem2(material, count: row.MaterialCount);

            for (var i = 1; i < requiredStage + 1; i++)
            {
                _avatarState.worldInformation.ClearStage(
                    1,
                    i,
                    0,
                    _tableSheets.WorldSheet,
                    _tableSheets.WorldUnlockSheet
                );
            }

            _initialState = _initialState.SetState(_avatarAddress, _avatarState.Serialize());

            var action = new CombinationEquipment0()
            {
                AvatarAddress = _avatarAddress,
                RecipeId = row.Id,
                SlotIndex = 0,
            };

            Assert.Throws<NotEnoughClearedStageLevelException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = _initialState,
                Signer = _agentAddress,
                Random = new TestRandom(),
            }));
        }

        [Fact]
        public void ExecuteThrowNotEnoughMaterialException()
        {
            var row = _tableSheets.EquipmentItemRecipeSheet.Values.First();

            const int requiredStage = GameConfig.RequireClearedStageLevel.CombinationEquipmentAction;
            for (var i = 1; i < requiredStage + 1; i++)
            {
                _avatarState.worldInformation.ClearStage(
                    1,
                    i,
                    0,
                    _tableSheets.WorldSheet,
                    _tableSheets.WorldUnlockSheet
                );
            }

            _initialState = _initialState.SetState(_avatarAddress, _avatarState.Serialize());

            var action = new CombinationEquipment0()
            {
                AvatarAddress = _avatarAddress,
                RecipeId = row.Id,
                SlotIndex = 0,
            };

            Assert.Throws<NotEnoughMaterialException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = _initialState,
                Signer = _agentAddress,
                Random = new TestRandom(),
            }));
        }
    }
}
