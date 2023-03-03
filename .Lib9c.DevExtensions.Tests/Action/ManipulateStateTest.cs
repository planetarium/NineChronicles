#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Lib9c.DevExtensions.Action;
using Lib9c.Tests;
using Lib9c.Tests.Action;
using Lib9c.Tests.Util;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Action;
using Nekoyume.Model;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.Model.Quest;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Xunit;

namespace Lib9c.DevExtensions.Tests.Action
{
    public class ManipulateStateTest
    {
        private readonly TableSheets _tableSheets;
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly IAccountStateDelta _initialStateV2;
        private readonly Address _inventoryAddress;
        private readonly Address _worldInformationAddress;
        private readonly Address _questListAddress;
        private readonly Address _recipeAddress;
        private readonly AvatarState _avatarState;

        public ManipulateStateTest()
        {
            (_tableSheets, _agentAddress, _avatarAddress, _, _initialStateV2) =
                InitializeUtil.InitializeStates(isDevEx: true);
            _inventoryAddress = _avatarAddress.Derive(SerializeKeys.LegacyInventoryKey);
            _worldInformationAddress =
                _avatarAddress.Derive(SerializeKeys.LegacyWorldInformationKey);
            _questListAddress = _avatarAddress.Derive(SerializeKeys.LegacyQuestListKey);
            _recipeAddress = _avatarAddress.Derive("recipe_ids");
            _avatarState = _initialStateV2.GetAvatarStateV2(_avatarAddress);
        }

        // MemberData
        public static IEnumerable<object[]> FetchAvatarState()
        {
            var random = new Random();
            var blockIndex = (long)random.Next(1, 100);

            /* name, level, exp, actionPoint,
             dailyRewardReceivedIndex, blockIndex,
             hair, lens, ear, tail */

            // Change name
            yield return new object[]
            {
                "newAvatar", null, null, null,
                null, null,
                null, null, null, null
            };
            // Change level and exp
            yield return new object[]
            {
                null, random.Next(1, 300), (long)random.Next(0, 100), null,
                null, null,
                null, null, null, null
            };
            // Change AP
            yield return new object[]
            {
                null, null, null, random.Next(0, 120),
                null, null,
                null, null, null, null
            };
            // Change block indexes
            yield return new object[]
            {
                null, null, null, null,
                blockIndex + 1700, blockIndex, // Get another daily reward
                null, null, null, null
            };
            // Change outfit
            yield return new object[]
            {
                null, null, null, null,
                null, null,
                random.Next(0, 4), random.Next(0, 4), random.Next(0, 4), random.Next(0, 4)
            };
            // Change multiple things
            yield return new object[]
            {
                "newAvatar", random.Next(1, 300), (long)random.Next(0, 100), random.Next(0, 120),
                blockIndex + 1700, blockIndex,
                random.Next(0, 4), random.Next(0, 4), random.Next(0, 4), random.Next(0, 4)
            };
        }

        public static IEnumerable<object[]> FetchInventory()
        {
            var random = new TestRandom();
            var (tableSheets, _, _, _, _) = InitializeUtil.InitializeStates(isDevEx: true);
            var equipmentList = tableSheets.EquipmentItemSheet.Values.ToList();
            var consumableList = tableSheets.ConsumableItemSheet.Values.ToList();
            var materialList = tableSheets.MaterialItemSheet.Values.ToList();

            var equipment = ItemFactory.CreateItem(
                equipmentList[random.Next(0, equipmentList.Count)],
                random
            );
            var consumable = ItemFactory.CreateItem(
                consumableList[random.Next(consumableList.Count)],
                random
            );
            var material = ItemFactory.CreateItem(
                materialList[random.Next(0, materialList.Count)],
                random
            );
            // Clear Inventory
            yield return new object[]
            {
                new Inventory()
            };

            // Equipment
            var equipmentInventory = new Inventory();
            equipmentInventory.AddItem(equipment);
            yield return new object[]
            {
                equipmentInventory
            };

            // Material
            var materialInventory = new Inventory();
            materialInventory.AddItem(material);
            yield return new object[]
            {
                materialInventory
            };
            // Consumable
            var consumableInventory = new Inventory();
            consumableInventory.AddItem(equipment);
            yield return new object[]
            {
                consumableInventory
            };
            // Mixed
            var inventory = new Inventory();
            inventory.AddItem(equipment);
            inventory.AddItem(consumable);
            inventory.AddItem(material);
            yield return new object[]
            {
                inventory
            };
        }

        public static IEnumerable<object[]> FetchWorldInfo()
        {
            var random = new Random();
            var (tableSheets, _, _, _, _) = InitializeUtil.InitializeStates(isDevEx: true);
            var worldSheet = tableSheets.WorldSheet;
            yield return new object[]
            {
                0,
                new WorldInformation(0L, worldSheet, 0)
            };

            var targetStage = random.Next(1, 300);
            yield return new object[]
            {
                targetStage,
                new WorldInformation(0L, worldSheet, targetStage)
            };

            yield return new object[]
            {
                tableSheets.WorldSheet.OrderedList.Last(world => world.Id < 100).StageEnd,
                new WorldInformation(0L, worldSheet, true)
            };
        }

        public static IEnumerable<object[]> FetchQuest()
        {
            var random = new Random();
            var (tableSheets, _, _, _, stateV2) = InitializeUtil.InitializeStates(isDevEx: true);
            // Empty QuestList
            yield return new object[]
            {
                new List<int>(),
                new QuestList(Dictionary.Empty),
            };

            // QuestList
            yield return new object[]
            {
                new List<int>(),
                new QuestList(
                    tableSheets.QuestSheet,
                    tableSheets.QuestRewardSheet,
                    tableSheets.QuestItemRewardSheet,
                    tableSheets.EquipmentItemRecipeSheet,
                    tableSheets.EquipmentItemSubRecipeSheet
                )
            };

            // Clear combination quest
            var combinationQuestList = new QuestList(
                tableSheets.QuestSheet,
                tableSheets.QuestRewardSheet,
                tableSheets.QuestItemRewardSheet,
                tableSheets.EquipmentItemRecipeSheet,
                tableSheets.EquipmentItemSubRecipeSheet
            );
            var equipmentList = tableSheets.EquipmentItemSheet.Values.ToList();
            var equipmentRow = equipmentList[random.Next(0, equipmentList.Count)];
            var item = ItemFactory.CreateItemUsable(equipmentRow, Guid.NewGuid(), 0);
            combinationQuestList.UpdateCombinationQuest(item);
            yield return new object[]
            {
                combinationQuestList.completedQuestIds,
                combinationQuestList
            };

            // Clear trade quest
            var tradeQuestList = new QuestList(
                tableSheets.QuestSheet,
                tableSheets.QuestRewardSheet,
                tableSheets.QuestItemRewardSheet,
                tableSheets.EquipmentItemRecipeSheet,
                tableSheets.EquipmentItemSubRecipeSheet
            );
            tradeQuestList.UpdateTradeQuest(TradeType.Sell, stateV2.GetGoldCurrency() * 1);

            yield return new object[]
            {
                tradeQuestList.completedQuestIds,
                tradeQuestList
            };

            // Clear stage quest
            var stageQuestList = new QuestList(
                tableSheets.QuestSheet,
                tableSheets.QuestRewardSheet,
                tableSheets.QuestItemRewardSheet,
                tableSheets.EquipmentItemRecipeSheet,
                tableSheets.EquipmentItemSubRecipeSheet
            );
            var targetStage = random.Next(1, 300);
            var stageMap = new CollectionMap();
            for (var i = 1; i <= targetStage; i++)
            {
                stageMap.Add(new KeyValuePair<int, int>(i, 1));
            }

            stageQuestList.UpdateStageQuest(stageMap);
            yield return new object[]
            {
                stageQuestList.completedQuestIds,
                stageQuestList
            };

            // Clear multiple
            var questList = new QuestList(
                tableSheets.QuestSheet,
                tableSheets.QuestRewardSheet,
                tableSheets.QuestItemRewardSheet,
                tableSheets.EquipmentItemRecipeSheet,
                tableSheets.EquipmentItemSubRecipeSheet
            );
            combinationQuestList.UpdateCombinationQuest(item);
            tradeQuestList.UpdateTradeQuest(TradeType.Sell, stateV2.GetGoldCurrency() * 1);
            stageMap = new CollectionMap();
            for (var i = 1; i <= targetStage; i++)
            {
                stageMap.Add(new KeyValuePair<int, int>(i, 1));
            }

            yield return new object[]
            {
                questList.completedQuestIds,
                questList
            };
        }
        // ~MemberData

        // Logics
        private IAccountStateDelta Manipulate(
            IAccountStateDelta state,
            List<(Address, IValue)> targetStateList
        )
        {
            var action = new ManipulateState
            {
                StateList = targetStateList
            };

            return action.Execute(new ActionContext
            {
                PreviousStates = state,
                Signer = _agentAddress,
                BlockIndex = int.MaxValue / 2
            });
        }

        private void TestAvatarState(
            IAccountStateDelta state,
            string? name, int? level, long? exp, int? actionPoint,
            long? blockIndex, long? dailyRewardReceivedIndex,
            int? hair, int? lens, int? ear, int? tail
        )
        {
            var targetAvatarState = state.GetAvatarStateV2(_avatarAddress);

            if (name != null)
            {
                Assert.Equal(name, targetAvatarState.name);
            }

            if (level != null)
            {
                Assert.Equal(level, targetAvatarState.level);
            }

            if (exp != null)
            {
                Assert.Equal(exp, targetAvatarState.exp);
            }

            if (actionPoint != null)
            {
                Assert.Equal(actionPoint, targetAvatarState.actionPoint);
            }

            if (blockIndex != null)
            {
                Assert.Equal(blockIndex, targetAvatarState.blockIndex);
            }

            if (dailyRewardReceivedIndex != null)
            {
                Assert.Equal(dailyRewardReceivedIndex, targetAvatarState.dailyRewardReceivedIndex);
            }

            if (hair != null)
            {
                Assert.Equal(hair, targetAvatarState.hair);
            }

            if (lens != null)
            {
                Assert.Equal(lens, targetAvatarState.lens);
            }

            if (ear != null)
            {
                Assert.Equal(ear, targetAvatarState.ear);
            }

            if (tail != null)
            {
                Assert.Equal(tail, targetAvatarState.tail);
            }
        }

        private void TestInventoryState(IAccountStateDelta state, Inventory targetInventory)
        {
            var avatarState = state.GetAvatarStateV2(_avatarAddress);
            var inventoryState = avatarState.inventory;
            Assert.Equal(targetInventory.Items.Count, inventoryState.Items.Count);
            foreach (var item in targetInventory.Items)
            {
                Assert.Contains(item, inventoryState.Items);
            }
        }

        // Tests
        [Theory]
        [MemberData(nameof(FetchAvatarState))]
        public void SetAvatarState(
            string? name, int? level, long? exp, int? actionPoint,
            long? blockIndex, long? dailyRewardReceivedIndex,
            int? hair, int? lens, int? ear, int? tail
        )
        {
            var newAvatarState = (AvatarState)_avatarState.Clone();
            newAvatarState.name = name ?? _avatarState.name;
            newAvatarState.level = level ?? _avatarState.level;
            newAvatarState.exp = exp ?? _avatarState.exp;
            newAvatarState.actionPoint = actionPoint ?? _avatarState.actionPoint;
            newAvatarState.blockIndex = blockIndex ?? _avatarState.blockIndex;
            newAvatarState.dailyRewardReceivedIndex =
                dailyRewardReceivedIndex ?? _avatarState.dailyRewardReceivedIndex;
            newAvatarState.hair = hair ?? _avatarState.hair;
            newAvatarState.lens = lens ?? _avatarState.lens;
            newAvatarState.ear = ear ?? _avatarState.ear;
            newAvatarState.tail = tail ?? _avatarState.tail;

            var state = Manipulate(
                _initialStateV2,
                new List<(Address, IValue)>
                {
                    (_avatarAddress, newAvatarState.SerializeV2())
                }
            );

            TestAvatarState(
                state,
                name, level, exp, actionPoint,
                blockIndex, dailyRewardReceivedIndex,
                hair, lens, ear, tail
            );
        }

        private void TestQuestState(IAccountStateDelta state, List<int> targetQuestIdList)
        {
            var avatarState = state.GetAvatarStateV2(_avatarAddress);
            var questState = avatarState.questList;
            foreach (var target in targetQuestIdList)
            {
                Assert.Contains(target, questState.completedQuestIds);
            }
        }

        private void TestWorldInformation(IAccountStateDelta state, int lastClearedStage)
        {
            var avatarState = state.GetAvatarStateV2(_avatarAddress);
            var worldInformation = avatarState.worldInformation;

            for (var i = 0; i < lastClearedStage; i++)
            {
                Assert.True(worldInformation.IsStageCleared(i));
            }
        }

        [Theory]
        [MemberData(nameof(FetchInventory))]
        public void SetInventoryState(Inventory targetInventory)
        {
            var state = Manipulate(
                _initialStateV2,
                new List<(Address, IValue)>
                {
                    (_inventoryAddress, targetInventory.Serialize()),
                }
            );

            TestInventoryState(state, targetInventory);
        }

        [Theory]
        [MemberData(nameof(FetchWorldInfo))]
        public void SetWorldInformation(int lastClearedStage, WorldInformation targetInfo)
        {
            var state = Manipulate(
                _initialStateV2,
                new List<(Address, IValue)>
                {
                    (_worldInformationAddress, targetInfo.Serialize())
                }
            );

            TestWorldInformation(state, lastClearedStage);
        }

        [Theory]
        [MemberData(nameof(FetchQuest))]
        public void SetQuestState(List<int> targetQuestIdList, QuestList questList)
        {
            var state = Manipulate(_initialStateV2,
                new List<(Address, IValue)>
                {
                    (_questListAddress, questList.Serialize())
                }
            );

            TestQuestState(state, targetQuestIdList);
        }

        [Fact]
        public void SetMultipleStates()
        {
            var avatarData = FetchAvatarState().Last();
            var newAvatarState = (AvatarState)_avatarState.Clone();
            newAvatarState.name = (string)avatarData[0];
            newAvatarState.level = (int)avatarData[1];
            newAvatarState.exp = (long)avatarData[2];
            newAvatarState.actionPoint = (int)avatarData[3];
            newAvatarState.blockIndex = (long)avatarData[4];
            newAvatarState.dailyRewardReceivedIndex = (long)avatarData[5];
            newAvatarState.hair = (int)avatarData[6];
            newAvatarState.lens = (int)avatarData[7];
            newAvatarState.ear = (int)avatarData[8];
            newAvatarState.tail = (int)avatarData[9];

            var inventory = (Inventory)FetchInventory().Last()[0];
            var worldInfoData = FetchWorldInfo().Last();
            var lastClearedStage = (int)worldInfoData[0];
            var worldState = (WorldInformation)worldInfoData[1];
            var questData = FetchQuest().Last();
            var targetQuestIdList = (List<int>)questData[0];
            var questList = (QuestList)questData[1];

            var state = Manipulate(_initialStateV2, new List<(Address, IValue)>
                {
                    (_avatarAddress, newAvatarState.Serialize()),
                    (_inventoryAddress, inventory.Serialize()),
                    (_worldInformationAddress, worldState.Serialize()),
                    (_questListAddress, questList.Serialize()),
                }
            );

            TestAvatarState(state,
                (string?)avatarData[0], (int?)avatarData[1],
                (long?)avatarData[2], (int?)avatarData[3],
                (long?)avatarData[4], (long?)avatarData[5],
                (int?)avatarData[6], (int?)avatarData[7], (int?)avatarData[8], (int?)avatarData[9]
            );
            TestInventoryState(state, inventory);
            TestWorldInformation(state, lastClearedStage);
            TestQuestState(state, targetQuestIdList);
        }
    }
}
