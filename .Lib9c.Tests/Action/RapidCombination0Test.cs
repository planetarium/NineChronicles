namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using Bencodex.Types;
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
    using Xunit;

    public class RapidCombination0Test
    {
        private readonly IAccountStateDelta _initialState;

        private readonly TableSheets _tableSheets;

        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;

        public RapidCombination0Test()
        {
            _initialState = new State();

            var sheets = TableSheetsImporter.ImportSheets();
            foreach (var (key, value) in sheets)
            {
                _initialState = _initialState.SetState(
                    Addresses.TableSheet.Derive(key),
                    value.Serialize());
            }

            _tableSheets = new TableSheets(sheets);

            _agentAddress = new PrivateKey().ToAddress();
            var agentState = new AgentState(_agentAddress);

            _avatarAddress = new PrivateKey().ToAddress();
            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                default
            );

            agentState.avatarAddresses[0] = _avatarAddress;

            _initialState = _initialState
                .SetState(Addresses.GameConfig, new GameConfigState(sheets[nameof(GameConfigSheet)]).Serialize())
                .SetState(_agentAddress, agentState.Serialize())
                .SetState(_avatarAddress, avatarState.Serialize());
        }

        [Fact]
        public void Execute()
        {
            const int slotStateUnlockStage = 1;

            var avatarState = _initialState.GetAvatarState(_avatarAddress);
            avatarState.worldInformation = new WorldInformation(
                0,
                _initialState.GetSheet<WorldSheet>(),
                slotStateUnlockStage);

            var material = ItemFactory.CreateMaterial(
                _tableSheets.MaterialItemSheet.Values.First(r =>
                    r.ItemSubType == ItemSubType.Hourglass));
            avatarState.inventory.AddItem2(material);

            var firstEquipmentRow = _tableSheets.EquipmentItemSheet.First;
            Assert.NotNull(firstEquipmentRow);

            var gameConfigState = _initialState.GetGameConfigState();
            var requiredBlockIndex = gameConfigState.HourglassPerBlock;
            var equipment = (Equipment)ItemFactory.CreateItemUsable(
                firstEquipmentRow,
                Guid.NewGuid(),
                requiredBlockIndex);
            avatarState.inventory.AddItem2(equipment);

            var result = new CombinationConsumable5.ResultModel
            {
                actionPoint = 0,
                gold = 0,
                materials = new Dictionary<Material, int>(),
                itemUsable = equipment,
                recipeId = 0,
                itemType = ItemType.Equipment,
            };

            var mail = new CombinationMail(result, 0, default, requiredBlockIndex);
            result.id = mail.id;
            avatarState.Update2(mail);

            var slotAddress = _avatarAddress.Derive(string.Format(
                CultureInfo.InvariantCulture,
                CombinationSlotState.DeriveFormat,
                0));
            var slotState = new CombinationSlotState(slotAddress, slotStateUnlockStage);
            slotState.Update(result, 0, 0);

            var tempState = _initialState
                .SetState(_avatarAddress, avatarState.Serialize())
                .SetState(slotAddress, slotState.Serialize());

            var action = new RapidCombination0
            {
                avatarAddress = _avatarAddress,
                slotIndex = 0,
            };

            var nextState = action.Execute(new ActionContext
            {
                PreviousStates = tempState,
                Signer = _agentAddress,
                BlockIndex = 1,
            });

            var nextAvatarState = nextState.GetAvatarState(_avatarAddress);
            var item = nextAvatarState.inventory.Equipments.First();

            Assert.Empty(nextAvatarState.inventory.Materials.Select(r => r.ItemSubType == ItemSubType.Hourglass));
            Assert.Equal(equipment.ItemId, item.ItemId);
            Assert.Equal(1, item.RequiredBlockIndex);
        }

        [Fact]
        public void ExecuteThrowCombinationSlotResultNullException()
        {
            var slotAddress = _avatarAddress.Derive(string.Format(
                CultureInfo.InvariantCulture,
                CombinationSlotState.DeriveFormat,
                0));
            var slotState = new CombinationSlotState(slotAddress, 0);
            slotState.Update(null, 0, 0);

            var tempState = _initialState
                .SetState(slotAddress, slotState.Serialize());

            var action = new RapidCombination0
            {
                avatarAddress = _avatarAddress,
                slotIndex = 0,
            };

            Assert.Throws<CombinationSlotResultNullException>(() => action.Execute(new ActionContext
            {
                PreviousStates = tempState,
                Signer = _agentAddress,
                BlockIndex = 1,
            }));
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(1, 2)]
        public void ExecuteThrowCombinationSlotUnlockException(int avatarClearedStage, int slotStateUnlockStage)
        {
            var avatarState = _initialState.GetAvatarState(_avatarAddress);
            avatarState.worldInformation = new WorldInformation(
                0,
                _initialState.GetSheet<WorldSheet>(),
                avatarClearedStage);

            var firstEquipmentRow = _tableSheets.EquipmentItemSheet.First;
            Assert.NotNull(firstEquipmentRow);

            var equipment = (Equipment)ItemFactory.CreateItemUsable(
                firstEquipmentRow,
                Guid.NewGuid(),
                0);

            var result = new CombinationConsumable5.ResultModel
            {
                actionPoint = 0,
                gold = 0,
                materials = new Dictionary<Material, int>(),
                itemUsable = equipment,
                recipeId = 0,
                itemType = ItemType.Equipment,
            };

            var slotAddress = _avatarAddress.Derive(string.Format(
                CultureInfo.InvariantCulture,
                CombinationSlotState.DeriveFormat,
                0));
            var slotState = new CombinationSlotState(slotAddress, slotStateUnlockStage);
            slotState.Update(result, 0, 0);

            var tempState = _initialState
                .SetState(_avatarAddress, avatarState.Serialize())
                .SetState(slotAddress, slotState.Serialize());

            var action = new RapidCombination0
            {
                avatarAddress = _avatarAddress,
                slotIndex = 0,
            };

            Assert.Throws<CombinationSlotUnlockException>(() => action.Execute(new ActionContext
            {
                PreviousStates = tempState,
                Signer = _agentAddress,
                BlockIndex = 1,
            }));
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(10, 100)]
        public void ExecuteThrowRequiredBlockIndexException(int itemRequiredBlockIndex, int contextBlockIndex)
        {
            const int avatarClearedStage = 1;

            var avatarState = _initialState.GetAvatarState(_avatarAddress);
            avatarState.worldInformation = new WorldInformation(
                0,
                _initialState.GetSheet<WorldSheet>(),
                avatarClearedStage);

            var firstEquipmentRow = _tableSheets.EquipmentItemSheet.First;
            Assert.NotNull(firstEquipmentRow);

            var equipment = (Equipment)ItemFactory.CreateItemUsable(
                firstEquipmentRow,
                Guid.NewGuid(),
                itemRequiredBlockIndex);

            var result = new CombinationConsumable5.ResultModel
            {
                actionPoint = 0,
                gold = 0,
                materials = new Dictionary<Material, int>(),
                itemUsable = equipment,
                recipeId = 0,
                itemType = ItemType.Equipment,
            };

            var slotAddress = _avatarAddress.Derive(string.Format(
                CultureInfo.InvariantCulture,
                CombinationSlotState.DeriveFormat,
                0));
            var slotState = new CombinationSlotState(slotAddress, avatarClearedStage);
            slotState.Update(result, 0, 0);

            var tempState = _initialState
                .SetState(_avatarAddress, avatarState.Serialize())
                .SetState(slotAddress, slotState.Serialize());

            var action = new RapidCombination0
            {
                avatarAddress = _avatarAddress,
                slotIndex = 0,
            };

            Assert.Throws<RequiredBlockIndexException>(() => action.Execute(new ActionContext
            {
                PreviousStates = tempState,
                Signer = _agentAddress,
                BlockIndex = contextBlockIndex,
            }));
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(100, 101)]
        public void ExecuteThrowNotEnoughMaterialException(int alreadyHasCount, int requiredCount)
        {
            const int slotStateUnlockStage = 1;

            var avatarState = _initialState.GetAvatarState(_avatarAddress);
            avatarState.worldInformation = new WorldInformation(
                0,
                _initialState.GetSheet<WorldSheet>(),
                slotStateUnlockStage);

            var material = ItemFactory.CreateMaterial(
                _tableSheets.MaterialItemSheet.Values.First(r =>
                    r.ItemSubType == ItemSubType.Hourglass));
            avatarState.inventory.AddItem2(material, count: alreadyHasCount);

            var firstEquipmentRow = _tableSheets.EquipmentItemSheet.First;
            Assert.NotNull(firstEquipmentRow);

            var gameConfigState = _initialState.GetGameConfigState();
            var requiredBlockIndex = gameConfigState.HourglassPerBlock * requiredCount;
            var equipment = (Equipment)ItemFactory.CreateItemUsable(
                firstEquipmentRow,
                Guid.NewGuid(),
                requiredBlockIndex);
            avatarState.inventory.AddItem2(equipment);

            var result = new CombinationConsumable5.ResultModel
            {
                actionPoint = 0,
                gold = 0,
                materials = new Dictionary<Material, int>(),
                itemUsable = equipment,
                recipeId = 0,
                itemType = ItemType.Equipment,
            };

            var mail = new CombinationMail(result, 0, default, requiredBlockIndex);
            result.id = mail.id;
            avatarState.Update2(mail);

            var slotAddress = _avatarAddress.Derive(string.Format(
                CultureInfo.InvariantCulture,
                CombinationSlotState.DeriveFormat,
                0));
            var slotState = new CombinationSlotState(slotAddress, slotStateUnlockStage);
            slotState.Update(result, 0, 0);

            var tempState = _initialState
                .SetState(_avatarAddress, avatarState.Serialize())
                .SetState(slotAddress, slotState.Serialize());

            var action = new RapidCombination0
            {
                avatarAddress = _avatarAddress,
                slotIndex = 0,
            };

            Assert.Throws<NotEnoughMaterialException>(() => action.Execute(new ActionContext
            {
                PreviousStates = tempState,
                Signer = _agentAddress,
                BlockIndex = 1,
            }));
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
            var r = new CombinationConsumable5.ResultModel
            {
                id = default,
                gold = 0,
                actionPoint = 0,
                recipeId = 1,
                subRecipeId = subRecipeId,
                materials = new Dictionary<Material, int>
                {
                    [material] = 1,
                    [material2] = 1,
                },
                itemUsable = itemUsable,
            };
            var result = new RapidCombination0.ResultModel((Dictionary)r.Serialize())
            {
                cost = new Dictionary<Material, int>
                {
                    [material] = 1,
                    [material2] = 1,
                },
            };

            var r2 = new CombinationConsumable5.ResultModel
            {
                id = default,
                gold = 0,
                actionPoint = 0,
                recipeId = 1,
                subRecipeId = subRecipeId,
                materials = new Dictionary<Material, int>
                {
                    [material2] = 1,
                    [material] = 1,
                },
                itemUsable = itemUsable,
            };

            var result2 = new RapidCombination0.ResultModel((Dictionary)r2.Serialize())
            {
                cost = new Dictionary<Material, int>
                {
                    [material2] = 1,
                    [material] = 1,
                },
            };

            Assert.Equal(result.Serialize(), result2.Serialize());
        }
    }
}
