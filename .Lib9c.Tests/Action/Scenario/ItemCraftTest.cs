/* DISCLAIMER
 This test only tests AvatarStateV2.
 AvatarStateV1 is old version and not tested.
 */

namespace Lib9c.Tests.Action.Scenario
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Lib9c.Tests.Util;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Nekoyume.Action;
    using Nekoyume.Model.EnumType;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;
    using static Lib9c.SerializeKeys;

    public class ItemCraftTest
    {
        private readonly Address _agentAddr;
        private readonly Address _avatarAddr;
        private readonly Address _inventoryAddr;
        private readonly Address _worldInformationAddr;
        private readonly IAccountStateDelta _initialStatesWithAvatarStateV1;
        private readonly IAccountStateDelta _initialStatesWithAvatarStateV2;
        private readonly TableSheets _tableSheets;

        public ItemCraftTest()
        {
            (
                _tableSheets,
                _agentAddr,
                _avatarAddr,
                _initialStatesWithAvatarStateV1,
                _initialStatesWithAvatarStateV2
            ) = InitializeUtil.InitializeStates();
            _inventoryAddr = _avatarAddr.Derive(LegacyInventoryKey);
            _worldInformationAddr = _avatarAddr.Derive(LegacyWorldInformationKey);
        }

        [Theory]
        [InlineData(1, new[] { 10110000 })] // 검
        [InlineData(1, new[] { 10110000, 10111000 })] // 검, 롱 소드(불)
        [InlineData(1, new[] { 10110000, 10111000, 10114000 })] // 검, 롱 소드(불), 롱 소드(바람)
        public void CraftEquipmentTest(int randomSeed, int[] targetItemIdList)
        {
            // Disable all quests to prevent contamination by quest reward
            var (stateV1, stateV2) = QuestUtil.DisableQuestList(
                _initialStatesWithAvatarStateV1,
                _initialStatesWithAvatarStateV2,
                _avatarAddr
            );

            // Setup requirements
            var random = new TestRandom(randomSeed);
            var recipeList = _tableSheets.EquipmentItemRecipeSheet.OrderedList.Where(
                recipe => targetItemIdList.Contains(recipe.ResultEquipmentId)
            ).ToList();
            Assert.Equal(targetItemIdList.Length, recipeList.Count);

            List<EquipmentItemSubRecipeSheet.MaterialInfo> allMaterialList =
                new List<EquipmentItemSubRecipeSheet.MaterialInfo>();
            foreach (var recipe in recipeList)
            {
                allMaterialList = allMaterialList
                    .Concat(recipe.GetAllMaterials(
                        _tableSheets.EquipmentItemSubRecipeSheetV2,
                        CraftType.Normal
                    ))
                    .ToList();
            }

            // Unlock recipe
            var maxUnlockStage = recipeList.Aggregate(0, (e, c) => Math.Max(e, c.UnlockStage));
            var unlockRecipeIdsAddress = _avatarAddr.Derive("recipe_ids");
            var recipeIds = List.Empty;
            for (int i = 1; i < maxUnlockStage + 1; i++)
            {
                recipeIds = recipeIds.Add(i.Serialize());
            }

            stateV2 = stateV2.SetState(unlockRecipeIdsAddress, recipeIds);

            // Prepare combination slot
            for (var i = 0; i < targetItemIdList.Length; i++)
            {
                stateV2 = CraftUtil.PrepareCombinationSlot(stateV2, _avatarAddr, i);
            }

            // Initial inventory must be empty
            var inventoryState = new Inventory((List)stateV2.GetState(_inventoryAddr));
            Assert.Equal(0, inventoryState.Items.Count);

            // Add materials to inventory
            stateV2 = CraftUtil.AddMaterialsToInventory(
                stateV2,
                _tableSheets,
                _avatarAddr,
                allMaterialList,
                random
            );

            for (var i = 0; i < recipeList.Count; i++)
            {
                // Unlock stage
                var equipmentRecipe = recipeList[i];
                stateV2 = CraftUtil.UnlockStage(
                    stateV2,
                    _tableSheets,
                    _worldInformationAddr,
                    equipmentRecipe.UnlockStage
                );

                // Do Combination Action
                var action = new CombinationEquipment
                {
                    avatarAddress = _avatarAddr,
                    slotIndex = i,
                    recipeId = equipmentRecipe.Id,
                    subRecipeId = equipmentRecipe.SubRecipeIds?[0],
                };

                stateV2 = action.Execute(new ActionContext
                {
                    PreviousState = stateV2,
                    Signer = _agentAddr,
                    BlockIndex = 0L,
                    Random = random,
                });
                var slotState = stateV2.GetCombinationSlotState(_avatarAddr, i);
                // TEST: requiredBlock
                // TODO: Check reduced required block when pet comes in
                Assert.Equal(equipmentRecipe.RequiredBlockIndex, slotState.RequiredBlockIndex);
            }

            inventoryState = new Inventory((List)stateV2.GetState(_inventoryAddr));
            // TEST: Only created equipments should remain in inventory
            Assert.Equal(recipeList.Count, inventoryState.Items.Count);
            foreach (var itemId in targetItemIdList)
            {
                // TEST: Created equipment should match with targetItemList
                Assert.NotNull(inventoryState.Items.Where(e => e.item.Id == itemId));
            }
        }

        [Theory]
        [InlineData(1, new[] { 201000 })] // 참치캔
        [InlineData(1, new[] { 201000, 201002 })] // 참치캔, 계란후라이
        [InlineData(1, new[] { 201011, 201012, 201013 })] // 스테이크, 모둠스테이크, 전설의 스테이크
        public void CraftConsumableTest(int randomSeed, int[] targetItemIdList)
        {
            // Disable all quests to prevent contamination by quest reward
            var (stateV1, stateV2) = QuestUtil.DisableQuestList(
                _initialStatesWithAvatarStateV1,
                _initialStatesWithAvatarStateV2,
                _avatarAddr
            );

            // Setup requirements
            var random = new TestRandom(randomSeed);
            var recipeList = _tableSheets.ConsumableItemRecipeSheet.OrderedList.Where(
                recipe => targetItemIdList.Contains(recipe.ResultConsumableItemId)
            ).ToList();
            Assert.Equal(targetItemIdList.Length, recipeList.Count);

            var allMaterialList = new List<EquipmentItemSubRecipeSheet.MaterialInfo>();
            foreach (var recipe in recipeList)
            {
                allMaterialList = allMaterialList.Concat(recipe.GetAllMaterials()).ToList();
            }

            // Prepare combination slot
            for (var i = 0; i < targetItemIdList.Length; i++)
            {
                stateV2 = CraftUtil.PrepareCombinationSlot(stateV2, _avatarAddr, i);
            }

            // Initial inventory must be empty
            var inventoryState = new Inventory((List)stateV2.GetState(_inventoryAddr));
            Assert.Equal(0, inventoryState.Items.Count);

            // Add materials to inventory
            stateV2 = CraftUtil.AddMaterialsToInventory(
                stateV2,
                _tableSheets,
                _avatarAddr,
                allMaterialList,
                random
            );

            // Unlock stage
            stateV2 = CraftUtil.UnlockStage(
                stateV2,
                _tableSheets,
                _worldInformationAddr,
                6 // Stage to open craft consumables
            );

            for (var i = 0; i < recipeList.Count; i++)
            {
                // Do combination action
                var recipe = recipeList[i];
                var action = new CombinationConsumable
                {
                    avatarAddress = _avatarAddr,
                    slotIndex = i,
                    recipeId = recipe.Id,
                };

                stateV2 = action.Execute(new ActionContext
                {
                    PreviousState = stateV2,
                    Signer = _agentAddr,
                    BlockIndex = 0L,
                    Random = random,
                });
                var slotState = stateV2.GetCombinationSlotState(_avatarAddr, i);
                // TEST: requiredBlockIndex
                // TODO: Check reduced required block when pet comens in
                Assert.Equal(recipe.RequiredBlockIndex, slotState.RequiredBlockIndex);
            }

            inventoryState = new Inventory((List)stateV2.GetState(_inventoryAddr));
            // TEST: Only created items should remain in inventory
            Assert.Equal(recipeList.Count, inventoryState.Items.Count);
            foreach (var itemId in targetItemIdList)
            {
                // TEST: Created consumables should be match with targetItemList
                Assert.NotNull(inventoryState.Items.Where(e => e.item.Id == itemId));
            }
        }

        [Theory]
        [InlineData(1, 1001, new[] { 900101 })] // 2022 Summer Event, 몬스터펀치
        public void EventConsumableItemCraftTest(
            int randomSeed,
            int eventScheduleId,
            int[] targetItemIdList
        )
        {
            // Disable all quests to prevent contamination by quest reward
            var (stateV1, stateV2) = QuestUtil.DisableQuestList(
                _initialStatesWithAvatarStateV1,
                _initialStatesWithAvatarStateV2,
                _avatarAddr
            );

            // Setup requirements
            var random = new TestRandom(randomSeed);
            var recipeList = _tableSheets.EventConsumableItemRecipeSheet.OrderedList.Where(
                recipe => targetItemIdList.Contains(recipe.ResultConsumableItemId)
            ).ToList();
            var allMaterialList = new List<EquipmentItemSubRecipeSheet.MaterialInfo>();
            foreach (var recipe in recipeList)
            {
                allMaterialList = allMaterialList.Concat(recipe.GetAllMaterials()).ToList();
            }

            // Unlock stage to create consumables
            stateV2 = CraftUtil.UnlockStage(stateV2, _tableSheets, _worldInformationAddr, 6);

            // Prepare combination slot
            for (var i = 0; i < targetItemIdList.Length; i++)
            {
                stateV2 = CraftUtil.PrepareCombinationSlot(stateV2, _avatarAddr, i);
            }

            // Initial inventory must be empty
            var inventoryState = new Inventory((List)stateV2.GetState(_inventoryAddr));
            Assert.Equal(0, inventoryState.Items.Count);

            // Add materials to inventory
            stateV2 = CraftUtil.AddMaterialsToInventory(
                stateV2,
                _tableSheets,
                _avatarAddr,
                allMaterialList,
                random
            );

            for (var i = 0; i < recipeList.Count; i++)
            {
                var eventRow = _tableSheets.EventScheduleSheet[eventScheduleId];
                // Do combination action
                var recipe = recipeList[i];
                var action = new EventConsumableItemCrafts
                {
                    AvatarAddress = _avatarAddr,
                    EventScheduleId = eventScheduleId,
                    EventConsumableItemRecipeId = recipe.Id,
                    SlotIndex = i,
                };

                stateV2 = action.Execute(new ActionContext
                {
                    PreviousState = stateV2,
                    Signer = _agentAddr,
                    BlockIndex = eventRow.StartBlockIndex,
                    Random = random,
                });
                var slotState = stateV2.GetCombinationSlotState(_avatarAddr, i);
                // TEST: requiredBlockIndex
                Assert.Equal(recipe.RequiredBlockIndex, slotState.RequiredBlockIndex);
            }

            inventoryState = new Inventory((List)stateV2.GetState(_inventoryAddr));
            // TEST: Only created items should remain in inventory
            Assert.Equal(recipeList.Count, inventoryState.Items.Count);
            foreach (var itemId in targetItemIdList)
            {
                // TEST: Created comsumables should be match with targetItemList
                Assert.NotNull(inventoryState.Items.Where(e => e.item.Id == itemId));
            }
        }

        [Theory]
        [InlineData(1, 1002, new[] { 10020001 })] // Grand Finale, AP Stone
        public void EventMaterialItemCraftsTest(
            int randomSeed,
            int eventScheduleId,
            int[] targetItemIdList
        )
        {
            // Disable all quests to prevent contamination by quest reward
            var (stateV1, stateV2) = QuestUtil.DisableQuestList(
                _initialStatesWithAvatarStateV1,
                _initialStatesWithAvatarStateV2,
                _avatarAddr
            );

            // Setup requirements
            var random = new TestRandom(randomSeed);
            var recipeList = _tableSheets.EventConsumableItemRecipeSheet.OrderedList.Where(
                recipe => targetItemIdList.Contains(recipe.ResultConsumableItemId)
            ).ToList();
            var allMaterialList = new List<EquipmentItemSubRecipeSheet.MaterialInfo>();
            foreach (var recipe in recipeList)
            {
                allMaterialList = allMaterialList.Concat(recipe.GetAllMaterials()).ToList();
            }

            // Unlock stage to create consumables
            stateV2 = CraftUtil.UnlockStage(stateV2, _tableSheets, _worldInformationAddr, 6);

            // Prepare combination slot
            for (var i = 0; i < targetItemIdList.Length; i++)
            {
                stateV2 = CraftUtil.PrepareCombinationSlot(stateV2, _avatarAddr, i);
            }

            // Initial inventory must be empty
            var inventoryState = new Inventory((List)stateV2.GetState(_inventoryAddr));
            Assert.Equal(0, inventoryState.Items.Count);

            // Add materials to inventory
            stateV2 = CraftUtil.AddMaterialsToInventory(
                stateV2,
                _tableSheets,
                _avatarAddr,
                allMaterialList,
                random
            );

            for (var i = 0; i < recipeList.Count; i++)
            {
                var eventRow = _tableSheets.EventScheduleSheet[eventScheduleId];
                // Do combination action
                var recipe = recipeList[i];

                // FIXME: This should be fixed if you test multiple items
                var materialsToUse = new Dictionary<int, int>();
                foreach (var material in allMaterialList)
                {
                    if (materialsToUse.ContainsKey(material.Id))
                    {
                        materialsToUse[material.Id] += material.Count;
                    }
                    else
                    {
                        materialsToUse[material.Id] = material.Count;
                    }
                }

                var action = new EventMaterialItemCrafts
                {
                    AvatarAddress = _avatarAddr,
                    EventScheduleId = eventScheduleId,
                    EventMaterialItemRecipeId = recipe.Id,
                    MaterialsToUse = materialsToUse,
                };

                stateV2 = action.Execute(new ActionContext
                {
                    PreviousState = stateV2,
                    Signer = _agentAddr,
                    BlockIndex = eventRow.StartBlockIndex,
                    Random = random,
                });
                var slotState = stateV2.GetCombinationSlotState(_avatarAddr, i);
                // TEST: requiredBlockIndex
                Assert.Equal(recipe.RequiredBlockIndex, slotState.RequiredBlockIndex);
            }

            inventoryState = new Inventory((List)stateV2.GetState(_inventoryAddr));
            // TEST: Only created items should remain in inventory
            Assert.Equal(recipeList.Count, inventoryState.Items.Count);
            foreach (var itemId in targetItemIdList)
            {
                // TEST: Created comsumables should be match with targetItemList
                Assert.NotNull(inventoryState.Items.Where(e => e.item.Id == itemId));
            }
        }
    }
}
