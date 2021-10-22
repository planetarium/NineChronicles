namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
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

    public class MimisbrunnrBattle0Test
    {
        private readonly TableSheets _tableSheets;

        private readonly Address _agentAddress;

        private readonly Address _avatarAddress;

        private readonly Address _rankingMapAddress;

        private readonly WeeklyArenaState _weeklyArenaState;
        private readonly IAccountStateDelta _initialState;

        public MimisbrunnrBattle0Test()
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

            _weeklyArenaState = new WeeklyArenaState(0);

            _initialState = new State()
                .SetState(_weeklyArenaState.address, _weeklyArenaState.Serialize())
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
        [InlineData(200, GameConfig.MimisbrunnrWorldId, GameConfig.MimisbrunnrStartStageId, 140)]
        [InlineData(400, GameConfig.MimisbrunnrWorldId, GameConfig.MimisbrunnrStartStageId, 100)]
        public void Execute(int avatarLevel, int worldId, int stageId, int clearStageId)
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
            previousAvatarState.inventory.AddItem2(costume);

            var mimisbrunnrSheet = _tableSheets.MimisbrunnrSheet;
            if (!mimisbrunnrSheet.TryGetValue(stageId, out var mimisbrunnrSheetRow))
            {
                throw new SheetRowNotFoundException("MimisbrunnrSheet", stageId);
            }

            var equipments = new List<Guid>();

            var equipmentRow =
                _tableSheets.EquipmentItemSheet.Values.Last(x => x.Id == 10151001);
            var equipment = ItemFactory.CreateItemUsable(equipmentRow, Guid.NewGuid(), 0);
            previousAvatarState.inventory.AddItem(equipment);

            var armorEquipmentRow = _tableSheets.EquipmentItemSheet.Values.Last(x => x.Id == 10251001);
            var armorEquipment = ItemFactory.CreateItemUsable(armorEquipmentRow, Guid.NewGuid(), 0);
            previousAvatarState.inventory.AddItem(armorEquipment);

            var beltEquipment = ItemFactory.CreateItemUsable(
                _tableSheets.EquipmentItemSheet.Values.Last(x => x.Id == 10351000), Guid.NewGuid(), 0);
            previousAvatarState.inventory.AddItem(beltEquipment);

            var necklaceEquipment = ItemFactory.CreateItemUsable(
                _tableSheets.EquipmentItemSheet.Values.Last(x => x.Id == 10451000), Guid.NewGuid(), 0);
            previousAvatarState.inventory.AddItem(necklaceEquipment);
            equipments.Add(equipment.ItemId);
            equipments.Add(armorEquipment.ItemId);
            equipments.Add(beltEquipment.ItemId);
            equipments.Add(necklaceEquipment.ItemId);

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
                previousAvatarState.Update2(mail);
            }

            var state = _initialState.SetState(_avatarAddress, previousAvatarState.Serialize());

            var action = new MimisbrunnrBattle0()
            {
                costumes = new List<Guid> { ((Costume)costume).ItemId },
                equipments = equipments,
                foods = new List<Guid>(),
                worldId = worldId,
                stageId = stageId,
                avatarAddress = _avatarAddress,
                WeeklyArenaAddress = _weeklyArenaState.address,
                RankingMapAddress = _rankingMapAddress,
            };

            Assert.Null(action.Result);

            var nextState = action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Signer = _agentAddress,
                Random = new TestRandom(),
                Rehearsal = false,
            });

            var nextAvatarState = nextState.GetAvatarState(_avatarAddress);
            var newWeeklyState = nextState.GetWeeklyArenaState(0);
            Assert.NotNull(action.Result);
            var reward = action.Result.OfType<GetReward>();
            Assert.NotEmpty(reward);
            Assert.Equal(BattleLog.Result.Win, action.Result.result);
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
            previousAvatarState.inventory.AddItem2(equipment);

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

            var action = new MimisbrunnrBattle0()
            {
                costumes = new List<Guid> { costume.ItemId },
                equipments = new List<Guid>() { equipment.ItemId },
                foods = new List<Guid>(),
                worldId = worldId,
                stageId = stageId,
                avatarAddress = _avatarAddress,
                WeeklyArenaAddress = _weeklyArenaState.address,
                RankingMapAddress = _rankingMapAddress,
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
            var action = new MimisbrunnrBattle0()
            {
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                foods = new List<Guid>(),
                worldId = 10001,
                stageId = 10000002,
                avatarAddress = _avatarAddress,
                WeeklyArenaAddress = _weeklyArenaState.address,
                RankingMapAddress = _rankingMapAddress,
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
            var action = new MimisbrunnrBattle0()
            {
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                foods = new List<Guid>(),
                worldId = 10001,
                stageId = 10000002,
                avatarAddress = _avatarAddress,
                WeeklyArenaAddress = _weeklyArenaState.address,
                RankingMapAddress = default,
            };

            Assert.Null(action.Result);

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
            var action = new MimisbrunnrBattle0()
            {
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                foods = new List<Guid>(),
                worldId = 10011,
                stageId = 10000002,
                avatarAddress = _avatarAddress,
                WeeklyArenaAddress = _weeklyArenaState.address,
                RankingMapAddress = _rankingMapAddress,
            };

            Assert.Null(action.Result);

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
            var action = new MimisbrunnrBattle0()
            {
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                foods = new List<Guid>(),
                worldId = 10001,
                stageId = 10000022,
                avatarAddress = _avatarAddress,
                WeeklyArenaAddress = _weeklyArenaState.address,
                RankingMapAddress = _rankingMapAddress,
            };

            Assert.Null(action.Result);

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

            var action = new MimisbrunnrBattle0()
            {
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                foods = new List<Guid>(),
                worldId = 10001,
                stageId = 10000001,
                avatarAddress = _avatarAddress,
                WeeklyArenaAddress = _weeklyArenaState.address,
                RankingMapAddress = _rankingMapAddress,
            };

            Assert.Null(action.Result);
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
            previousAvatarState.inventory.AddItem2(costume);

            var mimisbrunnrSheet = _tableSheets.MimisbrunnrSheet;
            if (!mimisbrunnrSheet.TryGetValue(stageId, out var mimisbrunnrSheetRow))
            {
                throw new SheetRowNotFoundException("MimisbrunnrSheet", stageId);
            }

            var equipmentRow =
                _tableSheets.EquipmentItemSheet.Values.First(x => x.ElementalType == ElementalType.Fire);
            var equipment = ItemFactory.CreateItemUsable(equipmentRow, default, 0);
            previousAvatarState.inventory.AddItem2(equipment);

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
                previousAvatarState.Update2(mail);
            }

            var state = _initialState.SetState(_avatarAddress, previousAvatarState.Serialize());

            var action = new MimisbrunnrBattle0()
            {
                costumes = new List<Guid> { ((Costume)costume).ItemId },
                equipments = new List<Guid>() { equipment.ItemId },
                foods = new List<Guid>(),
                worldId = worldId,
                stageId = stageId,
                avatarAddress = _avatarAddress,
                WeeklyArenaAddress = _weeklyArenaState.address,
                RankingMapAddress = _rankingMapAddress,
            };

            Assert.Null(action.Result);

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

            var action = new MimisbrunnrBattle0
            {
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                foods = new List<Guid>(),
                worldId = worldId,
                stageId = stageId,
                avatarAddress = _avatarAddress,
                WeeklyArenaAddress = _weeklyArenaState.address,
                RankingMapAddress = _rankingMapAddress,
            };

            Assert.Null(action.Result);

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
            avatarState.inventory.AddItem2(costume);

            var equipmentRow =
                _tableSheets.EquipmentItemSheet.OrderedList.First(x => x.ElementalType == ElementalType.Fire);
            var equipment = ItemFactory.CreateItemUsable(equipmentRow, default, 0);
            avatarState.inventory.AddItem2(equipment);
            var nextState = _initialState.SetState(_avatarAddress, avatarState.Serialize());

            var action = new MimisbrunnrBattle0()
            {
                costumes = new List<Guid> { ((Costume)costume).ItemId },
                equipments = new List<Guid>() { equipment.ItemId },
                foods = new List<Guid>(),
                worldId = GameConfig.MimisbrunnrWorldId,
                stageId = GameConfig.MimisbrunnrStartStageId,
                avatarAddress = _avatarAddress,
                WeeklyArenaAddress = _weeklyArenaState.address,
                RankingMapAddress = _rankingMapAddress,
            };

            Assert.Null(action.Result);

            action.Execute(new ActionContext
            {
                PreviousStates = nextState,
                Signer = _agentAddress,
                Rehearsal = false,
                Random = new TestRandom(),
            });

            var spawnPlayer = action.Result.FirstOrDefault(e => e is SpawnPlayer);
            Assert.NotNull(spawnPlayer);
            Assert.True(spawnPlayer.Character is Player p);
            var player = (Player)spawnPlayer.Character;
            Assert.Equal(player.Costumes.First().ItemId, ((Costume)costume).ItemId);
            Assert.Equal(player.Equipments.First().ItemId, equipment.ItemId);
        }
    }
}
