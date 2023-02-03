namespace Lib9c.Tests.Action.Scenario
{
    using System;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Action;
    using Nekoyume.Action;
    using Nekoyume.Model;
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
            ) = TestUtils.InitializeStates();
        }

        [Theory]
        [InlineData(new[] { 10110000 })] // 검
        public void CraftTest(int[] targetItemIdList)
        {
            var random = new TestRandom();
            var recipeIdList = TestUtils.GetRecipeIdFromItemId(_tableSheets, targetItemIdList);
            var materialDict = TestUtils.GetMaterialsFromRecipeId(_tableSheets, recipeIdList);
            var avatarState = _initialStatesWithAvatarStateV2.GetAvatarStateV2(_avatarAddr);
            foreach (var material in materialDict)
            {
                avatarState.inventory.AddItem(material.Key, material.Value);
            }

            var maxUnlockStage = recipeIdList.Aggregate(0, (e, c) => Math.Max(e, c.Item3));
            var unlockRecipeIdsAddress = _avatarAddr.Derive("recipe_ids");
            var recipeIds = List.Empty;
            for (int i = 1; i < maxUnlockStage + 1; i++)
            {
                recipeIds = recipeIds.Add(i.Serialize());
            }

            var state = _initialStatesWithAvatarStateV2.SetState(unlockRecipeIdsAddress, recipeIds);

            foreach (var (recipeId, subRecipeId, unlockStage) in recipeIdList)
            {
                avatarState.worldInformation =
                    new WorldInformation(0, _tableSheets.WorldSheet, unlockStage);
                state = state.SetState(_avatarAddr, avatarState.Serialize());

                var action = new CombinationEquipment
                {
                    avatarAddress = _avatarAddr,
                    slotIndex = 0,
                    recipeId = recipeId,
                    subRecipeId = subRecipeId,
                };

                var nextState = action.Execute(new ActionContext
                {
                    PreviousStates = state,
                    Signer = _agentAddr,
                    BlockIndex = 0L,
                    Random = random,
                });
                var slotState = nextState.GetCombinationSlotState(_avatarAddr, 0);
                Assert.Equal(5, slotState.RequiredBlockIndex);
            }
        }
    }
}
