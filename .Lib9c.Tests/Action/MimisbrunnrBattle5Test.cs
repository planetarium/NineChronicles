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
    using Nekoyume.Model;
    using Nekoyume.Model.BattleStatus;
    using Nekoyume.Model.Elemental;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Mail;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;
    using static SerializeKeys;

    public class MimisbrunnrBattle5Test
    {
        private readonly TableSheets _tableSheets;

        private readonly Address _agentAddress;

        private readonly Address _avatarAddress;

        private readonly Address _rankingMapAddress;

        private readonly IAccountStateDelta _initialState;

        public MimisbrunnrBattle5Test()
        {
            var sheets = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(sheets);

            var privateKey = new PrivateKey();
            _agentAddress = privateKey.PublicKey.ToAddress();
            var agentState = new AgentState(_agentAddress);

            _avatarAddress = _agentAddress.Derive("avatar");
            _rankingMapAddress = _avatarAddress.Derive("ranking_map");
            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(sheets[nameof(GameConfigSheet)]),
                _rankingMapAddress
            )
            {
                level = 400,
            };
            agentState.avatarAddresses.Add(0, _avatarAddress);

            _initialState = new State()
                .SetState(_agentAddress, agentState.Serialize())
                .SetState(_avatarAddress, avatarState.Serialize())
                .SetState(_rankingMapAddress, new RankingMapState(_rankingMapAddress).Serialize());

            foreach (var (key, value) in sheets)
            {
                _initialState = _initialState
                    .SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }
        }

        [Theory]
        [InlineData(200, GameConfig.MimisbrunnrWorldId, GameConfig.MimisbrunnrStartStageId, 140, true)]
        [InlineData(400, GameConfig.MimisbrunnrWorldId, GameConfig.MimisbrunnrStartStageId, 100, true)]
        [InlineData(200, GameConfig.MimisbrunnrWorldId, GameConfig.MimisbrunnrStartStageId, 140, false)]
        [InlineData(400, GameConfig.MimisbrunnrWorldId, GameConfig.MimisbrunnrStartStageId, 100, false)]
        public void Execute(int avatarLevel, int worldId, int stageId, int clearStageId, bool backward)
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

            var result = new CombinationConsumable5.ResultModel()
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

            var action = new MimisbrunnrBattle5()
            {
                costumes = new List<Guid> { ((Costume)costume).ItemId },
                equipments = new List<Guid>() { equipment.ItemId },
                foods = new List<Guid>(),
                worldId = worldId,
                stageId = stageId,
                avatarAddress = _avatarAddress,
                rankingMapAddress = _rankingMapAddress,
            };

            var nextState = action.Execute(new ActionContext()
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

            var value = nextState.GetState(_rankingMapAddress);
            if (value != null)
            {
                var rankingMapState = new RankingMapState((Dictionary)value);
                var info = rankingMapState.GetRankingInfos(null).First();

                Assert.Equal(info.AgentAddress, _agentAddress);
                Assert.Equal(info.AvatarAddress, _avatarAddress);
            }
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

            var action = new MimisbrunnrBattle5()
            {
                costumes = new List<Guid> { costume.ItemId },
                equipments = new List<Guid>() { equipment.ItemId },
                foods = new List<Guid>(),
                worldId = worldId,
                stageId = stageId,
                avatarAddress = _avatarAddress,
                rankingMapAddress = _rankingMapAddress,
            };

            Assert.Throws<InvalidStageException>(() =>
            {
                action.Execute(new ActionContext()
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
            var action = new MimisbrunnrBattle5()
            {
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                foods = new List<Guid>(),
                worldId = 10001,
                stageId = 10000002,
                avatarAddress = _avatarAddress,
                rankingMapAddress = _rankingMapAddress,
            };

            Assert.Throws<FailedLoadStateException>(() =>
            {
                action.Execute(new ActionContext()
                {
                    PreviousStates = new State(),
                    Signer = _agentAddress,
                });
            });
        }

        [Fact]
        public void ExecuteThrowInvalidRankingMapAddress()
        {
            var action = new MimisbrunnrBattle5()
            {
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                foods = new List<Guid>(),
                worldId = 10001,
                stageId = 10000002,
                avatarAddress = _avatarAddress,
                rankingMapAddress = default,
            };

            Assert.Throws<InvalidAddressException>(() =>
            {
                action.Execute(new ActionContext()
                {
                    PreviousStates = _initialState,
                    Signer = _agentAddress,
                });
            });
        }

        [Fact]
        public void ExecuteThrowSheetRowNotFound()
        {
            var action = new MimisbrunnrBattle5()
            {
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                foods = new List<Guid>(),
                worldId = 10011,
                stageId = 10000002,
                avatarAddress = _avatarAddress,
                rankingMapAddress = _rankingMapAddress,
            };

            Assert.Throws<SheetRowNotFoundException>(() =>
            {
                action.Execute(new ActionContext()
                {
                    PreviousStates = _initialState,
                    Signer = _agentAddress,
                });
            });
        }

        [Fact]
        public void ExecuteThrowSheetRowColumn()
        {
            var action = new MimisbrunnrBattle5()
            {
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                foods = new List<Guid>(),
                worldId = 10001,
                stageId = 10000022,
                avatarAddress = _avatarAddress,
                rankingMapAddress = _rankingMapAddress,
            };

            Assert.Throws<SheetRowColumnException>(() =>
            {
                action.Execute(new ActionContext()
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

            var action = new MimisbrunnrBattle5()
            {
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                foods = new List<Guid>(),
                worldId = 10001,
                stageId = 10000001,
                avatarAddress = _avatarAddress,
                rankingMapAddress = _rankingMapAddress,
            };

            var state = _initialState;
            state = state.SetState(_avatarAddress, previousAvatarState.Serialize());
            Assert.Throws<NotEnoughActionPointException>(() =>
            {
                action.Execute(new ActionContext()
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

            var result = new CombinationConsumable5.ResultModel()
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

            var action = new MimisbrunnrBattle5()
            {
                costumes = new List<Guid> { ((Costume)costume).ItemId },
                equipments = new List<Guid>() { equipment.ItemId },
                foods = new List<Guid>(),
                worldId = worldId,
                stageId = stageId,
                avatarAddress = _avatarAddress,
                rankingMapAddress = _rankingMapAddress,
            };

            Assert.Throws<InvalidWorldException>(() =>
            {
                action.Execute(new ActionContext()
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

            var action = new MimisbrunnrBattle5
            {
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                foods = new List<Guid>(),
                worldId = worldId,
                stageId = stageId,
                avatarAddress = _avatarAddress,
                rankingMapAddress = _rankingMapAddress,
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

            var action = new MimisbrunnrBattle5()
            {
                costumes = new List<Guid> { ((Costume)costume).ItemId },
                equipments = new List<Guid>() { equipment.ItemId },
                foods = new List<Guid>(),
                worldId = GameConfig.MimisbrunnrWorldId,
                stageId = GameConfig.MimisbrunnrStartStageId,
                avatarAddress = _avatarAddress,
                rankingMapAddress = _rankingMapAddress,
            };

            action.Execute(new ActionContext
            {
                PreviousStates = nextState,
                Signer = _agentAddress,
                Rehearsal = false,
                Random = new TestRandom(),
            });
        }

        [Fact]
        public void Rehearsal()
        {
            var action = new MimisbrunnrBattle5()
            {
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                foods = new List<Guid>(),
                worldId = 1,
                stageId = 1,
                avatarAddress = _avatarAddress,
                rankingMapAddress = _rankingMapAddress,
            };

            var updatedAddresses = new List<Address>()
            {
                _agentAddress,
                _avatarAddress,
                _rankingMapAddress,
                _avatarAddress.Derive(LegacyInventoryKey),
                _avatarAddress.Derive(LegacyWorldInformationKey),
                _avatarAddress.Derive(LegacyQuestListKey),
            };

            var state = new State();

            var nextState = action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Signer = _agentAddress,
                BlockIndex = 0,
                Rehearsal = true,
            });

            Assert.Equal(updatedAddresses.ToImmutableHashSet(), nextState.UpdatedAddresses);
        }
    }
}
