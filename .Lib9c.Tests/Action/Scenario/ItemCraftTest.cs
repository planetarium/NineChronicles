namespace Lib9c.Tests.Action.Scenario
{
    using System;
    using System.Globalization;
    using System.Linq;
    using Bencodex.Types;
    using Lib9c.Tests.Util;
    using Libplanet;
    using Libplanet.Action;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Xunit;
    using Xunit.Abstractions;
    using static Lib9c.SerializeKeys;

    public class ItemCraftTest
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly Address _agentAddr;
        private readonly Address _avatarAddr;
        private readonly IAccountStateDelta _initialStatesWithAvatarStateV1;
        private readonly IAccountStateDelta _initialStatesWithAvatarStateV2;
        private readonly TableSheets _tableSheets;

        public ItemCraftTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            (
                _tableSheets,
                _agentAddr,
                _avatarAddr,
                _initialStatesWithAvatarStateV1,
                _initialStatesWithAvatarStateV2
            ) = InitializeUtil.InitializeStates();
        }

        [Theory]
        [InlineData(1, new[] { 10110000 })] // 검
        [InlineData(1, new[] { 10110000, 10111000 })] // 검, 롱 소드(불)
        [InlineData(1, new[] { 10110000, 10111000, 10114000 })] // 검, 롱 소드(불), 롱 소드(바람)
        public void CraftTest(int randomSeed, int[] targetItemIdList)
        {
            // NOTE: Do we have to test on both stateV1 and stateV2?

            // Disable all quests to prevent contamination by quest reward
            var (stateV1, stateV2) = QuestUtil.DisableQuestList(
                _initialStatesWithAvatarStateV1,
                _initialStatesWithAvatarStateV2,
                _avatarAddr
            );

            // Setup requirements
            var random = new TestRandom(randomSeed);
            var craftInfoList = CraftUtil.GetEquipmentCraftInfoFromItemId(_tableSheets, targetItemIdList);
            var materialDict = CraftUtil.GetMaterialsFromCraftInfo(_tableSheets, craftInfoList);
            var avatarState = stateV2.GetAvatarStateV2(_avatarAddr);

            // Unlock recipe
            var maxUnlockStage = craftInfoList.Aggregate(0, (e, c) => Math.Max(e, c.Item3));
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
                var slotAddress = _avatarAddr.Derive(string.Format(
                    CultureInfo.InvariantCulture,
                    CombinationSlotState.DeriveFormat,
                    i));
                var slotState = new CombinationSlotState(
                    slotAddress,
                    GameConfig.RequireClearedStageLevel.CombinationEquipmentAction
                );
                stateV2 = stateV2.SetState(slotAddress, slotState.Serialize());
            }

            // Initial inventory must be clear
            var inventoryAddress = _avatarAddr.Derive(LegacyInventoryKey);
            var inventoryState = new Inventory((List)stateV2.GetState(inventoryAddress));
            Assert.Equal(0, inventoryState.Items.Count);

            // Add materials to inventory
            foreach (var material in materialDict)
            {
                avatarState.inventory.AddItem(material.Key, material.Value);
                stateV2 = stateV2.SetState(inventoryAddress, avatarState.inventory.Serialize());
            }

            // Give material items to combine item
            for (var i = 0; i < craftInfoList.Count; i++)
            {
                // Unlock stage
                var (recipeId, subRecipeId, unlockStage, requiredBlockIndex) = craftInfoList[i];
                avatarState.worldInformation =
                    new WorldInformation(0, _tableSheets.WorldSheet, unlockStage);

                stateV2 = stateV2
                    .SetState(
                        _avatarAddr.Derive(LegacyWorldInformationKey),
                        avatarState.worldInformation.Serialize()
                    );

                // Do Combination Action
                var action = new CombinationEquipment
                {
                    avatarAddress = _avatarAddr,
                    slotIndex = i,
                    recipeId = recipeId,
                    subRecipeId = subRecipeId,
                };

                stateV2 = action.Execute(new ActionContext
                {
                    PreviousStates = stateV2,
                    Signer = _agentAddr,
                    BlockIndex = 0L,
                    Random = random,
                });
                var slotState = stateV2.GetCombinationSlotState(_avatarAddr, i);
                // TEST: requiredBlock
                Assert.Equal(requiredBlockIndex, slotState.RequiredBlockIndex);
            }

            inventoryState = new Inventory((List)stateV2.GetState(inventoryAddress));
            // TEST: Only created equipments should remain
            Assert.Equal(craftInfoList.Count, inventoryState.Items.Count);
            foreach (var itemId in targetItemIdList)
            {
                // TEST: Created equipment should match with targetItemList
                Assert.NotNull(inventoryState.Items.Where(e => e.item.Id == itemId));
            }
        }
    }
}
