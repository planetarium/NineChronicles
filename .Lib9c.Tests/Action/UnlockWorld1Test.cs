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
    using Nekoyume.Helper;
    using Nekoyume.Model;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;
    using static Lib9c.SerializeKeys;

    public class UnlockWorld1Test
    {
        private readonly IRandom _random;
        private readonly TableSheets _tableSheets;
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly AvatarState _avatarState;
        private readonly Currency _currency;
        private readonly IAccountStateDelta _initialState;

        public UnlockWorld1Test()
        {
            _random = new TestRandom();
            var sheets = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(sheets);
            _agentAddress = new PrivateKey().ToAddress();
            _avatarAddress = new PrivateKey().ToAddress();
            _currency = CrystalCalculator.CRYSTAL;
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
                .SetState(Addresses.GetSheetAddress<WorldUnlockSheet>(), _tableSheets.WorldUnlockSheet.Serialize())
                .SetState(Addresses.GameConfig, gameConfigState.Serialize());
        }

        [Theory]
        [InlineData(new[] { 2 }, true, false, false, true, 500, null)]
        // Migration AvatarState.
        [InlineData(new[] { 2, 3, 4, 5 }, true, true, false, true, 153000, null)]
        // Try open Yggdrasil.
        [InlineData(new[] { 1 }, false, true, false, true, 0, typeof(InvalidWorldException))]
        // Try open Mimisbrunnr.
        [InlineData(new[] { GameConfig.MimisbrunnrWorldId }, false, true, false, true, 0, typeof(InvalidWorldException))]
        // Empty WorldId.
        [InlineData(new int[] { }, false, true, false, true, 0, typeof(InvalidWorldException))]
        // AvatarState is null.
        [InlineData(new[] { 2 }, false, true, false, true, 0, typeof(FailedLoadStateException))]
        [InlineData(new[] { 2 }, false, false, false, true, 0, typeof(FailedLoadStateException))]
        // Already unlocked world.
        [InlineData(new[] { 2 }, true, false, true, true, 0, typeof(AlreadyWorldUnlockedException))]
        // Skip previous world.
        [InlineData(new[] { 3 }, true, false, false, true, 0, typeof(FailedToUnlockWorldException))]
        // Stage not cleared.
        [InlineData(new[] { 2 }, true, false, false, false, 0, typeof(FailedToUnlockWorldException))]
        // Insufficient CRYSTAL.
        [InlineData(new[] { 2 }, true, false, false, true, 100, typeof(NotEnoughFungibleAssetValueException))]
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
            var state = _initialState.MintAsset(_agentAddress, balance * _currency);
            var worldIds = ids.ToList();

            if (stateExist)
            {
                var worldInformation = _avatarState.worldInformation;
                if (stageCleared)
                {
                    foreach (var wordId in worldIds)
                    {
                        var row = _tableSheets.WorldUnlockSheet.OrderedList.First(r =>
                            r.WorldIdToUnlock == wordId);
                        var worldRow = _tableSheets.WorldSheet[row.WorldId];
                        var prevRow =
                            _tableSheets.WorldUnlockSheet.OrderedList.FirstOrDefault(r =>
                                r.WorldIdToUnlock == row.WorldId);
                        // Clear prev world.
                        if (!(prevRow is null))
                        {
                            var prevWorldRow = _tableSheets.WorldSheet[prevRow.WorldId];
                            for (int i = prevWorldRow.StageBegin; i < prevWorldRow.StageEnd + 1; i++)
                            {
                                worldInformation.ClearStage(prevWorldRow.Id, i, 0, _tableSheets.WorldSheet, _tableSheets.WorldUnlockSheet);
                            }
                        }

                        for (int i = worldRow.StageBegin; i < worldRow.StageEnd + 1; i++)
                        {
                            worldInformation.ClearStage(worldRow.Id, i, 0, _tableSheets.WorldSheet, _tableSheets.WorldUnlockSheet);
                        }
                    }
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

            var unlockedWorldIdsAddress = _avatarAddress.Derive("world_ids");
            if (alreadyUnlocked)
            {
                var unlockIds = List.Empty.Add(1.Serialize());
                foreach (var worldId in worldIds)
                {
                    unlockIds = unlockIds.Add(worldId.Serialize());
                }

                state = state.SetState(unlockedWorldIdsAddress, unlockIds);
            }

            var action = new UnlockWorld1
            {
                WorldIds = worldIds,
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

                Assert.True(nextState.TryGetState(unlockedWorldIdsAddress, out List rawIds));

                var unlockedIds = rawIds.ToList(StateExtensions.ToInteger);

                Assert.All(worldIds, worldId => Assert.Contains(worldId, unlockedIds));
                Assert.Equal(0 * _currency, nextState.GetBalance(_agentAddress, _currency));
                Assert.Equal(balance * _currency, nextState.GetBalance(Addresses.UnlockWorld, _currency));
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
