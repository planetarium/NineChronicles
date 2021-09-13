namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization.Formatters.Binary;
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
    using Nekoyume.Model.Mail;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class HackAndSlash5Test
    {
        private readonly TableSheets _tableSheets;

        private readonly Address _agentAddress;

        private readonly Address _avatarAddress;
        private readonly AvatarState _avatarState;

        private readonly Address _rankingMapAddress;

        private readonly WeeklyArenaState _weeklyArenaState;
        private readonly IAccountStateDelta _initialState;

        public HackAndSlash5Test()
        {
            var sheets = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(sheets);

            var privateKey = new PrivateKey();
            _agentAddress = privateKey.PublicKey.ToAddress();
            var agentState = new AgentState(_agentAddress);

            _avatarAddress = _agentAddress.Derive("avatar");
            _rankingMapAddress = _avatarAddress.Derive("ranking_map");
            _avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(sheets[nameof(GameConfigSheet)]),
                _rankingMapAddress
            )
            {
                level = 100,
            };
            agentState.avatarAddresses.Add(0, _avatarAddress);

            _weeklyArenaState = new WeeklyArenaState(0);

            _initialState = new State()
                .SetState(_weeklyArenaState.address, _weeklyArenaState.Serialize())
                .SetState(_agentAddress, agentState.Serialize())
                .SetState(_avatarAddress, _avatarState.Serialize())
                .SetState(_rankingMapAddress, new RankingMapState(_rankingMapAddress).Serialize());

            foreach (var (key, value) in sheets)
            {
                _initialState = _initialState
                    .SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }
        }

        [Theory]
        [InlineData(GameConfig.RequireCharacterLevel.CharacterFullCostumeSlot, 1, 1, false)]
        [InlineData(200, 1, GameConfig.RequireClearedStageLevel.ActionsInRankingBoard, true)]
        public void Execute(int avatarLevel, int worldId, int stageId, bool contains)
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
                Math.Max(_tableSheets.StageSheet.First?.Id ?? 1, stageId - 1));

            List<Guid> costumes = new List<Guid>();
            IRandom random = new TestRandom();
            if (avatarLevel >= GameConfig.RequireCharacterLevel.CharacterFullCostumeSlot)
            {
                var costumeId = _tableSheets
                .CostumeItemSheet
                .Values
                .First(r => r.ItemSubType == ItemSubType.FullCostume)
                .Id;

                var costume = (Costume)ItemFactory.CreateItem(
                    _tableSheets.ItemSheet[costumeId], random);
                previousAvatarState.inventory.AddItem2(costume);
                costumes.Add(costume.ItemId);
            }

            List<Guid> equipments = new List<Guid>();

            if (avatarLevel >= GameConfig.RequireCharacterLevel.CharacterEquipmentSlotWeapon)
            {
                var weaponId = _tableSheets
                    .EquipmentItemSheet
                    .Values
                    .Where(r => r.ItemSubType == ItemSubType.Weapon)
                    .OrderBy(r => r.Stat.ValueAsInt)
                    .Last()
                    .Id;

                var weapon = ItemFactory.CreateItem(
                    _tableSheets.EquipmentItemSheet[weaponId],
                    random)
                    as Equipment;
                equipments.Add(weapon.ItemId);
                previousAvatarState.inventory.AddItem2(weapon);
            }

            if (avatarLevel >= GameConfig.RequireCharacterLevel.CharacterEquipmentSlotArmor)
            {
                var armorId = _tableSheets
                    .EquipmentItemSheet
                    .Values
                    .Where(r => r.ItemSubType == ItemSubType.Armor)
                    .OrderBy(r => r.Stat.ValueAsInt)
                    .Last()
                    .Id;

                var armor = ItemFactory.CreateItem(
                    _tableSheets.EquipmentItemSheet[armorId],
                    random)
                    as Equipment;
                equipments.Add(armor.ItemId);
                previousAvatarState.inventory.AddItem2(armor);
            }

            var mailEquipmentRow = _tableSheets.EquipmentItemSheet.Values.First();
            var mailEquipment = ItemFactory.CreateItemUsable(mailEquipmentRow, default, 0);
            var result = new CombinationConsumable5.ResultModel
            {
                id = default,
                gold = 0,
                actionPoint = 0,
                recipeId = 1,
                materials = new Dictionary<Material, int>(),
                itemUsable = mailEquipment,
            };
            for (var i = 0; i < 100; i++)
            {
                var mail = new CombinationMail(result, i, default, 0);
                previousAvatarState.Update2(mail);
            }

            var state = _initialState.SetState(_avatarAddress, previousAvatarState.Serialize());

            var action = new HackAndSlash5
            {
                costumes = costumes,
                equipments = equipments,
                foods = new List<Guid>(),
                worldId = worldId,
                stageId = stageId,
                avatarAddress = _avatarAddress,
                WeeklyArenaAddress = _weeklyArenaState.address,
                RankingMapAddress = _rankingMapAddress,
            };

            Assert.Null(action.Result);

            var nextState = action.Execute(new ActionContext
            {
                PreviousStates = state,
                Signer = _agentAddress,
                Random = new TestRandom(),
                Rehearsal = false,
                BlockIndex = 1,
            });

            var nextAvatarState = nextState.GetAvatarState(_avatarAddress);
            var newWeeklyState = nextState.GetWeeklyArenaState(0);

            Assert.NotNull(action.Result);

            Assert.NotEmpty(action.Result.OfType<GetReward>());
            Assert.Equal(BattleLog.Result.Win, action.Result.result);
            Assert.Equal(contains, newWeeklyState.ContainsKey(_avatarAddress));
            Assert.True(nextAvatarState.worldInformation.IsStageCleared(stageId));
            Assert.Equal(30, nextAvatarState.mailBox.Count);
            if (contains)
            {
                //Check for Costume CP.
                Assert.True(
                    newWeeklyState[_avatarAddress].CombatPoint >
                    CPHelper.GetCP(nextAvatarState, _tableSheets.CharacterSheet)
                );
            }

            var value = nextState.GetState(_rankingMapAddress);

            var rankingMapState = new RankingMapState((Dictionary)value);
            var info = rankingMapState.GetRankingInfos(null).First();

            Assert.Equal(info.AgentAddress, _agentAddress);
            Assert.Equal(info.AvatarAddress, _avatarAddress);
        }

        [Fact]
        public void MaxLevelTest()
        {
            var previousAvatarState = _initialState.GetAvatarState(_avatarAddress);
            var maxLevel = _tableSheets.CharacterLevelSheet.Max(row => row.Value.Level);
            var expRow = _tableSheets.CharacterLevelSheet[maxLevel];
            var maxLevelExp = expRow.Exp;
            var requiredExp = expRow.ExpNeed;

            previousAvatarState.level = maxLevel;
            previousAvatarState.exp = maxLevelExp + requiredExp - 1;

            var stageId = _tableSheets.StageSheet
                .FirstOrDefault(row =>
                (previousAvatarState.level - row.Value.Id) <= StageRewardExpHelper.DifferLowerLimit ||
                (previousAvatarState.level - row.Value.Id) > StageRewardExpHelper.DifferUpperLimit)
                .Value.Id;
            var worldRow = _tableSheets.WorldSheet
                .FirstOrDefault(row => stageId >= row.Value.StageBegin &&
                stageId <= row.Value.StageEnd);
            var worldId = worldRow.Value.Id;

            previousAvatarState.worldInformation = new WorldInformation(
                0,
                _tableSheets.WorldSheet,
                Math.Max(_tableSheets.StageSheet.First?.Id ?? 1, stageId));

            var state = _initialState.SetState(_avatarAddress, previousAvatarState.Serialize());

            var action = new HackAndSlash5
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

            var nextState = action.Execute(new ActionContext
            {
                PreviousStates = state,
                Signer = _agentAddress,
                Random = new TestRandom(),
                Rehearsal = false,
            });

            var nextAvatarState = nextState.GetAvatarState(_avatarAddress);
            Assert.Equal(maxLevelExp + requiredExp - 1, nextAvatarState.exp);
            Assert.Equal(previousAvatarState.level, nextAvatarState.level);
        }

        [Theory]
        [InlineData(ItemSubType.Weapon, GameConfig.MaxEquipmentSlotCount.Weapon)]
        [InlineData(ItemSubType.Armor, GameConfig.MaxEquipmentSlotCount.Armor)]
        [InlineData(ItemSubType.Belt, GameConfig.MaxEquipmentSlotCount.Belt)]
        [InlineData(ItemSubType.Necklace, GameConfig.MaxEquipmentSlotCount.Necklace)]
        [InlineData(ItemSubType.Ring, GameConfig.MaxEquipmentSlotCount.Ring)]
        public void MultipleEquipmentTest(ItemSubType type, int maxCount)
        {
            var previousAvatarState = _initialState.GetAvatarState(_avatarAddress);
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
                previousAvatarState.inventory.AddItem2(equipment);
            }

            var state = _initialState.SetState(_avatarAddress, previousAvatarState.Serialize());

            var action = new HackAndSlash5
            {
                costumes = new List<Guid>(),
                equipments = equipments,
                foods = new List<Guid>(),
                worldId = 1,
                stageId = 1,
                avatarAddress = _avatarAddress,
                WeeklyArenaAddress = _weeklyArenaState.address,
                RankingMapAddress = _rankingMapAddress,
            };

            Assert.Null(action.Result);

            var exec = Assert.Throws<DuplicateEquipmentException>(() => action.Execute(new ActionContext
            {
                PreviousStates = state,
                Signer = _agentAddress,
                Random = new TestRandom(),
                Rehearsal = false,
            }));

            SerializeException<DuplicateEquipmentException>(exec);
        }

        [Fact]
        public void ExecuteThrowInvalidRankingMapAddress()
        {
            var action = new HackAndSlash5()
            {
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                foods = new List<Guid>(),
                worldId = 1,
                stageId = 1,
                avatarAddress = _avatarAddress,
                WeeklyArenaAddress = _weeklyArenaState.address,
                RankingMapAddress = default,
            };

            Assert.Null(action.Result);

            var exec = Assert.Throws<InvalidAddressException>(() =>
                action.Execute(new ActionContext()
                {
                    PreviousStates = _initialState,
                    Signer = _agentAddress,
                    Random = new TestRandom(),
                    Rehearsal = false,
                })
            );

            SerializeException<InvalidAddressException>(exec);
        }

        [Fact]
        public void ExecuteThrowFailedLoadStateException()
        {
            var action = new HackAndSlash5()
            {
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                foods = new List<Guid>(),
                worldId = 1,
                stageId = 1,
                avatarAddress = _avatarAddress,
                WeeklyArenaAddress = _weeklyArenaState.address,
            };

            Assert.Null(action.Result);

            var exec = Assert.Throws<FailedLoadStateException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = new State(),
                Signer = _agentAddress,
                Random = new TestRandom(),
            }));

            Assert.Null(action.Result);

            SerializeException<FailedLoadStateException>(exec);
        }

        [Fact]
        public void ExecuteThrowSheetRowNotFoundExceptionByWorld()
        {
            var action = new HackAndSlash5()
            {
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                foods = new List<Guid>(),
                worldId = 100,
                stageId = 1,
                avatarAddress = _avatarAddress,
                WeeklyArenaAddress = _weeklyArenaState.address,
                RankingMapAddress = _rankingMapAddress,
            };

            Assert.Null(action.Result);

            var exec = Assert.Throws<SheetRowNotFoundException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = _initialState,
                Signer = _agentAddress,
                Random = new TestRandom(),
            }));

            Assert.Null(action.Result);

            SerializeException<SheetRowNotFoundException>(exec);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(51)]
        public void ExecuteThrowSheetRowColumnException(int stageId)
        {
            var action = new HackAndSlash5()
            {
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                foods = new List<Guid>(),
                worldId = 1,
                stageId = stageId,
                avatarAddress = _avatarAddress,
                WeeklyArenaAddress = _weeklyArenaState.address,
                RankingMapAddress = _rankingMapAddress,
            };

            Assert.Null(action.Result);

            var exec = Assert.Throws<SheetRowColumnException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = _initialState,
                Signer = _agentAddress,
                Random = new TestRandom(),
            }));

            Assert.Null(action.Result);

            SerializeException<SheetRowColumnException>(exec);
        }

        [Fact]
        public void ExecuteThrowSheetRowNotFoundExceptionByStage()
        {
            var action = new HackAndSlash5()
            {
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                foods = new List<Guid>(),
                worldId = 1,
                stageId = 1,
                avatarAddress = _avatarAddress,
                WeeklyArenaAddress = _weeklyArenaState.address,
                RankingMapAddress = _rankingMapAddress,
            };

            Assert.Null(action.Result);

            var state = _initialState;
            state = state.SetState(Addresses.TableSheet.Derive(nameof(StageSheet)), "test".Serialize());

            var exec = Assert.Throws<SheetRowNotFoundException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Signer = _agentAddress,
                Random = new TestRandom(),
            }));

            Assert.Null(action.Result);

            SerializeException<SheetRowNotFoundException>(exec);
        }

        [Fact]
        public void ExecuteThrowFailedAddWorldException()
        {
            var action = new HackAndSlash5()
            {
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                foods = new List<Guid>(),
                worldId = 1,
                stageId = 1,
                avatarAddress = _avatarAddress,
                WeeklyArenaAddress = _weeklyArenaState.address,
                RankingMapAddress = _rankingMapAddress,
            };

            Assert.Null(action.Result);

            var state = _initialState;
            var worldSheet = new WorldSheet();
            worldSheet.Set("test");
            var avatarState = new AvatarState(_avatarState)
            {
                worldInformation = new WorldInformation(0, worldSheet, false),
            };
            state = state.SetState(_avatarAddress, avatarState.Serialize());

            Assert.False(avatarState.worldInformation.IsStageCleared(0));

            var exec = Assert.Throws<FailedAddWorldException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Signer = _agentAddress,
                Random = new TestRandom(),
            }));

            Assert.Null(action.Result);

            SerializeException<FailedAddWorldException>(exec);
        }

        [Fact]
        public void ExecuteThrowInvalidWorldException()
        {
            var action = new HackAndSlash5()
            {
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                foods = new List<Guid>(),
                worldId = 2,
                stageId = 51,
                avatarAddress = _avatarAddress,
                WeeklyArenaAddress = _weeklyArenaState.address,
                RankingMapAddress = _rankingMapAddress,
            };

            Assert.Null(action.Result);

            Assert.False(_avatarState.worldInformation.IsStageCleared(51));

            var exec = Assert.Throws<InvalidWorldException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = _initialState,
                Signer = _agentAddress,
                Random = new TestRandom(),
            }));

            Assert.Null(action.Result);

            SerializeException<InvalidWorldException>(exec);
        }

        [Fact]
        public void ExecuteThrowInvalidStageException()
        {
            var action = new HackAndSlash5()
            {
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                foods = new List<Guid>(),
                worldId = 1,
                stageId = 3,
                avatarAddress = _avatarAddress,
                WeeklyArenaAddress = _weeklyArenaState.address,
                RankingMapAddress = _rankingMapAddress,
            };

            Assert.Null(action.Result);

            var avatarState = new AvatarState(_avatarState);
            avatarState.worldInformation.ClearStage(
                1,
                1,
                0,
                _tableSheets.WorldSheet,
                _tableSheets.WorldUnlockSheet
            );

            avatarState.worldInformation.TryGetWorld(1, out var world);

            Assert.True(world.IsStageCleared);
            Assert.True(avatarState.worldInformation.IsWorldUnlocked(1));

            var state = _initialState;
            state = state.SetState(_avatarAddress, avatarState.Serialize());

            var exec = Assert.Throws<InvalidStageException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Signer = _agentAddress,
                Random = new TestRandom(),
            }));

            Assert.Null(action.Result);

            SerializeException<InvalidStageException>(exec);
        }

        [Fact]
        public void ExecuteThrowInvalidStageExceptionUnlockedWorld()
        {
            var action = new HackAndSlash5()
            {
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                foods = new List<Guid>(),
                worldId = 1,
                stageId = 2,
                avatarAddress = _avatarAddress,
                WeeklyArenaAddress = _weeklyArenaState.address,
                RankingMapAddress = _rankingMapAddress,
            };

            Assert.Null(action.Result);

            _avatarState.worldInformation.TryGetWorld(1, out var world);
            Assert.False(world.IsStageCleared);

            var exec = Assert.Throws<InvalidStageException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = _initialState,
                Signer = _agentAddress,
                Random = new TestRandom(),
            }));

            Assert.Null(action.Result);

            SerializeException<InvalidStageException>(exec);
        }

        [Theory]
        [InlineData(ItemSubType.Weapon)]
        [InlineData(ItemSubType.Armor)]
        [InlineData(ItemSubType.Belt)]
        [InlineData(ItemSubType.Necklace)]
        [InlineData(ItemSubType.Ring)]
        public void ExecuteThrowInvalidEquipmentException(ItemSubType itemSubType)
        {
            var avatarState = new AvatarState(_avatarState);
            var equipRow = _tableSheets.EquipmentItemSheet.Values.First(r => r.ItemSubType == itemSubType);
            var equipment = ItemFactory.CreateItemUsable(equipRow, Guid.NewGuid(), 100);
            avatarState.inventory.AddItem2(equipment);

            var action = new HackAndSlash5()
            {
                costumes = new List<Guid>(),
                equipments = new List<Guid>()
                {
                    equipment.ItemId,
                },
                foods = new List<Guid>(),
                worldId = 1,
                stageId = 1,
                avatarAddress = _avatarAddress,
                WeeklyArenaAddress = _weeklyArenaState.address,
                RankingMapAddress = _rankingMapAddress,
            };

            Assert.Null(action.Result);

            var state = _initialState;
            state = state.SetState(_avatarAddress, avatarState.Serialize());

            var exec = Assert.Throws<RequiredBlockIndexException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Signer = _agentAddress,
                Random = new TestRandom(),
            }));

            Assert.Null(action.Result);

            SerializeException<RequiredBlockIndexException>(exec);
        }

        [Theory]
        [InlineData(ItemSubType.Weapon)]
        [InlineData(ItemSubType.Armor)]
        [InlineData(ItemSubType.Belt)]
        [InlineData(ItemSubType.Necklace)]
        [InlineData(ItemSubType.Ring)]
        public void ExecuteThrowEquipmentSlotUnlockException(ItemSubType itemSubType)
        {
            var avatarState = new AvatarState(_avatarState);
            var equipRow = _tableSheets.EquipmentItemSheet.Values.First(r => r.ItemSubType == itemSubType);
            var equipment = ItemFactory.CreateItemUsable(equipRow, Guid.NewGuid(), 0);
            avatarState.inventory.AddItem2(equipment);
            avatarState.level = 0;

            var action = new HackAndSlash5()
            {
                costumes = new List<Guid>(),
                equipments = new List<Guid>()
                {
                    equipment.ItemId,
                },
                foods = new List<Guid>(),
                worldId = 1,
                stageId = 1,
                avatarAddress = _avatarAddress,
                WeeklyArenaAddress = _weeklyArenaState.address,
                RankingMapAddress = _rankingMapAddress,
            };

            Assert.Null(action.Result);

            var state = _initialState;
            state = state.SetState(_avatarAddress, avatarState.Serialize());

            var exec = Assert.Throws<EquipmentSlotUnlockException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Signer = _agentAddress,
                Random = new TestRandom(),
            }));

            Assert.Null(action.Result);

            SerializeException<EquipmentSlotUnlockException>(exec);
        }

        [Fact]
        public void ExecuteThrowNotEnoughActionPointException()
        {
            var avatarState = new AvatarState(_avatarState)
            {
                actionPoint = 0,
            };

            var action = new HackAndSlash5()
            {
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                foods = new List<Guid>(),
                worldId = 1,
                stageId = 1,
                avatarAddress = _avatarAddress,
                WeeklyArenaAddress = _weeklyArenaState.address,
                RankingMapAddress = _rankingMapAddress,
            };

            Assert.Null(action.Result);

            var state = _initialState;
            state = state.SetState(_avatarAddress, avatarState.Serialize());

            var exec = Assert.Throws<NotEnoughActionPointException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Signer = _agentAddress,
                Random = new TestRandom(),
            }));

            Assert.Null(action.Result);

            SerializeException<NotEnoughActionPointException>(exec);
        }

        private static void SerializeException<T>(Exception exec)
            where T : Exception
        {
            var formatter = new BinaryFormatter();
            using var ms = new MemoryStream();
            formatter.Serialize(ms, exec);

            ms.Seek(0, SeekOrigin.Begin);
            var deserialized = (T)formatter.Deserialize(ms);

            Assert.Equal(exec.Message, deserialized.Message);
        }
    }
}
