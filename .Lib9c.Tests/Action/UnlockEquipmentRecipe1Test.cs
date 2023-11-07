namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;
    using static Lib9c.SerializeKeys;

    public class UnlockEquipmentRecipe1Test
    {
        private readonly IRandom _random;
        private readonly TableSheets _tableSheets;
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly AvatarState _avatarState;
        private readonly Currency _currency;
        private readonly IAccount _initialState;

        public UnlockEquipmentRecipe1Test()
        {
            _random = new TestRandom();
            var sheets = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(sheets);
            _agentAddress = new PrivateKey().ToAddress();
            _avatarAddress = new PrivateKey().ToAddress();
#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            _currency = Currency.Legacy("CRYSTAL", 18, null);
#pragma warning restore CS0618
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

            _initialState = new Account(MockState.Empty)
                .SetState(_agentAddress, agentState.Serialize())
                .SetState(Addresses.GetSheetAddress<EquipmentItemSheet>(), _tableSheets.EquipmentItemSheet.Serialize())
                .SetState(Addresses.GetSheetAddress<EquipmentItemRecipeSheet>(), _tableSheets.EquipmentItemRecipeSheet.Serialize())
                .SetState(Addresses.GameConfig, gameConfigState.Serialize());
        }

        [Theory]
        [InlineData(new[] { 6, 5 }, true, false, false, true, true, null)]
        [InlineData(new[] { 6 }, true, false, false, true, true, null)]
        // Unlock Belt without Armor unlock.
        [InlineData(new[] { 94 }, true, false, false, true, true, null)]
        // Unlock Weapon & Ring
        [InlineData(new[] { 6, 133 }, true, false, false, true, true, null)]
        // AvatarState migration.
        [InlineData(new[] { 6 }, true, true, false, true, true, null)]
        // Invalid recipe id.
        [InlineData(new[] { -1 }, true, false, false, false, false, typeof(InvalidRecipeIdException))]
        [InlineData(new[] { 1 }, true, false, false, true, false, typeof(InvalidRecipeIdException))]
        [InlineData(new int[] { }, true, false, false, false, false, typeof(InvalidRecipeIdException))]
        // AvatarState is null.
        [InlineData(new[] { 6 }, false, true, false, true, true, typeof(FailedLoadStateException))]
        [InlineData(new[] { 6 }, false, false, false, true, true, typeof(FailedLoadStateException))]
        // Already unlocked recipe.
        [InlineData(new[] { 6 }, true, false, true, true, true, typeof(AlreadyRecipeUnlockedException))]
        // Skip prev recipe.
        [InlineData(new[] { 5 }, true, false, false, true, false, typeof(InvalidRecipeIdException))]
        // Stage not cleared.
        [InlineData(new[] { 6 }, true, false, false, false, true, typeof(NotEnoughClearedStageLevelException))]
        // Insufficient CRYSTAL.
        [InlineData(new[] { 6 }, true, false, false, true, false, typeof(NotEnoughFungibleAssetValueException))]
        public void Execute(
            IEnumerable<int> ids,
            bool stateExist,
            bool migrationRequired,
            bool alreadyUnlocked,
            bool stageCleared,
            bool balanceEnough,
            Type exc
        )
        {
            var context = new ActionContext();
            List<int> recipeIds = ids.ToList();
            var rows = _tableSheets.EquipmentItemRecipeSheet.Values
                .Where(r => recipeIds.Contains(r.Id)).ToList();
            var balance = balanceEnough ? rows.Sum(r => r.CRYSTAL) : 1;
            var state = _initialState.MintAsset(context, _agentAddress, balance * _currency);
            Address unlockedRecipeIdsAddress = _avatarAddress.Derive("recipe_ids");
            if (stateExist)
            {
                var stage = rows.Any() ? rows.Max(r => r.UnlockStage) : 1;
                var worldSheet = _tableSheets.WorldSheet;
                var worldId = worldSheet.OrderedList
                    .Last(r => r.StageBegin <= stage && stage <= r.StageEnd).Id;
                var worldInformation = _avatarState.worldInformation;
                if (stageCleared)
                {
                    for (int j = 1; j < worldId + 1; j++)
                    {
                        for (var i = 1; i < stage + 1; i++)
                        {
                            _avatarState.worldInformation.ClearStage(
                                j,
                                i,
                                0,
                                _tableSheets.WorldSheet,
                                _tableSheets.WorldUnlockSheet
                            );
                        }
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

            var action = new UnlockEquipmentRecipe1
            {
                RecipeIds = recipeIds.ToList(),
                AvatarAddress = _avatarAddress,
            };

            if (exc is null)
            {
                IAccount nextState = action.Execute(new ActionContext
                {
                    PreviousState = state,
                    Signer = _agentAddress,
                    BlockIndex = 1,
                    RandomSeed = _random.Seed,
                });

                Assert.True(nextState.TryGetState(unlockedRecipeIdsAddress, out List rawIds));

                var unlockedIds = rawIds.ToList(StateExtensions.ToInteger);

                Assert.All(recipeIds, recipeId => Assert.Contains(recipeId, unlockedIds));
                Assert.Equal(0 * _currency, nextState.GetBalance(_agentAddress, _currency));
                Assert.Equal(balance * _currency, nextState.GetBalance(Addresses.UnlockEquipmentRecipe, _currency));
            }
            else
            {
                Assert.Throws(exc, () => action.Execute(new ActionContext
                {
                    PreviousState = state,
                    Signer = _agentAddress,
                    BlockIndex = 1,
                    RandomSeed = _random.Seed,
                }));
            }
        }

        [Theory]
        [InlineData(ItemSubType.Weapon)]
        [InlineData(ItemSubType.Armor)]
        [InlineData(ItemSubType.Belt)]
        [InlineData(ItemSubType.Necklace)]
        [InlineData(ItemSubType.Ring)]
        public void UnlockedIds(ItemSubType itemSubType)
        {
            var worldInformation = _avatarState.worldInformation;
            var rows = _tableSheets.EquipmentItemRecipeSheet.Values
                .Where(i => i.ItemSubType == itemSubType && i.Id != 1 && i.UnlockStage != 999);

            // Clear Stage
            for (int i = 0; i < _tableSheets.WorldSheet.Count; i++)
            {
                var worldRow = _tableSheets.WorldSheet.OrderedList[i];
                for (int v = worldRow.StageBegin; v < worldRow.StageEnd + 1; v++)
                {
                    worldInformation.ClearStage(worldRow.Id, v, 0, _tableSheets.WorldSheet, _tableSheets.WorldUnlockSheet);
                }
            }

            // Unlock All recipe by ItemSubType
            UnlockEquipmentRecipe1.UnlockedIds(_initialState, new PrivateKey().ToAddress(), _tableSheets.EquipmentItemRecipeSheet, worldInformation, rows.Select(i => i.Id).ToList());
        }
    }
}
