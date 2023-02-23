namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.BlockChain.Policy;
    using Nekoyume.Model;
    using Nekoyume.Model.Elemental;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Mail;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;
    using static Lib9c.SerializeKeys;

    public class MimisbrunnrBattle10Test
    {
        private readonly TableSheets _tableSheets;

        private readonly Address _agentAddress;

        private readonly Address _avatarAddress;

        private readonly IAccountStateDelta _initialState;
        private readonly Dictionary<string, string> _sheets;

        public MimisbrunnrBattle10Test()
        {
            _sheets = TableSheetsImporter.ImportSheets();
            _sheets.Remove(nameof(RuneOptionSheet));
            _tableSheets = new TableSheets(_sheets);

            var privateKey = new PrivateKey();
            _agentAddress = privateKey.PublicKey.ToAddress();
            var agentState = new AgentState(_agentAddress);

            _avatarAddress = _agentAddress.Derive("avatar");
            var gameConfigState = new GameConfigState(_sheets[nameof(GameConfigSheet)]);
            var rankingMapAddress = _avatarAddress.Derive("ranking_map");
            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                gameConfigState,
                rankingMapAddress
            )
            {
                level = 400,
            };
            agentState.avatarAddresses.Add(0, _avatarAddress);

            _initialState = new State()
                .SetState(_agentAddress, agentState.Serialize())
                .SetState(_avatarAddress, avatarState.Serialize())
                .SetState(_avatarAddress.Derive(LegacyInventoryKey), avatarState.inventory.Serialize())
                .SetState(_avatarAddress.Derive(LegacyWorldInformationKey), avatarState.worldInformation.Serialize())
                .SetState(_avatarAddress.Derive(LegacyQuestListKey), avatarState.questList.Serialize())
                .SetState(gameConfigState.address, gameConfigState.Serialize());

            foreach (var (key, value) in _sheets)
            {
                _initialState = _initialState
                    .SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }
        }

        [Theory]
        [InlineData(200, GameConfig.MimisbrunnrWorldId, GameConfig.MimisbrunnrStartStageId, 1, 140, true)]
        [InlineData(400, GameConfig.MimisbrunnrWorldId, GameConfig.MimisbrunnrStartStageId, 1, 100, true)]
        [InlineData(200, GameConfig.MimisbrunnrWorldId, GameConfig.MimisbrunnrStartStageId, 1, 140, false)]
        [InlineData(400, GameConfig.MimisbrunnrWorldId, GameConfig.MimisbrunnrStartStageId, 1, 100, false)]
        [InlineData(400, GameConfig.MimisbrunnrWorldId, GameConfig.MimisbrunnrStartStageId, 2, 100, false)]
        [InlineData(400, GameConfig.MimisbrunnrWorldId, GameConfig.MimisbrunnrStartStageId, 3, 100, false)]
        public void Execute(int avatarLevel, int worldId, int stageId, int playCount, int clearStageId, bool backward)
        {
            Assert.True(_tableSheets.WorldSheet.TryGetValue(worldId, out var worldRow));
            Assert.True(stageId >= worldRow.StageBegin);
            Assert.True(stageId <= worldRow.StageEnd);
            Assert.True(_tableSheets.StageSheet.TryGetValue(stageId, out _));

            var previousAvatarState = _initialState.GetAvatarState(_avatarAddress);
            previousAvatarState.level = avatarLevel;
            previousAvatarState.worldInformation = new WorldInformation(
                0,
                _tableSheets.WorldSheet,
                clearStageId);

            var costumeId = _tableSheets
                .CostumeItemSheet
                .Values
                .First(r => r.ItemSubType == ItemSubType.FullCostume)
                .Id;
            var costume =
                ItemFactory.CreateItem(_tableSheets.ItemSheet[costumeId], new TestRandom());
            previousAvatarState.inventory.AddItem(costume);

            var mimisbrunnrSheet = _tableSheets.MimisbrunnrSheet;
            if (!mimisbrunnrSheet.TryGetValue(stageId, out var mimisbrunnrSheetRow))
            {
                throw new SheetRowNotFoundException("MimisbrunnrSheet", stageId);
            }

            var elementalType = _tableSheets.MimisbrunnrSheet.TryGetValue(stageId, out var mimisbrunnrRow)
                ? mimisbrunnrRow.ElementalTypes.First()
                : ElementalType.Normal;
            var equipments = Doomfist.GetAllParts(_tableSheets, previousAvatarState.level, elementalType);
            foreach (var equipment in equipments)
            {
                previousAvatarState.inventory.AddItem(equipment);
            }

            var result = new CombinationConsumable5.ResultModel
            {
                id = default,
                gold = 0,
                actionPoint = 0,
                recipeId = 1,
                materials = new Dictionary<Material, int>(),
                itemUsable = equipments.First(),
            };
            for (var i = 0; i < 100; i++)
            {
                var mail = new CombinationMail(result, i, default, 0);
                previousAvatarState.Update(mail);
            }

            var state = _initialState;
            if (backward)
            {
                state = _initialState.SetState(_avatarAddress, previousAvatarState.Serialize());
            }
            else
            {
                state = _initialState
                    .SetState(_avatarAddress.Derive(LegacyInventoryKey), previousAvatarState.inventory.Serialize())
                    .SetState(_avatarAddress.Derive(LegacyWorldInformationKey), previousAvatarState.worldInformation.Serialize())
                    .SetState(_avatarAddress.Derive(LegacyQuestListKey), previousAvatarState.questList.Serialize())
                    .SetState(_avatarAddress, previousAvatarState.SerializeV2());
            }

            var action = new MimisbrunnrBattle10
            {
                costumes = new List<Guid> { ((Costume)costume).ItemId },
                equipments = equipments.Select(e => e.NonFungibleId).ToList(),
                foods = new List<Guid>(),
                worldId = worldId,
                stageId = stageId,
                playCount = playCount,
                avatarAddress = _avatarAddress,
            };

            var nextState = action.Execute(new ActionContext
            {
                PreviousStates = state,
                Signer = _agentAddress,
                Random = new TestRandom(),
                Rehearsal = false,
                BlockIndex = ActionObsoleteConfig.V100301ExecutedBlockIndex,
            });

            var nextAvatarState = nextState.GetAvatarStateV2(_avatarAddress);
            Assert.True(nextAvatarState.worldInformation.IsStageCleared(stageId));
            Assert.Equal(30, nextAvatarState.mailBox.Count);
        }

        [Fact]
        public void ExecuteThrowInvalidStageException()
        {
            var stageId = 10000002;
            var worldId = 10001;
            var previousAvatarState = _initialState.GetAvatarState(_avatarAddress);

            previousAvatarState.worldInformation = new WorldInformation(
                0,
                _tableSheets.WorldSheet,
                100
            );
            previousAvatarState.worldInformation.ClearStage(
                2,
                100,
                0,
                _tableSheets.WorldSheet,
                _tableSheets.WorldUnlockSheet);

            var previousState = _initialState.SetState(
                _avatarAddress,
                previousAvatarState.Serialize());

            var costumeRow =
                _tableSheets.CostumeItemSheet.Values.First(x => x.ItemSubType == ItemSubType.FullCostume);
            var costume = ItemFactory.CreateCostume(costumeRow, default);

            var equipmentRow =
                _tableSheets.EquipmentItemSheet.Values.First(x => x.ElementalType == ElementalType.Fire);
            var equipment = ItemFactory.CreateItemUsable(equipmentRow, default, 0);
            previousAvatarState.inventory.AddItem(equipment);

            var mimisbrunnrSheet = _tableSheets.MimisbrunnrSheet;
            if (!mimisbrunnrSheet.TryGetValue(stageId, out var mimisbrunnrSheetRow))
            {
                throw new SheetRowNotFoundException("MimisbrunnrSheet", stageId);
            }

            foreach (var equipmentId in previousAvatarState.inventory.Equipments)
            {
                if (previousAvatarState.inventory.TryGetNonFungibleItem(equipmentId, out ItemUsable itemUsable))
                {
                    var elementalType = ((Equipment)itemUsable).ElementalType;
                    Assert.True(mimisbrunnrSheetRow.ElementalTypes.Exists(x => x == elementalType));
                }
            }

            var action = new MimisbrunnrBattle10
            {
                costumes = new List<Guid> { costume.ItemId },
                equipments = new List<Guid> { equipment.ItemId },
                foods = new List<Guid>(),
                worldId = worldId,
                stageId = stageId,
                playCount = 1,
                avatarAddress = _avatarAddress,
            };

            Assert.Throws<InvalidStageException>(() =>
            {
                action.Execute(new ActionContext
                {
                    PreviousStates = previousState,
                    Signer = _agentAddress,
                    Random = new TestRandom(),
                    Rehearsal = false,
                });
            });
        }

        [Fact]
        public void ExecuteThrowFailedLoadStateException()
        {
            var action = new MimisbrunnrBattle10
            {
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                foods = new List<Guid>(),
                worldId = 10001,
                stageId = 10000002,
                playCount = 1,
                avatarAddress = _avatarAddress,
            };

            Assert.Throws<FailedLoadStateException>(() =>
            {
                action.Execute(new ActionContext
                {
                    PreviousStates = new State(),
                    Signer = _agentAddress,
                });
            });
        }

        [Fact]
        public void ExecuteThrowSheetRowNotFound()
        {
            var action = new MimisbrunnrBattle10
            {
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                foods = new List<Guid>(),
                worldId = 10011,
                stageId = 10000002,
                playCount = 1,
                avatarAddress = _avatarAddress,
            };

            Assert.Throws<SheetRowNotFoundException>(() =>
            {
                action.Execute(new ActionContext
                {
                    PreviousStates = _initialState,
                    Signer = _agentAddress,
                });
            });
        }

        [Fact]
        public void ExecuteThrowSheetRowColumn()
        {
            var action = new MimisbrunnrBattle10
            {
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                foods = new List<Guid>(),
                worldId = 10001,
                stageId = 10000022,
                playCount = 1,
                avatarAddress = _avatarAddress,
            };

            Assert.Throws<SheetRowColumnException>(() =>
            {
                action.Execute(new ActionContext
                {
                    PreviousStates = _initialState,
                    Signer = _agentAddress,
                });
            });
        }

        [Fact]
        public void ExecuteThrowNotEnoughActionPoint()
        {
            var previousAvatarState = _initialState.GetAvatarState(_avatarAddress);
            previousAvatarState.actionPoint = 0;

            previousAvatarState.worldInformation = new WorldInformation(
                0,
                _tableSheets.WorldSheet,
                100);

            previousAvatarState.worldInformation.ClearStage(
                2,
                100,
                0,
                _tableSheets.WorldSheet,
                _tableSheets.WorldUnlockSheet);

            var action = new MimisbrunnrBattle10
            {
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                foods = new List<Guid>(),
                worldId = 10001,
                stageId = 10000001,
                playCount = 1,
                avatarAddress = _avatarAddress,
            };

            var state = _initialState;
            state = state.SetState(_avatarAddress, previousAvatarState.Serialize());
            Assert.Throws<NotEnoughActionPointException>(() =>
            {
                action.Execute(new ActionContext
                {
                    PreviousStates = state,
                    Signer = _agentAddress,
                    Random = new TestRandom(),
                });
            });
        }

        [Theory]
        [InlineData(400, 10001, 10000001, 99)]
        public void ExecuteThrowInvalidWorld(int avatarLevel, int worldId, int stageId, int clearStageId)
        {
            Assert.True(_tableSheets.WorldSheet.TryGetValue(worldId, out var worldRow));
            Assert.True(stageId >= worldRow.StageBegin);
            Assert.True(stageId <= worldRow.StageEnd);
            Assert.True(_tableSheets.StageSheet.TryGetValue(stageId, out _));

            var previousAvatarState = _initialState.GetAvatarState(_avatarAddress);
            previousAvatarState.level = avatarLevel;
            previousAvatarState.worldInformation = new WorldInformation(
                0,
                _tableSheets.WorldSheet,
                clearStageId);

            var costumeId = _tableSheets
                .CostumeItemSheet
                .Values
                .First(r => r.ItemSubType == ItemSubType.FullCostume)
                .Id;
            var costume =
                ItemFactory.CreateItem(_tableSheets.ItemSheet[costumeId], new TestRandom());
            previousAvatarState.inventory.AddItem(costume);

            var mimisbrunnrSheet = _tableSheets.MimisbrunnrSheet;
            if (!mimisbrunnrSheet.TryGetValue(stageId, out var mimisbrunnrSheetRow))
            {
                throw new SheetRowNotFoundException("MimisbrunnrSheet", stageId);
            }

            var equipmentRow =
                _tableSheets.EquipmentItemSheet.Values.First(x => x.ElementalType == ElementalType.Fire);
            var equipment = ItemFactory.CreateItemUsable(equipmentRow, default, 0);
            previousAvatarState.inventory.AddItem(equipment);

            foreach (var equipmentId in previousAvatarState.inventory.Equipments)
            {
                if (previousAvatarState.inventory.TryGetNonFungibleItem(equipmentId, out ItemUsable itemUsable))
                {
                    var elementalType = ((Equipment)itemUsable).ElementalType;
                    Assert.True(mimisbrunnrSheetRow.ElementalTypes.Exists(x => x == elementalType));
                }
            }

            var result = new CombinationConsumable5.ResultModel
            {
                id = default,
                gold = 0,
                actionPoint = 0,
                recipeId = 1,
                materials = new Dictionary<Material, int>(),
                itemUsable = equipment,
            };
            for (var i = 0; i < 100; i++)
            {
                var mail = new CombinationMail(result, i, default, 0);
                previousAvatarState.Update(mail);
            }

            var state = _initialState.SetState(_avatarAddress, previousAvatarState.Serialize());

            var action = new MimisbrunnrBattle10
            {
                costumes = new List<Guid> { ((Costume)costume).ItemId },
                equipments = new List<Guid> { equipment.ItemId },
                foods = new List<Guid>(),
                worldId = worldId,
                stageId = stageId,
                playCount = 1,
                avatarAddress = _avatarAddress,
            };

            Assert.Throws<InvalidWorldException>(() =>
            {
                action.Execute(new ActionContext
                {
                    PreviousStates = state, Signer = _agentAddress, Random = new TestRandom(), Rehearsal = false,
                });
            });
        }

        /// <summary>
        /// void ExecuteThrowFailedAddWorldExceptionWhenDoesNotMimisbrunnr(int).
        /// </summary>
        /// <param name="alreadyClearedStageId">Less than stageId condition to unlock the mimisbrunnr world in `WorldUnlockSheet`.</param>
        [Theory]
        [InlineData(1)]
        [InlineData(99)]
        public void ExecuteThrowFailedAddWorldExceptionWhenDoesNotMimisbrunnr(int alreadyClearedStageId)
        {
            const int worldId = GameConfig.MimisbrunnrWorldId;
            const int stageId = GameConfig.MimisbrunnrStartStageId;
            var worldSheetCsv = _initialState.GetSheetCsv<WorldSheet>();
            worldSheetCsv = worldSheetCsv.Replace($"{worldId},", $"_{worldId},");
            var worldSheet = new WorldSheet();
            worldSheet.Set(worldSheetCsv);
            var avatarState = _initialState.GetAvatarState(_avatarAddress);
            avatarState.worldInformation = new WorldInformation(0, worldSheet, alreadyClearedStageId);
            var nextState = _initialState.SetState(_avatarAddress, avatarState.Serialize());

            var action = new MimisbrunnrBattle10
            {
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                foods = new List<Guid>(),
                worldId = worldId,
                stageId = stageId,
                avatarAddress = _avatarAddress,
            };

            Assert.Throws<FailedAddWorldException>(() =>
            {
                action.Execute(new ActionContext
                {
                    PreviousStates = nextState,
                    Signer = _agentAddress,
                });
            });
        }

        [Fact]
        public void ExecuteEquippableItemValidation()
        {
            var avatarState = _initialState.GetAvatarState(_avatarAddress);
            avatarState.worldInformation = new WorldInformation(
                0,
                _tableSheets.WorldSheet,
                140);

            var costumeId = _tableSheets
                .CostumeItemSheet
                .OrderedList
                .First(r => r.ItemSubType == ItemSubType.FullCostume)
                .Id;
            var costume =
                ItemFactory.CreateItem(_tableSheets.ItemSheet[costumeId], new TestRandom());
            avatarState.inventory.AddItem(costume);

            var equipmentRow =
                _tableSheets.EquipmentItemSheet.OrderedList.First(x => x.ElementalType == ElementalType.Fire);
            var equipment = ItemFactory.CreateItemUsable(equipmentRow, default, 0);
            avatarState.inventory.AddItem(equipment);
            var nextState = _initialState.SetState(_avatarAddress, avatarState.Serialize());

            var action = new MimisbrunnrBattle10
            {
                costumes = new List<Guid> { ((Costume)costume).ItemId },
                equipments = new List<Guid> { equipment.ItemId },
                foods = new List<Guid>(),
                worldId = GameConfig.MimisbrunnrWorldId,
                stageId = GameConfig.MimisbrunnrStartStageId,
                playCount = 1,
                avatarAddress = _avatarAddress,
            };

            action.Execute(new ActionContext
            {
                PreviousStates = nextState,
                Signer = _agentAddress,
                Rehearsal = false,
                Random = new TestRandom(),
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
        public void ExecuteThrowHighLevelItemRequirementException(int avatarLevel)
        {
            var state = _initialState;
            var avatarState = state.GetAvatarState(_avatarAddress);
            avatarState.actionPoint = 99999999;
            avatarState.level = avatarLevel;

            avatarState.worldInformation = new WorldInformation(
                0,
                _tableSheets.WorldSheet,
                100
            );

            avatarState.worldInformation.ClearStage(
                2,
                100,
                0,
                _tableSheets.WorldSheet,
                _tableSheets.WorldUnlockSheet);

            state = state.SetState(
                avatarState.address.Derive(LegacyWorldInformationKey),
                avatarState.worldInformation.Serialize());

            foreach (var requirementRow in _tableSheets.ItemRequirementSheet)
            {
                if (avatarState.level >= requirementRow.Level)
                {
                    continue;
                }

                var costumes = new List<Guid>();
                var equipments = new List<Guid>();
                var random = new TestRandom(DateTimeOffset.Now.Millisecond);
                if (_tableSheets.EquipmentItemSheet.TryGetValue(requirementRow.ItemId, out var row))
                {
                    var equipment = ItemFactory.CreateItem(row, random);
                    avatarState.inventory.AddItem(equipment);

                    if (equipment.ElementalType != ElementalType.Fire)
                    {
                        continue;
                    }
                }
                else if (_tableSheets.CostumeItemSheet.TryGetValue(requirementRow.ItemId, out var row2))
                {
                    var costume = ItemFactory.CreateItem(row2, random);
                    avatarState.inventory.AddItem(costume);
                    costumes.Add(((INonFungibleItem)costume).NonFungibleId);
                }

                if (!equipments.Any())
                {
                    continue;
                }

                state = state.SetState(avatarState.address, avatarState.SerializeV2())
                    .SetState(
                        avatarState.address.Derive(LegacyInventoryKey),
                        avatarState.inventory.Serialize());

                var action = new MimisbrunnrBattle10
                {
                    costumes = costumes,
                    equipments = equipments,
                    foods = new List<Guid>(),
                    worldId = GameConfig.MimisbrunnrWorldId,
                    stageId = GameConfig.MimisbrunnrStartStageId,
                    playCount = 1,
                    avatarAddress = avatarState.address,
                };

                Assert.Throws<NotEnoughAvatarLevelException>(() => action.Execute(new ActionContext
                {
                    PreviousStates = state,
                    Signer = avatarState.agentAddress,
                    Random = random,
                }));
            }
        }

        [Fact]
        public void Execute_v100291()
        {
            var avatarLevel = 200;
            var worldId = GameConfig.MimisbrunnrWorldId;
            var stageId = GameConfig.MimisbrunnrStartStageId;
            var playCount = 1;
            var clearStageId = 140;
            var backward = false;
            Assert.True(_tableSheets.WorldSheet.TryGetValue(worldId, out var worldRow));
            Assert.True(stageId >= worldRow.StageBegin);
            Assert.True(stageId <= worldRow.StageEnd);
            Assert.True(_tableSheets.StageSheet.TryGetValue(stageId, out _));

            var previousAvatarState = _initialState.GetAvatarState(_avatarAddress);
            previousAvatarState.level = avatarLevel;
            previousAvatarState.worldInformation = new WorldInformation(
                0,
                _tableSheets.WorldSheet,
                clearStageId);

            var costumeId = _tableSheets
                .CostumeItemSheet
                .Values
                .First(r => r.ItemSubType == ItemSubType.FullCostume)
                .Id;
            var costume =
                ItemFactory.CreateItem(_tableSheets.ItemSheet[costumeId], new TestRandom());
            previousAvatarState.inventory.AddItem(costume);

            var mimisbrunnrSheet = _tableSheets.MimisbrunnrSheet;
            if (!mimisbrunnrSheet.TryGetValue(stageId, out var mimisbrunnrSheetRow))
            {
                throw new SheetRowNotFoundException("MimisbrunnrSheet", stageId);
            }

            var elementalType = _tableSheets.MimisbrunnrSheet.TryGetValue(stageId, out var mimisbrunnrRow)
                ? mimisbrunnrRow.ElementalTypes.First()
                : ElementalType.Normal;
            var equipments = Doomfist.GetAllParts(_tableSheets, previousAvatarState.level, elementalType);
            foreach (var equipment in equipments)
            {
                previousAvatarState.inventory.AddItem(equipment);
            }

            var result = new CombinationConsumable5.ResultModel
            {
                id = default,
                gold = 0,
                actionPoint = 0,
                recipeId = 1,
                materials = new Dictionary<Material, int>(),
                itemUsable = equipments.First(),
            };
            for (var i = 0; i < 100; i++)
            {
                var mail = new CombinationMail(result, i, default, 0);
                previousAvatarState.Update(mail);
            }

            var state = _initialState;
            if (backward)
            {
                state = _initialState.SetState(_avatarAddress, previousAvatarState.Serialize());
            }
            else
            {
                state = _initialState
                    .SetState(_avatarAddress.Derive(LegacyInventoryKey), previousAvatarState.inventory.Serialize())
                    .SetState(_avatarAddress.Derive(LegacyWorldInformationKey), previousAvatarState.worldInformation.Serialize())
                    .SetState(_avatarAddress.Derive(LegacyQuestListKey), previousAvatarState.questList.Serialize())
                    .SetState(_avatarAddress, previousAvatarState.SerializeV2());
            }

            var keys = new List<string>
            {
                nameof(SkillActionBuffSheet),
                nameof(ActionBuffSheet),
                nameof(StatBuffSheet),
            };
            foreach (var (key, value) in _sheets)
            {
                if (keys.Contains(key))
                {
                    state = state.SetState(Addresses.TableSheet.Derive(key), null!);
                }
            }

            var action = new MimisbrunnrBattle10
            {
                costumes = new List<Guid> { ((Costume)costume).ItemId },
                equipments = equipments.Select(e => e.NonFungibleId).ToList(),
                foods = new List<Guid>(),
                worldId = worldId,
                stageId = stageId,
                playCount = playCount,
                avatarAddress = _avatarAddress,
            };

            var nextState = action.Execute(new ActionContext
            {
                PreviousStates = state,
                Signer = _agentAddress,
                Random = new TestRandom(),
                Rehearsal = false,
                BlockIndex = 1,
            });

            var nextAvatarState = nextState.GetAvatarStateV2(_avatarAddress);
            Assert.True(nextAvatarState.worldInformation.IsStageCleared(stageId));
            Assert.Equal(30, nextAvatarState.mailBox.Count);
        }

        [Theory]
        [InlineData(true, 0, 100)]
        [InlineData(true, 1, 100)]
        [InlineData(true, 2, 100)]
        [InlineData(true, 3, 100)]
        [InlineData(true, 4, 100)]
        [InlineData(true, 5, 100)]
        [InlineData(true, 6, 100)]
        [InlineData(true, 7, 100)]
        [InlineData(true, 8, 100)]
        [InlineData(true, 9, 100)]
        [InlineData(false, 0, 100)]
        [InlineData(false, 1, 100)]
        [InlineData(false, 2, 100)]
        [InlineData(false, 3, 100)]
        [InlineData(false, 4, 100)]
        public void CheckRewardItems(bool backward, int stageIndex, int playCount)
        {
            const int worldId = GameConfig.MimisbrunnrWorldId;
            var stageId = GameConfig.MimisbrunnrStartStageId + stageIndex;

            Assert.True(_tableSheets.WorldSheet.TryGetValue(worldId, out var worldRow));
            Assert.True(stageId >= worldRow.StageBegin);
            Assert.True(stageId <= worldRow.StageEnd);
            Assert.True(_tableSheets.StageSheet.TryGetValue(stageId, out var stageRow));

            var previousAvatarState = _initialState.GetAvatarStateV2(_avatarAddress);
            previousAvatarState.actionPoint = 999999;
            previousAvatarState.level = 400;
            previousAvatarState.worldInformation = new WorldInformation(
                0,
                _tableSheets.WorldSheet,
                stageId);

            var costumes = new List<Guid>();
            var random = new TestRandom();
            var costumeId = _tableSheets
                .CostumeItemSheet
                .Values
                .First(r => r.ItemSubType == ItemSubType.FullCostume)
                .Id;

            var costume = (Costume)ItemFactory.CreateItem(
                _tableSheets.ItemSheet[costumeId], random);
            previousAvatarState.inventory.AddItem(costume);
            costumes.Add(costume.ItemId);

            var elementalType = _tableSheets.MimisbrunnrSheet.TryGetValue(stageId, out var mimisbrunnrRow)
                ? mimisbrunnrRow.ElementalTypes.First()
                : ElementalType.Normal;
            var equipments = Doomfist.GetAllParts(_tableSheets, previousAvatarState.level, elementalType);
            foreach (var equipment in equipments)
            {
                previousAvatarState.inventory.AddItem(equipment);
            }

            var result = new CombinationConsumable5.ResultModel
            {
                id = default,
                gold = 0,
                actionPoint = 0,
                recipeId = 1,
                materials = new Dictionary<Material, int>(),
                itemUsable = equipments.First(),
            };
            for (var i = 0; i < 100; i++)
            {
                var mail = new CombinationMail(result, i, default, 0);
                previousAvatarState.Update(mail);
            }

            IAccountStateDelta state;
            if (backward)
            {
                state = _initialState.SetState(_avatarAddress, previousAvatarState.Serialize());
            }
            else
            {
                state = _initialState
                    .SetState(_avatarAddress, previousAvatarState.SerializeV2())
                    .SetState(_avatarAddress.Derive(LegacyInventoryKey), previousAvatarState.inventory.Serialize())
                    .SetState(_avatarAddress.Derive(LegacyWorldInformationKey), previousAvatarState.worldInformation.Serialize())
                    .SetState(_avatarAddress.Derive(LegacyQuestListKey), previousAvatarState.questList.Serialize());
            }

            var action = new MimisbrunnrBattle10
            {
                costumes = costumes,
                equipments = equipments.Select(e => e.NonFungibleId).ToList(),
                foods = new List<Guid>(),
                worldId = worldId,
                stageId = stageId,
                playCount = playCount,
                avatarAddress = _avatarAddress,
            };

            var nextState = action.Execute(new ActionContext
            {
                PreviousStates = state,
                Signer = _agentAddress,
                Random = new TestRandom(),
                Rehearsal = false,
                BlockIndex = 1,
            });

            var nextAvatarState = nextState.GetAvatarStateV2(_avatarAddress);
            Assert.True(nextAvatarState.worldInformation.IsStageCleared(stageId));
            Assert.Equal(30, nextAvatarState.mailBox.Count);

            var rewardItem = nextAvatarState.inventory.Items
                .Where(x =>
                    x.item.ItemSubType != ItemSubType.FoodMaterial &&
                    x.item is IFungibleItem &&
                    x.item.Id != 400000 && x.item.Id != 500000)
                .ToList();
            Assert.Equal(stageRow.Rewards.Count, rewardItem.Count);

            var min = stageRow.Rewards.OrderBy(x => x.Min).First().Min;
            var max = stageRow.Rewards.OrderBy(x => x.Max).First().Max;
            var totalMin = min * playCount * stageRow.DropItemMin;
            var totalMax = max * playCount * stageRow.DropItemMax;
            var totalCount = rewardItem.Sum(x => x.count);
            Assert.InRange(totalCount, totalMin, totalMax);
        }
    }
}
