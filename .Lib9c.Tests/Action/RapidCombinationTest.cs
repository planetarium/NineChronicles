namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Globalization;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Helper;
    using Nekoyume.Model;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Mail;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;
    using static Lib9c.SerializeKeys;

    public class RapidCombinationTest
    {
        private readonly IAccountStateDelta _initialState;

        private readonly TableSheets _tableSheets;

        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;

        public RapidCombinationTest()
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

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Execute(bool backward)
        {
            const int slotStateUnlockStage = 1;

            var avatarState = _initialState.GetAvatarState(_avatarAddress);
            avatarState.worldInformation = new WorldInformation(
                0,
                _initialState.GetSheet<WorldSheet>(),
                slotStateUnlockStage);

            var row = _tableSheets.MaterialItemSheet.Values.First(r =>
                r.ItemSubType == ItemSubType.Hourglass);
            avatarState.inventory.AddItem(ItemFactory.CreateMaterial(row), 83);
            avatarState.inventory.AddItem(ItemFactory.CreateTradableMaterial(row), 100);
            Assert.True(avatarState.inventory.HasFungibleItem(row.ItemId, 0, 183));

            var firstEquipmentRow = _tableSheets.EquipmentItemSheet.First;
            Assert.NotNull(firstEquipmentRow);

            var gameConfigState = _initialState.GetGameConfigState();
            var requiredBlockIndex = gameConfigState.HourglassPerBlock * 200;
            var equipment = (Equipment)ItemFactory.CreateItemUsable(
                firstEquipmentRow,
                Guid.NewGuid(),
                requiredBlockIndex);
            avatarState.inventory.AddItem(equipment);

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
            slotState.Update(result, 0, requiredBlockIndex);

            var tempState = _initialState.SetState(slotAddress, slotState.Serialize());

            if (backward)
            {
                tempState = tempState.SetState(_avatarAddress, avatarState.Serialize());
            }
            else
            {
                tempState = tempState
                    .SetState(_avatarAddress.Derive(LegacyInventoryKey), avatarState.inventory.Serialize())
                    .SetState(_avatarAddress.Derive(LegacyWorldInformationKey), avatarState.worldInformation.Serialize())
                    .SetState(_avatarAddress.Derive(LegacyQuestListKey), avatarState.questList.Serialize())
                    .SetState(_avatarAddress, avatarState.SerializeV2());
            }

            var action = new RapidCombination
            {
                avatarAddress = _avatarAddress,
                slotIndex = 0,
            };

            var nextState = action.Execute(new ActionContext
            {
                PreviousStates = tempState,
                Signer = _agentAddress,
                BlockIndex = 51,
            });

            var nextAvatarState = nextState.GetAvatarStateV2(_avatarAddress);
            var item = nextAvatarState.inventory.Equipments.First();

            Assert.Empty(nextAvatarState.inventory.Materials.Select(r => r.ItemSubType == ItemSubType.Hourglass));
            Assert.Equal(equipment.ItemId, item.ItemId);
            Assert.Equal(51, item.RequiredBlockIndex);
        }

        [Fact]
        public void Execute_Throw_CombinationSlotResultNullException()
        {
            var slotAddress = _avatarAddress.Derive(string.Format(
                CultureInfo.InvariantCulture,
                CombinationSlotState.DeriveFormat,
                0));
            var slotState = new CombinationSlotState(slotAddress, 0);
            slotState.Update(null, 0, 0);

            var tempState = _initialState
                .SetState(slotAddress, slotState.Serialize());

            var action = new RapidCombination
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
        public void Execute_Throw_NotEnoughClearedStageLevelException(int avatarClearedStage, int slotStateUnlockStage)
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
                100);

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

            var action = new RapidCombination
            {
                avatarAddress = _avatarAddress,
                slotIndex = 0,
            };

            Assert.Throws<NotEnoughClearedStageLevelException>(() => action.Execute(new ActionContext
            {
                PreviousStates = tempState,
                Signer = _agentAddress,
                BlockIndex = 1,
            }));
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(10, 100)]
        public void Execute_Throw_RequiredBlockIndexException(int itemRequiredBlockIndex, int contextBlockIndex)
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

            var action = new RapidCombination
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
        [InlineData(0, 0, 0, 40)]
        [InlineData(0, 1, 2, 40)]
        [InlineData(22, 0, 0, 40)]
        [InlineData(0, 22, 0, 40)]
        [InlineData(0, 22, 2, 40)]
        [InlineData(2, 10, 2, 40)]
        public void Execute_Throw_NotEnoughMaterialException(int materialCount, int tradableCount, long blockIndex, int requiredCount)
        {
            const int slotStateUnlockStage = 1;

            var avatarState = _initialState.GetAvatarState(_avatarAddress);
            avatarState.worldInformation = new WorldInformation(
                0,
                _initialState.GetSheet<WorldSheet>(),
                slotStateUnlockStage);

            var row = _tableSheets.MaterialItemSheet.Values.First(r => r.ItemSubType == ItemSubType.Hourglass);
            avatarState.inventory.AddItem(ItemFactory.CreateMaterial(row), count: materialCount);
            if (tradableCount > 0)
            {
                var material = ItemFactory.CreateTradableMaterial(row);
                material.RequiredBlockIndex = blockIndex;
                avatarState.inventory.AddItem(material, count: tradableCount);
            }

            var firstEquipmentRow = _tableSheets.EquipmentItemSheet.First;
            Assert.NotNull(firstEquipmentRow);

            var gameConfigState = _initialState.GetGameConfigState();
            var requiredBlockIndex = gameConfigState.HourglassPerBlock * requiredCount;
            var equipment = (Equipment)ItemFactory.CreateItemUsable(
                firstEquipmentRow,
                Guid.NewGuid(),
                requiredBlockIndex);
            avatarState.inventory.AddItem(equipment);

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

            var action = new RapidCombination
            {
                avatarAddress = _avatarAddress,
                slotIndex = 0,
            };

            Assert.Throws<NotEnoughMaterialException>(() => action.Execute(new ActionContext
            {
                PreviousStates = tempState,
                Signer = _agentAddress,
                BlockIndex = 51,
            }));
        }

        [Fact]
        public void Rehearsal()
        {
            var slotAddress = _avatarAddress.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CombinationSlotState.DeriveFormat,
                    0
                )
            );

            var updatedAddresses = new List<Address>()
            {
                _avatarAddress,
                _avatarAddress.Derive(LegacyInventoryKey),
                _avatarAddress.Derive(LegacyWorldInformationKey),
                _avatarAddress.Derive(LegacyQuestListKey),
                slotAddress,
            };

            var state = new State();

            var action = new RapidCombination
            {
                avatarAddress = _avatarAddress,
                slotIndex = 0,
            };

            var nextState = action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Signer = _agentAddress,
                BlockIndex = 0,
                Rehearsal = true,
            });

            Assert.Equal(updatedAddresses.ToImmutableHashSet(), nextState.UpdatedAddresses);
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

        [Fact]
        public void Execute_Throw_RequiredAppraiseBlockException()
        {
            const int slotStateUnlockStage = 1;

            var avatarState = _initialState.GetAvatarState(_avatarAddress);
            avatarState.worldInformation = new WorldInformation(
                0,
                _initialState.GetSheet<WorldSheet>(),
                slotStateUnlockStage);

            var row = _tableSheets.MaterialItemSheet.Values.First(r =>
                r.ItemSubType == ItemSubType.Hourglass);
            avatarState.inventory.AddItem(ItemFactory.CreateMaterial(row), count: 22);

            var firstEquipmentRow = _tableSheets.EquipmentItemSheet.First;
            Assert.NotNull(firstEquipmentRow);

            var gameConfigState = _initialState.GetGameConfigState();
            var requiredBlockIndex = gameConfigState.HourglassPerBlock * 40;
            var equipment = (Equipment)ItemFactory.CreateItemUsable(
                firstEquipmentRow,
                Guid.NewGuid(),
                requiredBlockIndex);
            avatarState.inventory.AddItem(equipment);

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
            avatarState.Update(mail);

            var slotAddress = _avatarAddress.Derive(string.Format(
                CultureInfo.InvariantCulture,
                CombinationSlotState.DeriveFormat,
                0));
            var slotState = new CombinationSlotState(slotAddress, slotStateUnlockStage);
            slotState.Update(result, 0, 0);

            var tempState = _initialState
                .SetState(_avatarAddress, avatarState.Serialize())
                .SetState(slotAddress, slotState.Serialize());

            var action = new RapidCombination
            {
                avatarAddress = _avatarAddress,
                slotIndex = 0,
            };

            Assert.Throws<AppraiseBlockNotReachedException>(() => action.Execute(new ActionContext
            {
                PreviousStates = tempState,
                Signer = _agentAddress,
                BlockIndex = 1,
            }));
        }

        [Theory]
        [InlineData(7)]
        [InlineData(9)]
        [InlineData(10)]
        [InlineData(11)]
        public void Execute_NotThrow_InvalidOperationException_When_TargetSlotCreatedBy(
            int itemEnhancementResultModelNumber)
        {
            const int slotStateUnlockStage = 1;

            var avatarState = _initialState.GetAvatarState(_avatarAddress);
            avatarState.worldInformation = new WorldInformation(
                0,
                _initialState.GetSheet<WorldSheet>(),
                slotStateUnlockStage);

            var row = _tableSheets.MaterialItemSheet.Values.First(r =>
                r.ItemSubType == ItemSubType.Hourglass);
            avatarState.inventory.AddItem(ItemFactory.CreateMaterial(row), 83);
            avatarState.inventory.AddItem(ItemFactory.CreateTradableMaterial(row), 100);
            Assert.True(avatarState.inventory.HasFungibleItem(row.ItemId, 0, 183));

            var firstEquipmentRow = _tableSheets.EquipmentItemSheet
                .OrderedList.First(e => e.Grade >= 1);
            Assert.NotNull(firstEquipmentRow);

            var gameConfigState = _initialState.GetGameConfigState();
            var requiredBlockIndex = gameConfigState.HourglassPerBlock * 200;
            var equipment = (Equipment)ItemFactory.CreateItemUsable(
                firstEquipmentRow,
                Guid.NewGuid(),
                requiredBlockIndex);
            var materialEquipment = (Equipment)ItemFactory.CreateItemUsable(
                firstEquipmentRow,
                Guid.NewGuid(),
                requiredBlockIndex);
            avatarState.inventory.AddItem(equipment);
            avatarState.inventory.AddItem(materialEquipment);

            AttachmentActionResult resultModel = null;
            var random = new TestRandom();
            var mailId = random.GenerateRandomGuid();
            var preItemUsable = new Equipment((Dictionary)equipment.Serialize());
            switch (itemEnhancementResultModelNumber)
            {
                case 7:
                {
                    equipment = ItemEnhancement7.UpgradeEquipment(equipment);
                    resultModel = new ItemEnhancement7.ResultModel
                    {
                        id = mailId,
                        itemUsable = equipment,
                        materialItemIdList = new[] { materialEquipment.NonFungibleId },
                    };

                    break;
                }

                case 9:
                {
                    Assert.True(ItemEnhancement9.TryGetRow(
                        equipment,
                        _tableSheets.EnhancementCostSheetV2,
                        out var costRow));
                    var equipmentResult = ItemEnhancement9.GetEnhancementResult(costRow, random);
                    equipment.LevelUpV2(
                        random,
                        costRow,
                        equipmentResult == ItemEnhancement9.EnhancementResult.GreatSuccess);
                    resultModel = new ItemEnhancement9.ResultModel
                    {
                        id = mailId,
                        preItemUsable = preItemUsable,
                        itemUsable = equipment,
                        materialItemIdList = new[] { materialEquipment.NonFungibleId },
                        gold = 0,
                        actionPoint = 0,
                        enhancementResult = ItemEnhancement9.EnhancementResult.GreatSuccess,
                    };

                    break;
                }

                case 10:
                {
                    Assert.True(ItemEnhancement10.TryGetRow(
                        equipment,
                        _tableSheets.EnhancementCostSheetV2,
                        out var costRow));
                    var equipmentResult = ItemEnhancement10.GetEnhancementResult(costRow, random);
                    equipment.LevelUpV2(
                        random,
                        costRow,
                        equipmentResult == ItemEnhancement10.EnhancementResult.GreatSuccess);
                    resultModel = new ItemEnhancement10.ResultModel
                    {
                        id = mailId,
                        preItemUsable = preItemUsable,
                        itemUsable = equipment,
                        materialItemIdList = new[] { materialEquipment.NonFungibleId },
                        gold = 0,
                        actionPoint = 0,
                        enhancementResult = ItemEnhancement10.EnhancementResult.GreatSuccess,
                    };

                    break;
                }

                case 11:
                {
                    Assert.True(ItemEnhancement.TryGetRow(
                        equipment,
                        _tableSheets.EnhancementCostSheetV2,
                        out var costRow));
                    var equipmentResult = ItemEnhancement.GetEnhancementResult(costRow, random);
                    equipment.LevelUpV2(
                        random,
                        costRow,
                        equipmentResult == ItemEnhancement.EnhancementResult.GreatSuccess);
                    resultModel = new ItemEnhancement.ResultModel
                    {
                        id = mailId,
                        preItemUsable = preItemUsable,
                        itemUsable = equipment,
                        materialItemIdList = new[] { materialEquipment.NonFungibleId },
                        gold = 0,
                        actionPoint = 0,
                        enhancementResult = ItemEnhancement.EnhancementResult.GreatSuccess,
                        CRYSTAL = 0 * CrystalCalculator.CRYSTAL,
                    };

                    break;
                }

                default:
                    break;
            }

            // NOTE: Do not update `mail`, because this test assumes that the `mail` was removed.
            {
                // var mail = new ItemEnhanceMail(resultModel, 0, random.GenerateRandomGuid(), requiredBlockIndex);
                // avatarState.Update(mail);
            }

            var slotAddress = _avatarAddress.Derive(string.Format(
                CultureInfo.InvariantCulture,
                CombinationSlotState.DeriveFormat,
                0));
            var slotState = new CombinationSlotState(slotAddress, slotStateUnlockStage);
            slotState.Update(resultModel, 0, requiredBlockIndex);

            var tempState = _initialState.SetState(slotAddress, slotState.Serialize())
                .SetState(_avatarAddress.Derive(LegacyInventoryKey), avatarState.inventory.Serialize())
                .SetState(_avatarAddress.Derive(LegacyWorldInformationKey), avatarState.worldInformation.Serialize())
                .SetState(_avatarAddress.Derive(LegacyQuestListKey), avatarState.questList.Serialize())
                .SetState(_avatarAddress, avatarState.SerializeV2());

            var action = new RapidCombination
            {
                avatarAddress = _avatarAddress,
                slotIndex = 0,
            };

            action.Execute(new ActionContext
            {
                PreviousStates = tempState,
                Signer = _agentAddress,
                BlockIndex = 51,
            });
        }
    }
}
