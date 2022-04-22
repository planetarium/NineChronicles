namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;
    using static SerializeKeys;

    public class UnlockEquipmentRecipeTest
    {
        private readonly IRandom _random;
        private readonly TableSheets _tableSheets;
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly AvatarState _avatarState;
        private readonly Currency _currency;
        private readonly IAccountStateDelta _initialState;

        public UnlockEquipmentRecipeTest()
        {
            _random = new TestRandom();
            var sheets = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(sheets);
            _agentAddress = new PrivateKey().ToAddress();
            _avatarAddress = new PrivateKey().ToAddress();
            _currency = new Currency("CRYSTAL", 18, minters: null);
            var gameConfigState = new GameConfigState(sheets[nameof(GameConfigSheet)]);

            var agentState = new AgentState(_agentAddress);
            _avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                gameConfigState,
                default
            );

            agentState.avatarAddresses.Add(0, _avatarAddress);

            _initialState = new State()
                .SetState(_agentAddress, agentState.Serialize())
                .SetState(Addresses.GetSheetAddress<EquipmentItemRecipeSheet>(), _tableSheets.EquipmentItemRecipeSheet.Serialize())
                .SetState(Addresses.GameConfig, gameConfigState.Serialize());
        }

        [Theory]
        [InlineData(new[] { 2, 3 }, true, false, false, true, 200, null)]
        [InlineData(new[] { 2 }, true, false, false, true, 100, null)]
        // AvatarState migration.
        [InlineData(new[] { 2 }, true, true, false, true, 100, null)]
        // Invalid recipe id.
        [InlineData(new[] { -1 }, true, false, false, false, 100, typeof(InvalidRecipeIdException))]
        [InlineData(new[] { 1 }, true, false, false, true, 100, typeof(InvalidRecipeIdException))]
        [InlineData(new int[] { }, true, false, false, false, 100, typeof(InvalidRecipeIdException))]
        // AvatarState is null.
        [InlineData(new[] { 2 }, false, true, false, true, 100, typeof(FailedLoadStateException))]
        [InlineData(new[] { 2 }, false, false, false, true, 100, typeof(FailedLoadStateException))]
        // Already unlocked recipe.
        [InlineData(new[] { 2 }, true, false, true, true, 100, typeof(AlreadyRecipeUnlockedException))]
        // Skip prev recipe.
        [InlineData(new[] { 3 }, true, false, false, true, 100, typeof(InvalidRecipeIdException))]
        // Stage not cleared.
        [InlineData(new[] { 2 }, true, false, false, false, 100, typeof(NotEnoughClearedStageLevelException))]
        // Insufficient CRYSTAL.
        [InlineData(new[] { 2 }, true, false, false, true, 50, typeof(NotEnoughFungibleAssetValueException))]
        public void Execute(
            IEnumerable<int> ids,
            bool stateExist,
            bool migrationRequired,
            bool alreadyUnlocked,
            bool stageCleared,
            int balance,
            Type exc
        )
        {
            var state = _initialState.MintAsset(_avatarAddress, balance * _currency);
            List<int> recipeIds = ids.ToList();
            Address unlockedRecipeIdsAddress = _avatarAddress.Derive("recipe_ids");
            if (stateExist)
            {
                var worldInformation = _avatarState.worldInformation;
                if (stageCleared)
                {
                    var stage = _tableSheets.EquipmentItemRecipeSheet[recipeIds.Max()].UnlockStage;
                    for (int i = 1; i < stage + 1; i++)
                    {
                        worldInformation.ClearStage(1, i, 0, _tableSheets.WorldSheet, _tableSheets.WorldUnlockSheet);
                    }
                }
                else
                {
                    Assert.All(recipeIds, recipeId => worldInformation.IsStageCleared(recipeId));
                }

                if (alreadyUnlocked)
                {
                    var serializedIds = new List(recipeIds.Select(i => i.Serialize()));
                    state = state.SetState(unlockedRecipeIdsAddress, serializedIds);
                }

                if (migrationRequired)
                {
                    state = state.SetState(_avatarAddress, _avatarState.Serialize());
                }
                else
                {
                    state = state
                        .SetState(_avatarAddress.Derive(LegacyInventoryKey), _avatarState.inventory.Serialize())
                        .SetState(_avatarAddress.Derive(LegacyWorldInformationKey), worldInformation.Serialize())
                        .SetState(_avatarAddress.Derive(LegacyQuestListKey), _avatarState.questList.Serialize())
                        .SetState(_avatarAddress, _avatarState.SerializeV2());
                }
            }

            var action = new UnlockEquipmentRecipe
            {
                RecipeIds = recipeIds.ToList(),
                AvatarAddress = _avatarAddress,
            };

            if (exc is null)
            {
                IAccountStateDelta nextState = action.Execute(new ActionContext
                {
                    PreviousStates = state,
                    Signer = _agentAddress,
                    BlockIndex = 1,
                    Random = _random,
                });

                Assert.True(nextState.TryGetState(unlockedRecipeIdsAddress, out List rawIds));

                var unlockedIds = rawIds.ToList(StateExtensions.ToInteger);

                Assert.All(recipeIds, recipeId => Assert.Contains(recipeId, unlockedIds));
                Assert.Equal(0 * _currency, nextState.GetBalance(_avatarAddress, _currency));
            }
            else
            {
                Assert.Throws(exc, () => action.Execute(new ActionContext
                {
                    PreviousStates = state,
                    Signer = _agentAddress,
                    BlockIndex = 1,
                    Random = _random,
                }));
            }
        }
    }
}
