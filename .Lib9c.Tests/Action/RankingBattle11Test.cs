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
    using static Lib9c.SerializeKeys;

    public class RankingBattle11Test
    {
        private const int ArenaIndex = RankingBattle11.UpdateTargetWeeklyArenaIndex - 1;
        private readonly TableSheets _tableSheets;
        private readonly Address _agent1Address;
        private readonly Address _avatar1Address;
        private readonly Address _avatar2Address;
        private readonly Address _weeklyArenaAddress;
        private readonly IAccountStateDelta _initialState;
        private IValue _arenaSheetState;

        public RankingBattle11Test(ITestOutputHelper outputHelper)
        {
            _initialState = new State();

            var keys = new List<string>
            {
                nameof(SkillActionBuffSheet),
                nameof(ActionBuffSheet),
                nameof(StatBuffSheet),
            };
            var sheets = TableSheetsImporter.ImportSheets();
            foreach (var (key, value) in sheets)
            {
                if (!keys.Contains(key))
                {
                    _initialState = _initialState.SetState(
                        Addresses.TableSheet.Derive(key),
                        value.Serialize());
                }
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

            var weeklyArenaState = new WeeklyArenaState(ArenaIndex);
            weeklyArenaState.SetV2(avatar1State, _tableSheets.CharacterSheet, _tableSheets.CostumeStatSheet);
            weeklyArenaState[_avatar1Address].Activate();
            weeklyArenaState.SetV2(avatar2State, _tableSheets.CharacterSheet, _tableSheets.CostumeStatSheet);
            weeklyArenaState[_avatar2Address].Activate();
            _weeklyArenaAddress = weeklyArenaState.address;

            _initialState = _initialState
                .SetState(_agent1Address, agent1State.Serialize())
                .SetState(_avatar1Address, avatar1State.Serialize())
                .SetState(agent2Address, agent2State.Serialize())
                .SetState(_avatar2Address, avatar2State.Serialize())
                .SetState(Addresses.GameConfig, new GameConfigState(sheets[nameof(GameConfigSheet)]).Serialize())
                .SetState(_weeklyArenaAddress, weeklyArenaState.Serialize());

            var arenaSheetAddress = Addresses.GetSheetAddress<ArenaSheet>();
            _arenaSheetState = _initialState.GetState(arenaSheetAddress);
            _initialState = _initialState.SetState(arenaSheetAddress, null);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();
        }

        public static (AgentState AgentState, AvatarState AvatarState) GetAgentStateWithAvatarState(
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
            var previousWeeklyState = _initialState.GetWeeklyArenaState(ArenaIndex);
            var previousAvatar1State = _initialState.GetAvatarState(_avatar1Address);
            previousAvatar1State.level = 10;
            var prevScore = previousWeeklyState[_avatar1Address].Score;

            var previousState = _initialState.SetState(
                _avatar1Address,
                previousAvatar1State.Serialize());

            var itemIds = _tableSheets.WeeklyArenaRewardSheet.Values
                .Select(r => r.Reward.ItemId)
                .ToList();

            Assert.All(itemIds, id => Assert.False(previousAvatar1State.inventory.HasItem(id)));

            var row = _tableSheets.CostumeStatSheet.Values.First(r => r.StatType == StatType.ATK);
            var costume = (Costume)ItemFactory.CreateItem(
                _tableSheets.ItemSheet[row.CostumeId], new TestRandom());
            costume.equipped = true;
            previousAvatar1State.inventory.AddItem(costume);

            var row2 = _tableSheets.CostumeStatSheet.Values.First(r => r.StatType == StatType.DEF);
            var enemyCostume = (Costume)ItemFactory.CreateItem(
                _tableSheets.ItemSheet[row2.CostumeId], new TestRandom());
            enemyCostume.equipped = true;
            var enemyAvatarState = _initialState.GetAvatarState(_avatar2Address);
            enemyAvatarState.inventory.AddItem(enemyCostume);

            Address worldInformationAddress = _avatar1Address.Derive(LegacyWorldInformationKey);
            var weeklyArenaAddress =
                WeeklyArenaState.DeriveAddress(RankingBattle11.UpdateTargetWeeklyArenaIndex);
            previousState = previousState
                .SetState(
                    _avatar1Address.Derive(LegacyInventoryKey),
                    previousAvatar1State.inventory.Serialize())
                .SetState(
                    worldInformationAddress,
                    previousAvatar1State.worldInformation.Serialize())
                .SetState(
                    _avatar1Address.Derive(LegacyQuestListKey),
                    previousAvatar1State.questList.Serialize())
                .SetState(_avatar1Address, previousAvatar1State.SerializeV2())
                .SetState(
                    _avatar2Address.Derive(LegacyInventoryKey),
                    enemyAvatarState.inventory.Serialize())
                .SetState(
                    _avatar2Address.Derive(LegacyWorldInformationKey),
                    enemyAvatarState.worldInformation.Serialize())
                .SetState(
                    _avatar2Address.Derive(LegacyQuestListKey),
                    enemyAvatarState.questList.Serialize())
                .SetState(_avatar2Address, enemyAvatarState.SerializeV2())
                .SetState(
                    weeklyArenaAddress,
                    new WeeklyArenaState(RankingBattle11.UpdateTargetWeeklyArenaIndex).Serialize());

            var action = new RankingBattle11
            {
                avatarAddress = _avatar1Address,
                enemyAddress = _avatar2Address,
                weeklyArenaAddress = weeklyArenaAddress,
                costumeIds = new List<Guid> { costume.ItemId },
                equipmentIds = new List<Guid>(),
            };

            var nextState = action.Execute(new ActionContext
            {
                PreviousStates = previousState,
                Signer = _agent1Address,
                Random = new TestRandom(),
                Rehearsal = false,
                BlockIndex = RankingBattle11.UpdateTargetBlockIndex,
            });

            Assert.True(nextState.TryGetState(weeklyArenaAddress.Derive(_avatar1Address.ToByteArray()), out Dictionary rawArenaInfo));
            Assert.True(nextState.TryGetState(weeklyArenaAddress.Derive(_avatar2Address.ToByteArray()), out Dictionary rawEnemyInfo));
            Assert.True(nextState.TryGetState(weeklyArenaAddress.Derive("address_list"), out List rawAddressList));
            var nextWeekly = nextState.GetWeeklyArenaState(weeklyArenaAddress);
            Assert.Empty(nextWeekly.Map);

            var nextAvatar1State = nextState.GetAvatarStateV2(_avatar1Address);
            var nextArenaInfo = new ArenaInfo(rawArenaInfo);
            var addressList = rawAddressList.ToList(StateExtensions.ToAddress);

            Assert.Contains(_avatar1Address, addressList);
            Assert.Contains(_avatar2Address, addressList);
            Assert.Contains(nextAvatar1State.inventory.Materials, i => itemIds.Contains(i.Id));
            Assert.NotNull(action.ArenaInfo);
            Assert.NotNull(action.EnemyArenaInfo);
            Assert.True(nextArenaInfo.Score > prevScore);

            // Check simulation result equal.
            var player = new Player(
                previousAvatar1State,
                _tableSheets.CharacterSheet,
                _tableSheets.CharacterLevelSheet,
                _tableSheets.EquipmentItemSetEffectSheet);
            var simulator = new RankingSimulatorV1(
                new TestRandom(),
                player,
                action.EnemyPlayerDigest,
                new List<Guid>(),
                _tableSheets.GetRankingSimulatorSheetsV1(),
                RankingBattle11.StageId,
                action.ArenaInfo,
                action.EnemyArenaInfo,
                _tableSheets.CostumeStatSheet);
            simulator.Simulate();

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
            var previousWeeklyState = _initialState.GetWeeklyArenaState(ArenaIndex);
            var previousAvatar1State = _initialState.GetAvatarState(_avatar1Address);
            previousAvatar1State.level = 10;
            var prevScore = previousWeeklyState[_avatar1Address].Score;
            if (isNew)
            {
                previousWeeklyState.Remove(_avatar1Address);
            }

            var previousState = _initialState.SetState(
                _avatar1Address,
                previousAvatar1State.Serialize());

            var itemIds = _tableSheets.WeeklyArenaRewardSheet.Values
                .Select(r => r.Reward.ItemId)
                .ToList();

            Assert.All(itemIds, id => Assert.False(previousAvatar1State.inventory.HasItem(id)));

            var row = _tableSheets.CostumeStatSheet.Values.First(r => r.StatType == StatType.ATK);
            var costume = (Costume)ItemFactory.CreateItem(
                _tableSheets.ItemSheet[row.CostumeId], new TestRandom());
            costume.equipped = true;
            previousAvatar1State.inventory.AddItem(costume);

            var row2 = _tableSheets.CostumeStatSheet.Values.First(r => r.StatType == StatType.DEF);
            var enemyCostume = (Costume)ItemFactory.CreateItem(
                _tableSheets.ItemSheet[row2.CostumeId], new TestRandom());
            enemyCostume.equipped = true;
            var enemyAvatarState = _initialState.GetAvatarState(_avatar2Address);
            enemyAvatarState.inventory.AddItem(enemyCostume);

            Address worldInformationAddress = _avatar1Address.Derive(LegacyWorldInformationKey);
            if (avatarBackward)
            {
                previousState =
                    previousState.SetState(_avatar1Address, previousAvatar1State.Serialize());
            }
            else
            {
                previousState = previousState
                    .SetState(
                        _avatar1Address.Derive(LegacyInventoryKey),
                        previousAvatar1State.inventory.Serialize())
                    .SetState(
                        worldInformationAddress,
                        previousAvatar1State.worldInformation.Serialize())
                    .SetState(
                        _avatar1Address.Derive(LegacyQuestListKey),
                        previousAvatar1State.questList.Serialize())
                    .SetState(_avatar1Address, previousAvatar1State.SerializeV2());
            }

            if (enemyBackward)
            {
                previousState =
                    previousState.SetState(_avatar2Address, enemyAvatarState.Serialize());
            }
            else
            {
                previousState = previousState
                    .SetState(
                        _avatar2Address.Derive(LegacyInventoryKey),
                        enemyAvatarState.inventory.Serialize())
                    .SetState(
                        _avatar2Address.Derive(LegacyWorldInformationKey),
                        enemyAvatarState.worldInformation.Serialize())
                    .SetState(
                        _avatar2Address.Derive(LegacyQuestListKey),
                        enemyAvatarState.questList.Serialize())
                    .SetState(_avatar2Address, enemyAvatarState.SerializeV2());
            }

            var action = new RankingBattle11
            {
                avatarAddress = _avatar1Address,
                enemyAddress = _avatar2Address,
                weeklyArenaAddress = _weeklyArenaAddress,
                costumeIds = new List<Guid> { costume.ItemId },
                equipmentIds = new List<Guid>(),
            };

            var nextState = action.Execute(new ActionContext
            {
                PreviousStates = previousState,
                Signer = _agent1Address,
                Random = new TestRandom(),
                Rehearsal = false,
                BlockIndex = RankingBattle11.UpdateTargetBlockIndex - 1,
            });

            var nextAvatar1State = nextState.GetAvatarStateV2(_avatar1Address);
            var nextWeeklyState = nextState.GetWeeklyArenaState(ArenaIndex);
            var nextArenaInfo = nextWeeklyState[_avatar1Address];

            Assert.Contains(nextAvatar1State.inventory.Materials, i => itemIds.Contains(i.Id));
            Assert.NotNull(action.ArenaInfo);
            Assert.NotNull(action.EnemyArenaInfo);
            Assert.True(nextArenaInfo.Score > prevScore);

            // Check simulation result equal.
            var player = new Player(
                previousAvatar1State,
                _tableSheets.CharacterSheet,
                _tableSheets.CharacterLevelSheet,
                _tableSheets.EquipmentItemSetEffectSheet);
            var simulator = new RankingSimulatorV1(
                new TestRandom(),
                player,
                action.EnemyPlayerDigest,
                new List<Guid>(),
                _tableSheets.GetRankingSimulatorSheetsV1(),
                RankingBattle11.StageId,
                action.ArenaInfo,
                action.EnemyArenaInfo,
                _tableSheets.CostumeStatSheet);
            simulator.Simulate();

            Assert.Equal(nextArenaInfo.Score, simulator.Log.score);
            Assert.Equal(previousAvatar1State.SerializeV2(), nextAvatar1State.SerializeV2());
            Assert.Equal(previousAvatar1State.worldInformation.Serialize(), nextAvatar1State.worldInformation.Serialize());
        }

        [Fact]
        public void ExecuteThrowInvalidAddressException()
        {
            var action = new RankingBattle11
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

            var action = new RankingBattle11
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

            var action = new RankingBattle11
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

            var action = new RankingBattle11
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
        public void ExecuteThrowWeeklyArenaStateNotContainsAvatarAddressException()
        {
            var targetAddress = _avatar2Address;

            var previousWeeklyArenaState = _initialState.GetWeeklyArenaState(_weeklyArenaAddress);
            previousWeeklyArenaState.Remove(targetAddress);

            var previousState = _initialState.SetState(
                _weeklyArenaAddress,
                previousWeeklyArenaState.Serialize());

            var action = new RankingBattle11
            {
                avatarAddress = _avatar1Address,
                enemyAddress = _avatar2Address,
                weeklyArenaAddress = _weeklyArenaAddress,
                costumeIds = new List<Guid>(),
                equipmentIds = new List<Guid>(),
            };

            Assert.Throws<WeeklyArenaStateNotContainsAvatarAddressException>(() =>
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
            var previousAvatarState = _initialState.GetAvatarState(_avatar1Address);
            var previousWeeklyArenaState = _initialState.GetWeeklyArenaState(_weeklyArenaAddress);
            while (true)
            {
                var arenaInfo = previousWeeklyArenaState.GetArenaInfo(_avatar1Address);
                arenaInfo.UpdateV3(previousAvatarState, arenaInfo, BattleLog.Result.Lose);
                if (arenaInfo.DailyChallengeCount == 0)
                {
                    break;
                }
            }

            var previousState = _initialState.SetState(
                _weeklyArenaAddress,
                previousWeeklyArenaState.Serialize());

            var action = new RankingBattle11
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

                    var action = new RankingBattle11
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
                        BlockIndex = 3806324 + 1, // Ranking Battle Action throws NotEnoughAvatarLevelException when BlockIndex is higher than 3806324.
                    }));
                }
            }
        }

        [Fact]
        public void Rehearsal()
        {
            var action = new RankingBattle11
            {
                avatarAddress = _avatar1Address,
                enemyAddress = _avatar2Address,
                weeklyArenaAddress = _weeklyArenaAddress,
                costumeIds = new List<Guid>(),
                equipmentIds = new List<Guid>(),
            };

            var updatedAddresses = new List<Address>
            {
                _avatar1Address,
                _weeklyArenaAddress,
                _avatar1Address.Derive(LegacyInventoryKey),
                _avatar1Address.Derive(LegacyWorldInformationKey),
                _avatar1Address.Derive(LegacyQuestListKey),
            };

            var state = new State();

            var nextState = action.Execute(new ActionContext
            {
                PreviousStates = state,
                Signer = _agent1Address,
                BlockIndex = 0,
                Rehearsal = true,
            });

            Assert.Equal(updatedAddresses.ToImmutableHashSet(), nextState.UpdatedAddresses);
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

            var action = new RankingBattle11
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

        [Fact]
        public void Execute_ActionObsoletedException()
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

            var arenaSheetAddress = Addresses.GetSheetAddress<ArenaSheet>();
            previousState = previousState.SetState(arenaSheetAddress, _arenaSheetState);

            var action = new RankingBattle11
            {
                avatarAddress = _avatar1Address,
                enemyAddress = _avatar2Address,
                weeklyArenaAddress = _weeklyArenaAddress,
                costumeIds = new List<Guid>(),
                equipmentIds = new List<Guid>(),
            };

            Assert.Throws<ActionObsoletedException>(() =>
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
    }
}
