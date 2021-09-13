namespace Lib9c.Tests.Action
{
    using System.Collections.Generic;
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

    public class CombinationConsumable0Test
    {
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly Address _slotAddress;
        private readonly Dictionary<string, string> _sheets;
        private readonly IRandom _random;
        private readonly TableSheets _tableSheets;
        private readonly AvatarState _avatarState;
        private IAccountStateDelta _initialState;

        public CombinationConsumable0Test()
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

            _initialState = new State()
                .SetState(_agentAddress, agentState.Serialize())
                .SetState(_avatarAddress, _avatarState.Serialize());

            foreach (var (key, value) in _sheets)
            {
                _initialState =
                    _initialState.SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }
        }

        [Fact]
        public void Execute()
        {
            var row = _tableSheets.ConsumableItemRecipeSheet.Values.First();
            foreach (var materialInfo in row.Materials)
            {
                var materialRow = _tableSheets.MaterialItemSheet[materialInfo.Id];
                var material = ItemFactory.CreateItem(materialRow, _random);
                _avatarState.inventory.AddItem2(material, count: materialInfo.Count);
            }

            const int requiredStage = GameConfig.RequireClearedStageLevel.CombinationConsumableAction;
            for (var i = 1; i < requiredStage + 1; i++)
            {
                _avatarState.worldInformation.ClearStage(1, i, 0, _tableSheets.WorldSheet, _tableSheets.WorldUnlockSheet);
            }

            _initialState = _initialState
                .SetState(_avatarAddress, _avatarState.Serialize())
                .SetState(_slotAddress, new CombinationSlotState(_slotAddress, requiredStage).Serialize());

            var action = new CombinationConsumable0()
            {
                AvatarAddress = _avatarAddress,
                recipeId = row.Id,
                slotIndex = 0,
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

            var consumable = (Consumable)slotState.Result.itemUsable;
            Assert.NotNull(consumable);
        }

        [Fact]
        public void ExecuteThrowFailedLoadStateException()
        {
            var action = new CombinationConsumable0()
            {
                AvatarAddress = _avatarAddress,
                recipeId = 1,
                slotIndex = 0,
            };

            Assert.Throws<FailedLoadStateException>(() => action.Execute(new ActionContext()
                {
                    PreviousStates = new State(),
                    Signer = _agentAddress,
                    BlockIndex = 1,
                    Random = _random,
                })
            );
        }

        [Fact]
        public void ExecuteThrowNotEnoughClearedStageLevelException()
        {
            _initialState = _initialState
                .SetState(_slotAddress, new CombinationSlotState(_slotAddress, 0).Serialize());

            var action = new CombinationConsumable0()
            {
                AvatarAddress = _avatarAddress,
                recipeId = 1,
                slotIndex = 0,
            };

            Assert.Throws<NotEnoughClearedStageLevelException>(() => action.Execute(new ActionContext()
                {
                    PreviousStates = _initialState,
                    Signer = _agentAddress,
                    BlockIndex = 1,
                    Random = _random,
                })
            );
        }

        [Fact]
        public void ExecuteThrowCombinationSlotUnlockException()
        {
            const int requiredStage = GameConfig.RequireClearedStageLevel.CombinationConsumableAction;
            for (var i = 1; i < requiredStage + 1; i++)
            {
                _avatarState.worldInformation.ClearStage(1, i, 0, _tableSheets.WorldSheet, _tableSheets.WorldUnlockSheet);
            }

            _initialState = _initialState
                .SetState(_avatarAddress, _avatarState.Serialize())
                .SetState(_slotAddress, new CombinationSlotState(_slotAddress, requiredStage + 10).Serialize());

            var action = new CombinationConsumable0()
            {
                AvatarAddress = _avatarAddress,
                recipeId = 1,
                slotIndex = 0,
            };

            Assert.Throws<CombinationSlotUnlockException>(() => action.Execute(new ActionContext()
                {
                    PreviousStates = _initialState,
                    Signer = _agentAddress,
                    BlockIndex = 1,
                    Random = _random,
                })
            );
        }

        [Fact]
        public void ExecuteThrowSheetRowNotFoundException()
        {
            const int requiredStage = GameConfig.RequireClearedStageLevel.CombinationConsumableAction;
            for (var i = 1; i < requiredStage + 1; i++)
            {
                _avatarState.worldInformation.ClearStage(1, i, 0, _tableSheets.WorldSheet, _tableSheets.WorldUnlockSheet);
            }

            _initialState = _initialState
                .SetState(_avatarAddress, _avatarState.Serialize())
                .SetState(_slotAddress, new CombinationSlotState(_slotAddress, requiredStage).Serialize());

            var action = new CombinationConsumable0()
            {
                AvatarAddress = _avatarAddress,
                recipeId = -1,
                slotIndex = 0,
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
        public void ExecuteThrowNotEnoughMaterialException()
        {
            var row = _tableSheets.ConsumableItemRecipeSheet.Values.First();

            const int requiredStage = GameConfig.RequireClearedStageLevel.CombinationConsumableAction;
            for (var i = 1; i < requiredStage + 1; i++)
            {
                _avatarState.worldInformation.ClearStage(1, i, 0, _tableSheets.WorldSheet, _tableSheets.WorldUnlockSheet);
            }

            _initialState = _initialState
                .SetState(_avatarAddress, _avatarState.Serialize())
                .SetState(_slotAddress, new CombinationSlotState(_slotAddress, requiredStage).Serialize());

            var action = new CombinationConsumable0()
            {
                AvatarAddress = _avatarAddress,
                recipeId = row.Id,
                slotIndex = 0,
            };

            Assert.Throws<NotEnoughMaterialException>(() => action.Execute(new ActionContext()
                {
                    PreviousStates = _initialState,
                    Signer = _agentAddress,
                    BlockIndex = 1,
                    Random = _random,
                })
            );
        }

        [Theory]
        [InlineData(null)]
        [InlineData(1)]
        public void ResultModelDeterministic(int? subRecipeId)
        {
            var row = _tableSheets.MaterialItemSheet.Values.First();
            var row2 = _tableSheets.MaterialItemSheet.Values.Last();

            Assert.True(row.Id < row2.Id);

            var material = ItemFactory.CreateMaterial(row);
            var material2 = ItemFactory.CreateMaterial(row2);

            var itemUsable = ItemFactory.CreateItemUsable(_tableSheets.EquipmentItemSheet.Values.First(), default, 0);
            var result = new CombinationConsumable5.ResultModel()
            {
                id = default,
                gold = 0,
                actionPoint = 0,
                recipeId = 1,
                subRecipeId = subRecipeId,
                materials = new Dictionary<Material, int>()
                {
                    [material] = 1,
                    [material2] = 1,
                },
                itemUsable = itemUsable,
            };

            var result2 = new CombinationConsumable5.ResultModel()
            {
                id = default,
                gold = 0,
                actionPoint = 0,
                recipeId = 1,
                subRecipeId = subRecipeId,
                materials = new Dictionary<Material, int>()
                {
                    [material2] = 1,
                    [material] = 1,
                },
                itemUsable = itemUsable,
            };

            Assert.Equal(result.Serialize(), result2.Serialize());
        }
    }
}
