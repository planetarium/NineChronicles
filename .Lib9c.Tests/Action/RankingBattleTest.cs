namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Battle;
    using Nekoyume.Model;
    using Nekoyume.Model.BattleStatus;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Stat;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;
    using static SerializeKeys;

    public class RankingBattleTest
    {
        private readonly TableSheets _tableSheets;
        private readonly Address _agent1Address;
        private readonly Address _avatar1Address;
        private readonly Address _avatar2Address;
        private readonly Address _weeklyArenaAddress;
        private readonly IAccountStateDelta _initialState;

        public RankingBattleTest(ITestOutputHelper outputHelper)
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

            var rankingMapAddress = new PrivateKey().ToAddress();

            var (agent1State, avatar1State) = GetAgentStateWithAvatarState(
                sheets,
                _tableSheets,
                rankingMapAddress);
            _agent1Address = agent1State.address;
            _avatar1Address = avatar1State.address;

            var (agent2State, avatar2State) = GetAgentStateWithAvatarState(
                sheets,
                _tableSheets,
                rankingMapAddress);
            var agent2Address = agent2State.address;
            _avatar2Address = avatar2State.address;

            var weeklyArenaState = new WeeklyArenaState(0);
            var weeklyAddressListAddress = weeklyArenaState.address.Derive("address_list");
            var weeklyAddressList = new List<Address>
            {
                _avatar1Address,
                _avatar2Address,
            };
            var arenaInfo1Address = weeklyArenaState.address.Derive(_avatar1Address.ToByteArray());
            var arenaInfo1 = new ArenaInfo(
                avatar1State,
                _tableSheets.CharacterSheet,
                _tableSheets.CostumeStatSheet,
                true);
            var arenaInfo2Address = weeklyArenaState.address.Derive(_avatar2Address.ToByteArray());
            var arenaInfo2 = new ArenaInfo(
                avatar2State,
                _tableSheets.CharacterSheet,
                _tableSheets.CostumeStatSheet,
                true);
            _weeklyArenaAddress = weeklyArenaState.address;

            _initialState = _initialState
                .SetState(_agent1Address, agent1State.Serialize())
                .SetState(_avatar1Address, avatar1State.Serialize())
                .SetState(agent2Address, agent2State.Serialize())
                .SetState(_avatar2Address, avatar2State.Serialize())
                .SetState(Addresses.GameConfig, new GameConfigState(sheets[nameof(GameConfigSheet)]).Serialize())
                .SetState(_weeklyArenaAddress, weeklyArenaState.Serialize())
                .SetState(
                    weeklyAddressListAddress,
                    weeklyAddressList.Aggregate(List.Empty, (list, address) => list.Add(address.Serialize())))
                .SetState(arenaInfo1Address, arenaInfo1.Serialize())
                .SetState(arenaInfo2Address, arenaInfo2.Serialize());

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();
        }

        public static (AgentState, AvatarState) GetAgentStateWithAvatarState(
            IReadOnlyDictionary<string, string> sheets,
            TableSheets tableSheets,
            Address rankingMapAddress)
        {
            var agentAddress = new PrivateKey().ToAddress();
            var agentState = new AgentState(agentAddress);

            var avatarAddress = agentAddress.Derive("avatar");
            var avatarState = new AvatarState(
                avatarAddress,
                agentAddress,
                0,
                tableSheets.GetAvatarSheets(),
                new GameConfigState(sheets[nameof(GameConfigSheet)]),
                rankingMapAddress)
            {
                worldInformation = new WorldInformation(
                    0,
                    tableSheets.WorldSheet,
                    Math.Max(
                        tableSheets.StageSheet.First?.Id ?? 1,
                        GameConfig.RequireClearedStageLevel.ActionsInRankingBoard)),
            };
            agentState.avatarAddresses.Add(0, avatarAddress);

            return (agentState, avatarState);
        }

        [Fact]
        public void Execute()
        {
            var previousState = _initialState;
            var previousWeeklyState = previousState.GetWeeklyArenaState(0);
            var previousAvatar1State = previousState.GetAvatarState(_avatar1Address);
            previousAvatar1State.level = 10;
            previousState = previousState.SetState(
                _avatar1Address,
                previousAvatar1State.Serialize());

            var previousArenaInfoAddress = previousWeeklyState.address.Derive(_avatar1Address.ToByteArray());
            var previousArenaInfo =
                new ArenaInfo((Dictionary)previousState.GetState(previousArenaInfoAddress));
            var previousScore = previousArenaInfo.Score;

            var itemIds = _tableSheets.WeeklyArenaRewardSheet.Values
                .Select(r => r.Reward.ItemId)
                .ToList();

            Assert.All(itemIds, id => Assert.False(previousAvatar1State.inventory.HasItem(id)));

            var previousEnemyAvatarState = previousState.GetAvatarState(_avatar2Address);
            var previousEnemyArenaInfoAddress = previousWeeklyState.address.Derive(_avatar2Address.ToByteArray());
            var previousEnemyArenaInfo =
                new ArenaInfo((Dictionary)previousState.GetState(previousEnemyArenaInfoAddress));
            previousState = SetState(
                previousState,
                _tableSheets,
                previousAvatar1State,
                false,
                previousEnemyAvatarState,
                false,
                out var costumeNonFungibleId);

            var action = new RankingBattle
            {
                avatarAddress = _avatar1Address,
                enemyAddress = _avatar2Address,
                weeklyArenaAddress = _weeklyArenaAddress,
                costumeIds = new List<Guid> { costumeNonFungibleId },
                equipmentIds = new List<Guid>(),
            };

            var nextState = action.Execute(new ActionContext
            {
                PreviousStates = previousState,
                Signer = _agent1Address,
                Random = new TestRandom(),
                Rehearsal = false,
            });
            Assert.NotNull(action.ArenaInfo);
            Assert.NotNull(action.EnemyArenaInfo);

            var nextWeeklyArenaState = nextState.GetWeeklyArenaState(0);
            Assert.True(nextState.TryGetState(
                nextWeeklyArenaState.address.Derive(_avatar1Address.ToByteArray()),
                out Dictionary rawArenaInfo));
            var nextArenaInfo = new ArenaInfo(rawArenaInfo);
            Assert.True(nextArenaInfo.Score > previousScore);

            Assert.True(nextState.TryGetState(
                nextWeeklyArenaState.address.Derive(_avatar2Address.ToByteArray()),
                out Dictionary _));

            Assert.True(nextState.TryGetState(nextWeeklyArenaState.address.Derive("address_list"), out List rawAddressList));
            var addressList = rawAddressList.ToList(StateExtensions.ToAddress);
            Assert.Contains(_avatar1Address, addressList);
            Assert.Contains(_avatar2Address, addressList);

            Assert.Empty(nextWeeklyArenaState.Map);

            var nextAvatar1State = nextState.GetAvatarStateV2(_avatar1Address);
            Assert.Contains(nextAvatar1State.inventory.Materials, i => itemIds.Contains(i.Id));

            // Check simulation result equal.
            var player = new Player(
                previousAvatar1State,
                _tableSheets.CharacterSheet,
                _tableSheets.CharacterLevelSheet,
                _tableSheets.EquipmentItemSetEffectSheet);
            var simulator = new RankingSimulator(
                new TestRandom(),
                player,
                new EnemyPlayerDigest(previousEnemyAvatarState),
                new List<Guid>(),
                _tableSheets.GetRankingSimulatorSheets(),
                RankingBattle.StageId,
                _tableSheets.CostumeStatSheet);
            simulator.Simulate();
            var rewards = RewardSelector.Select(
                new TestRandom(),
                _tableSheets.WeeklyArenaRewardSheet,
                _tableSheets.MaterialItemSheet,
                player.Level,
                previousArenaInfo.GetRewardCount());
            var challengerScoreDelta = previousArenaInfo.Update(
                previousEnemyArenaInfo,
                simulator.Result,
                ArenaScoreHelper.GetScore);
            simulator.PostSimulate(rewards, challengerScoreDelta, previousArenaInfo.Score);

            Assert.Equal(nextArenaInfo.Score, simulator.Log.score);
            Assert.Equal(previousAvatar1State.SerializeV2(), nextAvatar1State.SerializeV2());
            Assert.Equal(previousAvatar1State.worldInformation.Serialize(), nextAvatar1State.worldInformation.Serialize());
        }

        [Theory]
        [InlineData(true, true, true)]
        [InlineData(true, true, false)]
        [InlineData(true, false, true)]
        [InlineData(true, false, false)]
        [InlineData(false, true, true)]
        [InlineData(false, true, false)]
        [InlineData(false, false, true)]
        [InlineData(false, false, false)]
        public void Execute_Backward_Compatible(bool isNew, bool avatarBackward, bool enemyBackward)
        {
            var previousState = _initialState;
            var previousWeeklyState = previousState.GetWeeklyArenaState(0);
            var previousAvatar1State = previousState.GetAvatarState(_avatar1Address);
            previousAvatar1State.level = 10;
            previousState = previousState.SetState(
                _avatar1Address,
                previousAvatar1State.Serialize());

            var previousArenaInfoAddress = previousWeeklyState.address.Derive(_avatar1Address.ToByteArray());
            var previousArenaInfo =
                new ArenaInfo((Dictionary)previousState.GetState(previousArenaInfoAddress));
            var previousScore = previousArenaInfo.Score;
            if (isNew)
            {
                var addressListAddress = previousWeeklyState.address.Derive("address_list");
                if (previousState.TryGetState(addressListAddress, out List rawAddressList))
                {
                    rawAddressList = new List(rawAddressList.Remove(_avatar1Address.Serialize()));
                    previousState = previousState.SetState(addressListAddress, rawAddressList);
                }
            }

            var itemIds = _tableSheets.WeeklyArenaRewardSheet.Values
                .Select(r => r.Reward.ItemId)
                .ToList();

            Assert.All(itemIds, id => Assert.False(previousAvatar1State.inventory.HasItem(id)));

            var previousEnemyAvatarState = previousState.GetAvatarState(_avatar2Address);
            var previousEnemyArenaInfoAddress = previousWeeklyState.address.Derive(_avatar2Address.ToByteArray());
            var previousEnemyArenaInfo =
                new ArenaInfo((Dictionary)previousState.GetState(previousEnemyArenaInfoAddress));
            previousState = SetState(
                previousState,
                _tableSheets,
                previousAvatar1State,
                avatarBackward,
                previousEnemyAvatarState,
                enemyBackward,
                out var costumeNonFungibleId);

            var action = new RankingBattle
            {
                avatarAddress = _avatar1Address,
                enemyAddress = _avatar2Address,
                weeklyArenaAddress = _weeklyArenaAddress,
                costumeIds = new List<Guid> { costumeNonFungibleId },
                equipmentIds = new List<Guid>(),
            };

            var nextState = action.Execute(new ActionContext
            {
                PreviousStates = previousState,
                Signer = _agent1Address,
                Random = new TestRandom(),
                Rehearsal = false,
            });
            Assert.NotNull(action.ArenaInfo);
            Assert.NotNull(action.EnemyArenaInfo);

            var nextAvatar1State = nextState.GetAvatarStateV2(_avatar1Address);
            Assert.Contains(nextAvatar1State.inventory.Materials, i => itemIds.Contains(i.Id));

            var nextWeeklyArenaState = nextState.GetWeeklyArenaState(0);
            var nextArenaInfo1Address = nextWeeklyArenaState.address.Derive(_avatar1Address.ToByteArray());
            var nextArenaInfo1 =
                new ArenaInfo((Dictionary)nextState.GetState(nextArenaInfo1Address));
            Assert.True(nextArenaInfo1.Score > previousScore);

            // Check simulation result equal.
            var player = new Player(
                previousAvatar1State,
                _tableSheets.CharacterSheet,
                _tableSheets.CharacterLevelSheet,
                _tableSheets.EquipmentItemSetEffectSheet);
            var simulator = new RankingSimulator(
                new TestRandom(),
                player,
                new EnemyPlayerDigest(previousEnemyAvatarState),
                new List<Guid>(),
                _tableSheets.GetRankingSimulatorSheets(),
                RankingBattle.StageId,
                _tableSheets.CostumeStatSheet);
            simulator.Simulate();
            var rewards = RewardSelector.Select(
                new TestRandom(),
                _tableSheets.WeeklyArenaRewardSheet,
                _tableSheets.MaterialItemSheet,
                player.Level,
                previousArenaInfo.GetRewardCount());
            var challengerScoreDelta = previousArenaInfo.Update(
                previousEnemyArenaInfo,
                simulator.Result,
                ArenaScoreHelper.GetScore);
            simulator.PostSimulate(rewards, challengerScoreDelta, previousArenaInfo.Score);

            Assert.Equal(nextArenaInfo1.Score, simulator.Log.score);
            Assert.Equal(previousAvatar1State.SerializeV2(), nextAvatar1State.SerializeV2());
            Assert.Equal(previousAvatar1State.worldInformation.Serialize(), nextAvatar1State.worldInformation.Serialize());
        }

        [Fact]
        public void ExecuteThrowInvalidAddressException()
        {
            var action = new RankingBattle
            {
                avatarAddress = _avatar1Address,
                enemyAddress = _avatar1Address,
                weeklyArenaAddress = _weeklyArenaAddress,
                costumeIds = new List<Guid>(),
                equipmentIds = new List<Guid>(),
            };

            Assert.Throws<InvalidAddressException>(() =>
            {
                action.Execute(new ActionContext
                {
                    PreviousStates = _initialState,
                    Signer = _agent1Address,
                    Random = new TestRandom(),
                    Rehearsal = false,
                });
            });
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public void ExecuteThrowFailedLoadStateException(int caseIndex)
        {
            Address signer = default;
            Address avatarAddress = default;
            Address enemyAddress = default;

            switch (caseIndex)
            {
                case 0:
                    signer = new PrivateKey().ToAddress();
                    avatarAddress = _avatar1Address;
                    enemyAddress = _avatar2Address;
                    break;
                case 1:
                    signer = _agent1Address;
                    avatarAddress = _avatar1Address;
                    enemyAddress = new PrivateKey().ToAddress();
                    break;
            }

            var action = new RankingBattle
            {
                avatarAddress = avatarAddress,
                enemyAddress = enemyAddress,
                weeklyArenaAddress = _weeklyArenaAddress,
                costumeIds = new List<Guid>(),
                equipmentIds = new List<Guid>(),
            };

            Assert.Throws<FailedLoadStateException>(() =>
            {
                action.Execute(new ActionContext
                {
                    PreviousStates = _initialState,
                    Signer = signer,
                    Random = new TestRandom(),
                    Rehearsal = false,
                });
            });
        }

        [Fact]
        public void ExecuteThrowNotEnoughClearedStageLevelException()
        {
            var previousAvatar1State = _initialState.GetAvatarState(_avatar1Address);
            previousAvatar1State.worldInformation = new WorldInformation(
                0,
                _tableSheets.WorldSheet,
                false
            );
            var previousState = _initialState.SetState(
                _avatar1Address,
                previousAvatar1State.Serialize());

            var action = new RankingBattle
            {
                avatarAddress = _avatar1Address,
                enemyAddress = _avatar2Address,
                weeklyArenaAddress = _weeklyArenaAddress,
                costumeIds = new List<Guid>(),
                equipmentIds = new List<Guid>(),
            };

            Assert.Throws<NotEnoughClearedStageLevelException>(() =>
            {
                action.Execute(new ActionContext
                {
                    PreviousStates = previousState,
                    Signer = _agent1Address,
                    Random = new TestRandom(),
                    Rehearsal = false,
                });
            });
        }

        [Fact]
        public void ExecuteThrowWeeklyArenaStateAlreadyEndedException()
        {
            var previousWeeklyArenaState = _initialState.GetWeeklyArenaState(_weeklyArenaAddress);
            previousWeeklyArenaState.Ended = true;

            var previousState = _initialState.SetState(
                _weeklyArenaAddress,
                previousWeeklyArenaState.Serialize());

            var action = new RankingBattle
            {
                avatarAddress = _avatar1Address,
                enemyAddress = _avatar2Address,
                weeklyArenaAddress = _weeklyArenaAddress,
                costumeIds = new List<Guid>(),
                equipmentIds = new List<Guid>(),
            };

            Assert.Throws<WeeklyArenaStateAlreadyEndedException>(() =>
            {
                action.Execute(new ActionContext
                {
                    PreviousStates = previousState,
                    Signer = _agent1Address,
                    Random = new TestRandom(),
                    Rehearsal = false,
                });
            });
        }

        [Fact]
        public void ExecuteThrowNotEnoughWeeklyArenaChallengeCountException()
        {
            var previousArenaInfoAddress = _weeklyArenaAddress.Derive(_avatar1Address.ToByteArray());
            var previousArenaInfo = new ArenaInfo((Dictionary)_initialState.GetState(previousArenaInfoAddress));
            var previousAvatarState = _initialState.GetAvatarState(_avatar1Address);
            while (true)
            {
                previousArenaInfo.UpdateV3(previousAvatarState, previousArenaInfo, BattleLog.Result.Lose);
                if (previousArenaInfo.DailyChallengeCount == 0)
                {
                    break;
                }
            }

            var previousState = _initialState.SetState(
                previousArenaInfoAddress,
                previousArenaInfo.Serialize());

            var action = new RankingBattle
            {
                avatarAddress = _avatar1Address,
                enemyAddress = _avatar2Address,
                weeklyArenaAddress = _weeklyArenaAddress,
                costumeIds = new List<Guid>(),
                equipmentIds = new List<Guid>(),
            };

            Assert.Throws<NotEnoughWeeklyArenaChallengeCountException>(() =>
            {
                action.Execute(new ActionContext
                {
                    PreviousStates = previousState,
                    Signer = _agent1Address,
                    Random = new TestRandom(),
                    Rehearsal = false,
                });
            });
        }

        [Theory]
        [InlineData(15)]
        [InlineData(30)]
        [InlineData(50)]
        [InlineData(75)]
        [InlineData(100)]
        [InlineData(120)]
        [InlineData(150)]
        [InlineData(200)]
        public void Execute_Throw_NotEnoughAvatarLevelException(int avatarLevel)
        {
            var state = _initialState;
            var avatarState = state.GetAvatarState(_avatar1Address);
            avatarState.level = avatarLevel;
            var enemyAddress = _avatar2Address;

            var previousWeeklyArenaState = state.GetWeeklyArenaState(_weeklyArenaAddress);

            state = state.SetState(
                _weeklyArenaAddress,
                previousWeeklyArenaState.Serialize());

            var itemIds = new[] { GameConfig.DefaultAvatarWeaponId, 40100000 };
            foreach (var itemId in itemIds)
            {
                foreach (var requirementRow in _tableSheets.ItemRequirementSheet.OrderedList
                    .Where(e => e.ItemId >= itemId && e.Level > avatarState.level)
                    .Take(3))
                {
                    var costumes = new List<Guid>();
                    var equipments = new List<Guid>();
                    var random = new TestRandom(DateTimeOffset.Now.Millisecond);
                    if (_tableSheets.EquipmentItemSheet.TryGetValue(requirementRow.ItemId, out var row))
                    {
                        var equipment = ItemFactory.CreateItem(row, random);
                        avatarState.inventory.AddItem(equipment);
                        equipments.Add(((INonFungibleItem)equipment).NonFungibleId);
                    }
                    else if (_tableSheets.CostumeItemSheet.TryGetValue(requirementRow.ItemId, out var row2))
                    {
                        var costume = ItemFactory.CreateItem(row2, random);
                        avatarState.inventory.AddItem(costume);
                        costumes.Add(((INonFungibleItem)costume).NonFungibleId);
                    }

                    state = state.SetState(avatarState.address, avatarState.SerializeV2())
                        .SetState(
                            avatarState.address.Derive(LegacyInventoryKey),
                            avatarState.inventory.Serialize())
                        .SetState(
                            avatarState.address.Derive(LegacyWorldInformationKey),
                            avatarState.worldInformation.Serialize())
                        .SetState(
                            avatarState.address.Derive(LegacyQuestListKey),
                            avatarState.questList.Serialize());

                    var action = new RankingBattle
                    {
                        avatarAddress = avatarState.address,
                        enemyAddress = enemyAddress,
                        weeklyArenaAddress = _weeklyArenaAddress,
                        costumeIds = costumes,
                        equipmentIds = equipments,
                    };

                    Assert.Throws<NotEnoughAvatarLevelException>(() => action.Execute(new ActionContext
                    {
                        PreviousStates = state,
                        Signer = _agent1Address,
                        Random = random,
                    }));
                }
            }
        }

        [Theory]
        [InlineData(ItemSubType.Weapon, GameConfig.MaxEquipmentSlotCount.Weapon)]
        [InlineData(ItemSubType.Armor, GameConfig.MaxEquipmentSlotCount.Armor)]
        [InlineData(ItemSubType.Belt, GameConfig.MaxEquipmentSlotCount.Belt)]
        [InlineData(ItemSubType.Necklace, GameConfig.MaxEquipmentSlotCount.Necklace)]
        [InlineData(ItemSubType.Ring, GameConfig.MaxEquipmentSlotCount.Ring)]
        public void MultipleEquipmentTest(ItemSubType type, int maxCount)
        {
            var previousAvatarState = _initialState.GetAvatarState(_avatar1Address);
            var maxLevel = _tableSheets.CharacterLevelSheet.Max(row => row.Value.Level);
            var expRow = _tableSheets.CharacterLevelSheet[maxLevel];
            var maxLevelExp = expRow.Exp;

            previousAvatarState.level = maxLevel;
            previousAvatarState.exp = maxLevelExp;

            var weaponRows = _tableSheets
                .EquipmentItemSheet
                .Values
                .Where(r => r.ItemSubType == type)
                .Take(maxCount + 1);

            var equipments = new List<Guid>();
            foreach (var row in weaponRows)
            {
                var equipment = ItemFactory.CreateItem(
                        _tableSheets.EquipmentItemSheet[row.Id],
                        new TestRandom())
                    as Equipment;

                equipments.Add(equipment.ItemId);
                previousAvatarState.inventory.AddItem(equipment);
            }

            var state = _initialState.SetState(_avatar1Address, previousAvatarState.Serialize());

            var action = new RankingBattle
            {
                avatarAddress = _avatar1Address,
                enemyAddress = _avatar2Address,
                weeklyArenaAddress = _weeklyArenaAddress,
                costumeIds = new List<Guid>(),
                equipmentIds = equipments,
            };

            Assert.Throws<DuplicateEquipmentException>(() => action.Execute(new ActionContext
            {
                PreviousStates = state,
                Signer = _agent1Address,
                Random = new TestRandom(),
                Rehearsal = false,
            }));
        }

        private static IAccountStateDelta SetState(
            IAccountStateDelta states,
            TableSheets tableSheets,
            AvatarState avatarState,
            bool avatarStateBackward,
            AvatarState enemyAvatarState,
            bool enemyAvatarStateBackward,
            out Guid costumeNonFungibleId)
        {
            var row = tableSheets.CostumeStatSheet.Values.First(r => r.StatType == StatType.ATK);
            var costume = (Costume)ItemFactory.CreateItem(
                tableSheets.ItemSheet[row.CostumeId],
                new TestRandom());
            costumeNonFungibleId = costume.NonFungibleId;
            costume.equipped = true;
            avatarState.inventory.AddItem(costume);
            states = SetState(states, avatarState, avatarStateBackward);

            var row2 = tableSheets.CostumeStatSheet.Values.First(r => r.StatType == StatType.DEF);
            var enemyCostume = (Costume)ItemFactory.CreateItem(
                tableSheets.ItemSheet[row2.CostumeId],
                new TestRandom());
            enemyCostume.equipped = true;
            enemyAvatarState.inventory.AddItem(enemyCostume);
            return SetState(states, enemyAvatarState, enemyAvatarStateBackward);
        }

        private static IAccountStateDelta SetState(
            IAccountStateDelta states,
            AvatarState avatarState,
            bool backward = false)
        {
            var avatarAddress = avatarState.address;
            if (backward)
            {
                return states.SetState(avatarAddress, avatarState.Serialize());
            }

            return states
                .SetState(
                    avatarAddress.Derive(LegacyInventoryKey),
                    avatarState.inventory.Serialize())
                .SetState(
                    avatarAddress.Derive(LegacyWorldInformationKey),
                    avatarState.worldInformation.Serialize())
                .SetState(
                    avatarAddress.Derive(LegacyQuestListKey),
                    avatarState.questList.Serialize())
                .SetState(avatarAddress, avatarState.SerializeV2());
        }
    }
}
