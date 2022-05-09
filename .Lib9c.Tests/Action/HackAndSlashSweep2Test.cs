namespace Lib9c.Tests.Action
{
    using System.Collections.Generic;
    using System.Linq;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Helper;
    using Nekoyume.Model;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;
    using static Lib9c.SerializeKeys;

    public class HackAndSlashSweep2Test
    {
        private readonly Dictionary<string, string> _sheets;
        private readonly TableSheets _tableSheets;

        private readonly Address _agentAddress;

        private readonly Address _avatarAddress;
        private readonly AvatarState _avatarState;

        private readonly Address _inventoryAddress;
        private readonly Address _worldInformationAddress;
        private readonly Address _questListAddress;

        private readonly Address _rankingMapAddress;

        private readonly WeeklyArenaState _weeklyArenaState;
        private readonly IAccountStateDelta _initialState;
        private readonly IRandom _random;

        public HackAndSlashSweep2Test()
        {
            _random = new TestRandom();
            _sheets = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(_sheets);

            var privateKey = new PrivateKey();
            _agentAddress = privateKey.PublicKey.ToAddress();
            var agentState = new AgentState(_agentAddress);

            _avatarAddress = _agentAddress.Derive("avatar");
            var gameConfigState = new GameConfigState(_sheets[nameof(GameConfigSheet)]);
            _rankingMapAddress = _avatarAddress.Derive("ranking_map");
            _avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                gameConfigState,
                _rankingMapAddress
            )
            {
                level = 100,
            };
            _inventoryAddress = _avatarAddress.Derive(LegacyInventoryKey);
            _worldInformationAddress = _avatarAddress.Derive(LegacyWorldInformationKey);
            _questListAddress = _avatarAddress.Derive(LegacyQuestListKey);
            agentState.avatarAddresses.Add(0, _avatarAddress);

            _weeklyArenaState = new WeeklyArenaState(0);

            _initialState = new State()
                .SetState(_weeklyArenaState.address, _weeklyArenaState.Serialize())
                .SetState(_agentAddress, agentState.SerializeV2())
                .SetState(_avatarAddress, _avatarState.SerializeV2())
                .SetState(_inventoryAddress, _avatarState.inventory.Serialize())
                .SetState(_worldInformationAddress, _avatarState.worldInformation.Serialize())
                .SetState(_questListAddress, _avatarState.questList.Serialize())
                .SetState(gameConfigState.address, gameConfigState.Serialize());

            foreach (var (key, value) in _sheets)
            {
                _initialState = _initialState
                    .SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            foreach (var address in _avatarState.combinationSlotAddresses)
            {
                var slotState = new CombinationSlotState(
                    address,
                    GameConfig.RequireClearedStageLevel.CombinationEquipmentAction);
                _initialState = _initialState.SetState(address, slotState.Serialize());
            }
        }

        [Theory]
        [InlineData(1, 1, 1, true)]
        [InlineData(1, 1, 1, false)]
        [InlineData(2, 1, 30, true)]
        [InlineData(2, 1, 30, false)]
        [InlineData(5, 4, 199, false)]
        [InlineData(5, 4, 199, true)]
        [InlineData(9, 5, 250, false)]
        [InlineData(9, 5, 250, true)]
        public void Execute(int apStoneCount, int worldId, int stageId, bool backward)
        {
            var gameConfigState = _initialState.GetGameConfigState();
            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _initialState.GetAvatarSheets(),
                gameConfigState,
                _rankingMapAddress)
            {
                worldInformation =
                    new WorldInformation(0, _initialState.GetSheet<WorldSheet>(), stageId),
            };

            var row = _tableSheets.MaterialItemSheet.Values.First(r =>
                r.ItemSubType == ItemSubType.ApStone);
            var apStone = ItemFactory.CreateTradableMaterial(row);
            avatarState.inventory.AddItem(apStone, apStoneCount);

            IAccountStateDelta state;
            if (backward)
            {
                state = _initialState.SetState(_avatarAddress, avatarState.Serialize());
            }
            else
            {
                state = _initialState
                    .SetState(_avatarAddress, avatarState.SerializeV2())
                    .SetState(
                        _avatarAddress.Derive(LegacyInventoryKey),
                        avatarState.inventory.Serialize())
                    .SetState(
                        _avatarAddress.Derive(LegacyWorldInformationKey),
                        avatarState.worldInformation.Serialize())
                    .SetState(
                        _avatarAddress.Derive(LegacyQuestListKey),
                        avatarState.questList.Serialize());
            }

            var stageSheet = _initialState.GetSheet<StageSheet>();
            var (expectedLevel, expectedExp) = (0, 0L);
            if (stageSheet.TryGetValue(stageId, out var stageRow))
            {
                var itemPlayCount = gameConfigState.ActionPointMax / stageRow.CostAP * apStoneCount;
                var apPlayCount = avatarState.actionPoint / stageRow.CostAP;
                var playCount = apPlayCount + itemPlayCount;
                (expectedLevel, expectedExp) = avatarState.GetLevelAndExp(
                    _tableSheets.CharacterLevelSheet,
                    stageId,
                    playCount);

                var random = new TestRandom(_random.Seed);
                var expectedRewardItems =
                    HackAndSlashSweep2.GetRewardItems(random, playCount, stageRow, _tableSheets.MaterialItemSheet);

                var action = new HackAndSlashSweep2
                {
                    avatarAddress = _avatarAddress,
                    apStoneCount = apStoneCount,
                    worldId = worldId,
                    stageId = stageId,
                };

                state = action.Execute(new ActionContext
                {
                    PreviousStates = state,
                    Signer = _agentAddress,
                    Random = _random,
                });

                var nextAvatarState = state.GetAvatarStateV2(_avatarAddress);

                Assert.Equal(expectedLevel, nextAvatarState.level);
                Assert.Equal(expectedExp, nextAvatarState.exp);
                Assert.Equal(expectedRewardItems.Count(), nextAvatarState.inventory.Items.Sum(x => x.count));
                foreach (var i in nextAvatarState.inventory.Items)
                {
                    nextAvatarState.inventory.TryGetItem(i.item.Id, out var item);
                    Assert.Equal(expectedRewardItems.Count(x => x.Id == i.item.Id), item.count);
                }
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Execute_FailedLoadStateException(bool backward)
        {
            var action = new HackAndSlashSweep2
            {
                apStoneCount = 1,
                avatarAddress = _avatarAddress,
                worldId = 1,
                stageId = 1,
            };

            var state = backward ? new State() : _initialState;
            if (!backward)
            {
                state = _initialState
                    .SetState(_avatarAddress, _avatarState.SerializeV2())
                    .SetState(_avatarAddress.Derive(LegacyInventoryKey), null!)
                    .SetState(_avatarAddress.Derive(LegacyWorldInformationKey), null!)
                    .SetState(_avatarAddress.Derive(LegacyQuestListKey), null!);
            }

            Assert.Throws<FailedLoadStateException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Signer = _agentAddress,
                Random = new TestRandom(),
            }));
        }

        [Theory]
        [InlineData(100, 1)]
        public void Execute_SheetRowNotFoundException(int worldId, int stageId)
        {
            var action = new HackAndSlashSweep2
            {
                apStoneCount = 1,
                avatarAddress = _avatarAddress,
                worldId = worldId,
                stageId = stageId,
            };

            Assert.Throws<SheetRowNotFoundException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = _initialState,
                Signer = _agentAddress,
                Random = new TestRandom(),
            }));
        }

        [Theory]
        [InlineData(1, 999)]
        public void Execute_SheetRowColumnException(int worldId, int stageId)
        {
            var action = new HackAndSlashSweep2
            {
                apStoneCount = 1,
                avatarAddress = _avatarAddress,
                worldId = worldId,
                stageId = stageId,
            };

            Assert.Throws<SheetRowColumnException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = _initialState,
                Signer = _agentAddress,
                Random = new TestRandom(),
            }));
        }

        [Fact]
        public void Execute_StageClearedException()
        {
            var action = new HackAndSlashSweep2
            {
                apStoneCount = 1,
                avatarAddress = _avatarAddress,
                worldId = 1,
                stageId = 50,
            };

            Assert.Throws<StageClearedException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = _initialState,
                Signer = _agentAddress,
                Random = new TestRandom(),
            }));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Execute_InvalidStageException(bool backward)
        {
            var action = new HackAndSlashSweep2
            {
                apStoneCount = 1,
                avatarAddress = _avatarAddress,
                worldId = 1,
                stageId = 50,
            };
            var worldSheet = _initialState.GetSheet<WorldSheet>();
            var worldUnlockSheet = _initialState.GetSheet<WorldUnlockSheet>();

            _avatarState.worldInformation.ClearStage(1, 2, 1, worldSheet, worldUnlockSheet);

            var state = _initialState;
            if (backward)
            {
                state = _initialState.SetState(_avatarAddress, _avatarState.Serialize());
            }
            else
            {
                state = _initialState
                    .SetState(
                        _avatarAddress.Derive(LegacyWorldInformationKey),
                        _avatarState.worldInformation.Serialize());
            }

            Assert.Throws<InvalidStageException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Signer = _agentAddress,
                Random = new TestRandom(),
            }));
        }

        [Theory]
        [InlineData(GameConfig.MimisbrunnrWorldId, true)]
        [InlineData(GameConfig.MimisbrunnrWorldId, false)]
        public void Execute_InvalidWorldException(int worldId, bool backward)
        {
            var gameConfigState = new GameConfigState(_sheets[nameof(GameConfigSheet)]);
            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _initialState.GetAvatarSheets(),
                gameConfigState,
                _rankingMapAddress)
            {
                worldInformation =
                    new WorldInformation(0, _initialState.GetSheet<WorldSheet>(), 10000001),
            };

            IAccountStateDelta state;
            if (backward)
            {
                state = _initialState.SetState(_avatarAddress, avatarState.Serialize());
            }
            else
            {
                state = _initialState
                    .SetState(_avatarAddress, avatarState.SerializeV2())
                    .SetState(
                        _avatarAddress.Derive(LegacyInventoryKey),
                        avatarState.inventory.Serialize())
                    .SetState(
                        _avatarAddress.Derive(LegacyWorldInformationKey),
                        avatarState.worldInformation.Serialize())
                    .SetState(
                        _avatarAddress.Derive(LegacyQuestListKey),
                        avatarState.questList.Serialize());
            }

            var action = new HackAndSlashSweep2
            {
                apStoneCount = 1,
                avatarAddress = _avatarAddress,
                worldId = worldId,
                stageId = 10000001,
            };

            Assert.Throws<InvalidWorldException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Signer = _agentAddress,
                Random = new TestRandom(),
            }));
        }

        [Theory]
        [InlineData(99, true)]
        [InlineData(99, false)]
        public void Execute_UsageLimitExceedException(int apStoneCount, bool backward)
        {
            var gameConfigState = new GameConfigState(_sheets[nameof(GameConfigSheet)]);
            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _initialState.GetAvatarSheets(),
                gameConfigState,
                _rankingMapAddress)
            {
                worldInformation =
                    new WorldInformation(0, _initialState.GetSheet<WorldSheet>(), 25),
            };

            IAccountStateDelta state;
            if (backward)
            {
                state = _initialState.SetState(_avatarAddress, avatarState.Serialize());
            }
            else
            {
                state = _initialState
                    .SetState(_avatarAddress, avatarState.SerializeV2())
                    .SetState(
                        _avatarAddress.Derive(LegacyInventoryKey),
                        avatarState.inventory.Serialize())
                    .SetState(
                        _avatarAddress.Derive(LegacyWorldInformationKey),
                        avatarState.worldInformation.Serialize())
                    .SetState(
                        _avatarAddress.Derive(LegacyQuestListKey),
                        avatarState.questList.Serialize());
            }

            var action = new HackAndSlashSweep2
            {
                apStoneCount = apStoneCount,
                avatarAddress = _avatarAddress,
                worldId = 1,
                stageId = 25,
            };

            Assert.Throws<UsageLimitExceedException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Signer = _agentAddress,
                Random = new TestRandom(),
            }));
        }

        [Theory]
        [InlineData(3, 2, true)]
        [InlineData(7, 5, false)]
        public void Execute_NotEnoughMaterialException(int useApStoneCount, int holdingApStoneCount, bool backward)
        {
            var gameConfigState = _initialState.GetGameConfigState();
            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _initialState.GetAvatarSheets(),
                gameConfigState,
                _rankingMapAddress)
            {
                worldInformation =
                    new WorldInformation(0, _initialState.GetSheet<WorldSheet>(), 25),
            };

            var row = _tableSheets.MaterialItemSheet.Values.First(r =>
                r.ItemSubType == ItemSubType.ApStone);
            var apStone = ItemFactory.CreateTradableMaterial(row);
            avatarState.inventory.AddItem(apStone, holdingApStoneCount);

            IAccountStateDelta state;
            if (backward)
            {
                state = _initialState.SetState(_avatarAddress, avatarState.Serialize());
            }
            else
            {
                state = _initialState
                    .SetState(_avatarAddress, avatarState.SerializeV2())
                    .SetState(
                        _avatarAddress.Derive(LegacyInventoryKey),
                        avatarState.inventory.Serialize())
                    .SetState(
                        _avatarAddress.Derive(LegacyWorldInformationKey),
                        avatarState.worldInformation.Serialize())
                    .SetState(
                        _avatarAddress.Derive(LegacyQuestListKey),
                        avatarState.questList.Serialize());
            }

            var stageSheet = _initialState.GetSheet<StageSheet>();
            var (expectedLevel, expectedExp) = (0, 0L);
            if (stageSheet.TryGetValue(25, out var stageRow))
            {
                var itemPlayCount =
                    gameConfigState.ActionPointMax / stageRow.CostAP * useApStoneCount;
                var apPlayCount = avatarState.actionPoint / stageRow.CostAP;
                var playCount = apPlayCount + itemPlayCount;
                (expectedLevel, expectedExp) = avatarState.GetLevelAndExp(
                    _tableSheets.CharacterLevelSheet,
                    25,
                    playCount);

                var action = new HackAndSlashSweep2
                {
                    avatarAddress = _avatarAddress,
                    apStoneCount = useApStoneCount,
                    worldId = 1,
                    stageId = 25,
                };

                Assert.Throws<NotEnoughMaterialException>(() => action.Execute(new ActionContext()
                {
                    PreviousStates = state,
                    Signer = _agentAddress,
                    Random = new TestRandom(),
                }));
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Execute_NotEnoughActionPointException(bool backward)
        {
            var gameConfigState = _initialState.GetGameConfigState();
            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _initialState.GetAvatarSheets(),
                gameConfigState,
                _rankingMapAddress)
            {
                worldInformation =
                    new WorldInformation(0, _initialState.GetSheet<WorldSheet>(), 25),
                actionPoint = 0,
            };

            IAccountStateDelta state;
            if (backward)
            {
                state = _initialState.SetState(_avatarAddress, avatarState.Serialize());
            }
            else
            {
                state = _initialState
                    .SetState(_avatarAddress, avatarState.SerializeV2())
                    .SetState(
                        _avatarAddress.Derive(LegacyInventoryKey),
                        avatarState.inventory.Serialize())
                    .SetState(
                        _avatarAddress.Derive(LegacyWorldInformationKey),
                        avatarState.worldInformation.Serialize())
                    .SetState(
                        _avatarAddress.Derive(LegacyQuestListKey),
                        avatarState.questList.Serialize());
            }

            var stageSheet = _initialState.GetSheet<StageSheet>();
            var (expectedLevel, expectedExp) = (0, 0L);
            if (stageSheet.TryGetValue(25, out var stageRow))
            {
                var itemPlayCount =
                    gameConfigState.ActionPointMax / stageRow.CostAP * 1;
                var apPlayCount = avatarState.actionPoint / stageRow.CostAP;
                var playCount = apPlayCount + itemPlayCount;
                (expectedLevel, expectedExp) = avatarState.GetLevelAndExp(
                    _tableSheets.CharacterLevelSheet,
                    25,
                    playCount);

                var action = new HackAndSlashSweep2
                {
                    avatarAddress = _avatarAddress,
                    apStoneCount = 0,
                    worldId = 1,
                    stageId = 25,
                };

                Assert.Throws<NotEnoughActionPointException>(() => action.Execute(new ActionContext()
                {
                    PreviousStates = state,
                    Signer = _agentAddress,
                    Random = new TestRandom(),
                }));
            }
        }
    }
}
