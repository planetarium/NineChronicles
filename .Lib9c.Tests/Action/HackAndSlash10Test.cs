namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
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
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Mail;
    using Nekoyume.Model.Quest;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;
    using static Lib9c.SerializeKeys;

    public class HackAndSlash10Test
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

        public HackAndSlash10Test()
        {
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
                .SetState(_rankingMapAddress, new RankingMapState(_rankingMapAddress).Serialize())
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
        [InlineData(GameConfig.RequireCharacterLevel.CharacterFullCostumeSlot, 1, 2, 10, false, false, true)]
        [InlineData(GameConfig.RequireCharacterLevel.CharacterFullCostumeSlot, 1, 2, 10, false, true, true)]
        [InlineData(GameConfig.RequireCharacterLevel.CharacterFullCostumeSlot, 1, 1, 1, true, false, true)]
        [InlineData(200, 1, GameConfig.RequireClearedStageLevel.ActionsInRankingBoard, 1, false, false, true)]
        [InlineData(200, 1, GameConfig.RequireClearedStageLevel.ActionsInRankingBoard, 1, true, false, true)]
        [InlineData(GameConfig.RequireCharacterLevel.CharacterFullCostumeSlot, 1, 1, 1, false, false, false)]
        [InlineData(GameConfig.RequireCharacterLevel.CharacterFullCostumeSlot, 1, 1, 1, false, true, false)]
        [InlineData(GameConfig.RequireCharacterLevel.CharacterFullCostumeSlot, 1, 1, 1, true, false, false)]
        [InlineData(200, 1, GameConfig.RequireClearedStageLevel.ActionsInRankingBoard, 1, false, false, false)]
        [InlineData(200, 1, GameConfig.RequireClearedStageLevel.ActionsInRankingBoard, 1, true, false, false)]
        public void Execute(int avatarLevel, int worldId, int stageId, int playCount, bool backward, bool isLock, bool isClearedBefore)
        {
            Assert.True(_tableSheets.WorldSheet.TryGetValue(worldId, out var worldRow));
            Assert.True(stageId >= worldRow.StageBegin);
            Assert.True(stageId <= worldRow.StageEnd);
            Assert.True(_tableSheets.StageSheet.TryGetValue(stageId, out _));

            var previousAvatarState = _initialState.GetAvatarStateV2(_avatarAddress);
            previousAvatarState.level = avatarLevel;
            var clearedStageId = _tableSheets.StageSheet.First?.Id ?? 0;
            clearedStageId = isClearedBefore ? Math.Max(clearedStageId, stageId - 1) : stageId - 1;
            clearedStageId = playCount > 1 ? clearedStageId + 1 : clearedStageId;
            previousAvatarState.worldInformation = new WorldInformation(
                0,
                _tableSheets.WorldSheet,
                clearedStageId);

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
                previousAvatarState.inventory.AddItem(costume);
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
                OrderLock? orderLock = null;
                if (isLock)
                {
                    orderLock = new OrderLock(Guid.NewGuid());
                }

                previousAvatarState.inventory.AddItem(weapon, iLock: orderLock);
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
                previousAvatarState.inventory.AddItem(armor);
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

            var action = new HackAndSlash10
            {
                costumes = costumes,
                equipments = equipments,
                foods = new List<Guid>(),
                worldId = worldId,
                stageId = stageId,
                playCount = playCount,
                avatarAddress = _avatarAddress,
                rankingMapAddress = _rankingMapAddress,
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
            Assert.Equal(!isLock, nextAvatarState.inventory.Equipments.OfType<Weapon>().Any(w => w.equipped));

            var value = nextState.GetState(_rankingMapAddress);
            if (!isClearedBefore)
            {
                var rankingMapState = new RankingMapState((Dictionary)value);
                var info = rankingMapState.GetRankingInfos(null).First();
                Assert.Equal(info.AgentAddress, _agentAddress);
                Assert.Equal(info.AvatarAddress, _avatarAddress);
            }
        }

        [Theory]
        [InlineData(4, 200, 1)]
        [InlineData(4, 200, 2)]
        public void Execute_With_UpdateQuestList(int worldId, int stageId, int playCount)
        {
            var state = _initialState;

            // Remove stageId from WorldQuestSheet
            var worldQuestSheet = state.GetSheet<WorldQuestSheet>();
            var targetRow = worldQuestSheet.OrderedList.FirstOrDefault(e => e.Goal == stageId);
            Assert.NotNull(targetRow);
            // Update new AvatarState
            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                state.GetAvatarSheets(),
                state.GetGameConfigState(),
                _rankingMapAddress)
            {
                level = 400,
                exp = state.GetSheet<CharacterLevelSheet>().OrderedList.First(e => e.Level == 400).Exp,
                worldInformation = new WorldInformation(0, state.GetSheet<WorldSheet>(), stageId),
            };
            var equipments = Doomfist.GetAllParts(_tableSheets, avatarState.level);
            foreach (var equipment in equipments)
            {
                avatarState.inventory.AddItem(equipment);
            }

            state = state
                .SetState(avatarState.address, avatarState.SerializeV2())
                .SetState(_inventoryAddress, avatarState.inventory.Serialize())
                .SetState(_worldInformationAddress, avatarState.worldInformation.Serialize())
                .SetState(_questListAddress, avatarState.questList.Serialize());
            Assert.Equal(400, avatarState.level);
            Assert.True(avatarState.worldInformation.IsWorldUnlocked(worldId));
            Assert.True(avatarState.worldInformation.IsStageCleared(stageId));

            var avatarWorldQuests = avatarState.questList.OfType<WorldQuest>().ToList();
            Assert.Equal(worldQuestSheet.Count, avatarWorldQuests.Count);
            Assert.Empty(avatarState.questList.completedQuestIds);
            Assert.Equal(equipments.Count, avatarState.inventory.Items.Count);

            // HackAndSlash
            var action = new HackAndSlash10
            {
                costumes = new List<Guid>(),
                equipments = equipments.Select(e => e.NonFungibleId).ToList(),
                foods = new List<Guid>(),
                worldId = worldId,
                stageId = stageId,
                playCount = playCount,
                avatarAddress = avatarState.address,
                rankingMapAddress = _rankingMapAddress,
            };

            avatarState = state.GetAvatarStateV2(avatarState.address);
            avatarWorldQuests = avatarState.questList.OfType<WorldQuest>().ToList();
            Assert.DoesNotContain(avatarWorldQuests, e => e.Complete);

            // Second Execute
            state = action.Execute(new ActionContext
            {
                PreviousStates = state,
                Signer = _agentAddress,
                Random = new TestRandom(),
            });

            avatarState = state.GetAvatarStateV2(avatarState.address);
            avatarWorldQuests = avatarState.questList.OfType<WorldQuest>().ToList();
            Assert.Equal(worldQuestSheet.Count, avatarWorldQuests.Count);
            Assert.Single(avatarWorldQuests, e => e.Goal == stageId && e.Complete);
        }

        [Fact]
        public void MaxLevelTest()
        {
            var previousAvatarState = _initialState.GetAvatarStateV2(_avatarAddress);
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

            var state = _initialState.SetState(_avatarAddress, previousAvatarState.SerializeV2());

            var action = new HackAndSlash10
            {
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                foods = new List<Guid>(),
                worldId = worldId,
                stageId = stageId,
                playCount = 1,
                avatarAddress = _avatarAddress,
                rankingMapAddress = _rankingMapAddress,
            };

            var nextState = action.Execute(new ActionContext
            {
                PreviousStates = state,
                Signer = _agentAddress,
                Random = new TestRandom(),
                Rehearsal = false,
            });

            var nextAvatarState = nextState.GetAvatarStateV2(_avatarAddress);
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
            var previousAvatarState = _initialState.GetAvatarStateV2(_avatarAddress);
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

            var state = _initialState
                .SetState(_avatarAddress, previousAvatarState.SerializeV2())
                .SetState(_inventoryAddress, previousAvatarState.inventory.Serialize());

            var action = new HackAndSlash10
            {
                costumes = new List<Guid>(),
                equipments = equipments,
                foods = new List<Guid>(),
                worldId = 1,
                stageId = 1,
                playCount = 1,
                avatarAddress = _avatarAddress,
                rankingMapAddress = _rankingMapAddress,
            };

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
            var action = new HackAndSlash10
            {
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                foods = new List<Guid>(),
                worldId = 1,
                stageId = 1,
                playCount = 1,
                avatarAddress = _avatarAddress,
                rankingMapAddress = default,
            };

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

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Execute_Throw_FailedLoadStateException(bool backward)
        {
            var action = new HackAndSlash10
            {
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                foods = new List<Guid>(),
                worldId = 1,
                stageId = 1,
                playCount = 1,
                avatarAddress = _avatarAddress,
            };

            IAccountStateDelta state = backward ? new State() : _initialState;
            if (!backward)
            {
                state = _initialState
                    .SetState(_avatarAddress, _avatarState.SerializeV2())
                    .SetState(_avatarAddress.Derive(LegacyInventoryKey), null!)
                    .SetState(_avatarAddress.Derive(LegacyWorldInformationKey), null!)
                    .SetState(_avatarAddress.Derive(LegacyQuestListKey), null!);
            }

            var exec = Assert.Throws<FailedLoadStateException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Signer = _agentAddress,
                Random = new TestRandom(),
            }));

            SerializeException<FailedLoadStateException>(exec);
        }

        [Fact]
        public void ExecuteThrowSheetRowNotFoundExceptionByWorld()
        {
            var action = new HackAndSlash10
            {
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                foods = new List<Guid>(),
                worldId = 100,
                stageId = 1,
                playCount = 1,
                avatarAddress = _avatarAddress,
                rankingMapAddress = _rankingMapAddress,
            };

            var exec = Assert.Throws<SheetRowNotFoundException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = _initialState,
                Signer = _agentAddress,
                Random = new TestRandom(),
            }));

            SerializeException<SheetRowNotFoundException>(exec);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(51)]
        public void ExecuteThrowSheetRowColumnException(int stageId)
        {
            var action = new HackAndSlash10
            {
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                foods = new List<Guid>(),
                worldId = 1,
                stageId = stageId,
                playCount = 1,
                avatarAddress = _avatarAddress,
                rankingMapAddress = _rankingMapAddress,
            };

            var exec = Assert.Throws<SheetRowColumnException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = _initialState,
                Signer = _agentAddress,
                Random = new TestRandom(),
            }));

            SerializeException<SheetRowColumnException>(exec);
        }

        [Fact]
        public void ExecuteThrowSheetRowNotFoundExceptionByStage()
        {
            var action = new HackAndSlash10
            {
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                foods = new List<Guid>(),
                worldId = 1,
                stageId = 1,
                playCount = 1,
                avatarAddress = _avatarAddress,
                rankingMapAddress = _rankingMapAddress,
            };

            var state = _initialState;
            state = state.SetState(Addresses.TableSheet.Derive(nameof(StageSheet)), "test".Serialize());

            var exec = Assert.Throws<SheetRowNotFoundException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Signer = _agentAddress,
                Random = new TestRandom(),
            }));

            SerializeException<SheetRowNotFoundException>(exec);
        }

        [Fact]
        public void ExecuteThrowFailedAddWorldException()
        {
            var action = new HackAndSlash10
            {
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                foods = new List<Guid>(),
                worldId = 1,
                stageId = 1,
                playCount = 1,
                avatarAddress = _avatarAddress,
                rankingMapAddress = _rankingMapAddress,
            };

            var state = _initialState;
            var worldSheet = new WorldSheet();
            worldSheet.Set("test");
            var avatarState = new AvatarState(_avatarState)
            {
                worldInformation = new WorldInformation(0, worldSheet, false),
            };
            state = state.SetState(_worldInformationAddress, avatarState.worldInformation.Serialize());

            Assert.False(avatarState.worldInformation.IsStageCleared(0));

            var exec = Assert.Throws<FailedAddWorldException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Signer = _agentAddress,
                Random = new TestRandom(),
            }));

            SerializeException<FailedAddWorldException>(exec);
        }

        [Fact]
        public void ExecuteThrowInvalidWorldException()
        {
            var action = new HackAndSlash10
            {
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                foods = new List<Guid>(),
                worldId = 2,
                stageId = 51,
                playCount = 1,
                avatarAddress = _avatarAddress,
                rankingMapAddress = _rankingMapAddress,
            };

            Assert.False(_avatarState.worldInformation.IsStageCleared(51));

            var exec = Assert.Throws<InvalidWorldException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = _initialState,
                Signer = _agentAddress,
                Random = new TestRandom(),
            }));

            SerializeException<InvalidWorldException>(exec);
        }

        [Fact]
        public void ExecuteThrowInvalidStageException()
        {
            var action = new HackAndSlash10
            {
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                foods = new List<Guid>(),
                worldId = 1,
                stageId = 3,
                playCount = 1,
                avatarAddress = _avatarAddress,
                rankingMapAddress = _rankingMapAddress,
            };

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
            state = state.SetState(_avatarAddress, avatarState.SerializeV2());

            var exec = Assert.Throws<InvalidStageException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Signer = _agentAddress,
                Random = new TestRandom(),
            }));

            SerializeException<InvalidStageException>(exec);
        }

        [Fact]
        public void ExecuteThrowInvalidStageExceptionUnlockedWorld()
        {
            var action = new HackAndSlash10
            {
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                foods = new List<Guid>(),
                worldId = 1,
                stageId = 2,
                playCount = 1,
                avatarAddress = _avatarAddress,
                rankingMapAddress = _rankingMapAddress,
            };

            _avatarState.worldInformation.TryGetWorld(1, out var world);
            Assert.False(world.IsStageCleared);

            var exec = Assert.Throws<InvalidStageException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = _initialState,
                Signer = _agentAddress,
                Random = new TestRandom(),
            }));

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
            avatarState.inventory.AddItem(equipment);

            var action = new HackAndSlash10
            {
                costumes = new List<Guid>(),
                equipments = new List<Guid>()
                {
                    equipment.ItemId,
                },
                foods = new List<Guid>(),
                worldId = 1,
                stageId = 1,
                playCount = 1,
                avatarAddress = _avatarAddress,
                rankingMapAddress = _rankingMapAddress,
            };

            var state = _initialState
                .SetState(_avatarAddress, avatarState.SerializeV2())
                .SetState(_inventoryAddress, avatarState.inventory.Serialize());

            var exec = Assert.Throws<RequiredBlockIndexException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Signer = _agentAddress,
                Random = new TestRandom(),
            }));

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
            var state = _initialState;
            var avatarState = new AvatarState(_avatarState)
            {
                level = 0,
            };
            state = state.SetState(_avatarAddress, avatarState.SerializeV2());

            var equipRow = _tableSheets.EquipmentItemSheet.Values.First(r => r.ItemSubType == itemSubType);
            var equipment = ItemFactory.CreateItemUsable(equipRow, Guid.NewGuid(), 0);
            avatarState.inventory.AddItem(equipment);
            state = state.SetState(_inventoryAddress, avatarState.inventory.Serialize());

            var action = new HackAndSlash10
            {
                costumes = new List<Guid>(),
                equipments = new List<Guid>
                {
                    equipment.ItemId,
                },
                foods = new List<Guid>(),
                worldId = 1,
                stageId = 1,
                playCount = 1,
                avatarAddress = _avatarAddress,
                rankingMapAddress = _rankingMapAddress,
            };

            var exec = Assert.Throws<EquipmentSlotUnlockException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Signer = _agentAddress,
                Random = new TestRandom(),
            }));

            SerializeException<EquipmentSlotUnlockException>(exec);
        }

        [Fact]
        public void ExecuteThrowNotEnoughActionPointException()
        {
            var avatarState = new AvatarState(_avatarState)
            {
                actionPoint = 0,
            };

            var action = new HackAndSlash10
            {
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                foods = new List<Guid>(),
                worldId = 1,
                stageId = 1,
                playCount = 1,
                avatarAddress = _avatarAddress,
                rankingMapAddress = _rankingMapAddress,
            };

            var state = _initialState;
            state = state.SetState(_avatarAddress, avatarState.SerializeV2());

            var exec = Assert.Throws<NotEnoughActionPointException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Signer = _agentAddress,
                Random = new TestRandom(),
            }));

            SerializeException<NotEnoughActionPointException>(exec);
        }

        [Fact]
        public void ExecuteWithoutPlayCount()
        {
            var previousAvatarState = _initialState.GetAvatarStateV2(_avatarAddress);
            previousAvatarState.level = 1;
            var clearedStageId = 0;
            previousAvatarState.worldInformation = new WorldInformation(
                0,
                _tableSheets.WorldSheet,
                clearedStageId);

            var costumes = new List<Guid>();
            var equipments = new List<Guid>();
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
                previousAvatarState.Update(mail);
            }

            IAccountStateDelta state = _initialState
            .SetState(_avatarAddress, previousAvatarState.SerializeV2())
            .SetState(_avatarAddress.Derive(LegacyInventoryKey), previousAvatarState.inventory.Serialize())
            .SetState(_avatarAddress.Derive(LegacyWorldInformationKey), previousAvatarState.worldInformation.Serialize())
            .SetState(_avatarAddress.Derive(LegacyQuestListKey), previousAvatarState.questList.Serialize());

            var action = new HackAndSlash10
            {
                costumes = costumes,
                equipments = equipments,
                foods = new List<Guid>(),
                worldId = 1,
                stageId = 1,
                avatarAddress = _avatarAddress,
                rankingMapAddress = _rankingMapAddress,
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
            Assert.True(nextAvatarState.worldInformation.IsStageCleared(1));
        }

        [Theory]
        [InlineData(true, 1, 15, 100)]
        [InlineData(true, 2, 55, 100)]
        [InlineData(true, 3, 111, 100)]
        [InlineData(true, 4, 189, 100)]
        [InlineData(false, 1, 15, 100)]
        [InlineData(false, 2, 55, 100)]
        [InlineData(false, 3, 111, 100)]
        [InlineData(false, 4, 189, 100)]
        public void CheckRewardItems(bool backward, int worldId, int stageId, int playCount)
        {
            Assert.True(_tableSheets.WorldSheet.TryGetValue(worldId, out var worldRow));
            Assert.True(stageId >= worldRow.StageBegin);
            Assert.True(stageId <= worldRow.StageEnd);
            Assert.True(_tableSheets.StageSheet.TryGetValue(stageId, out var stageRow));

            var previousAvatarState = _initialState.GetAvatarStateV2(_avatarAddress);
            previousAvatarState.actionPoint = 999999;
            previousAvatarState.level = 400;
            var clearedStageId = _tableSheets.StageSheet.First?.Id ?? 0;
            clearedStageId = stageId;
            previousAvatarState.worldInformation = new WorldInformation(
                0,
                _tableSheets.WorldSheet,
                clearedStageId);

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

            List<Guid> equipments = new List<Guid>();

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
            OrderLock? orderLock = null;
            previousAvatarState.inventory.AddItem(weapon, iLock: orderLock);

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
            previousAvatarState.inventory.AddItem(armor);

            var beltId = _tableSheets
                .EquipmentItemSheet
                .Values
                .Where(r => r.ItemSubType == ItemSubType.Belt)
                .OrderBy(r => r.Stat.ValueAsInt)
                .Last()
                .Id;

            var belt = ItemFactory.CreateItem(
                    _tableSheets.EquipmentItemSheet[beltId],
                    random)
                as Equipment;
            equipments.Add(belt.ItemId);
            previousAvatarState.inventory.AddItem(belt);

            var necklaceId = _tableSheets
                .EquipmentItemSheet
                .Values
                .Where(r => r.ItemSubType == ItemSubType.Necklace)
                .OrderBy(r => r.Stat.ValueAsInt)
                .Last()
                .Id;

            var necklace = ItemFactory.CreateItem(
                    _tableSheets.EquipmentItemSheet[necklaceId],
                    random)
                as Equipment;
            equipments.Add(necklace.ItemId);
            previousAvatarState.inventory.AddItem(necklace);

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
                    .SetState(
                        _avatarAddress.Derive(LegacyInventoryKey),
                        previousAvatarState.inventory.Serialize())
                    .SetState(
                        _avatarAddress.Derive(LegacyWorldInformationKey),
                        previousAvatarState.worldInformation.Serialize())
                    .SetState(
                        _avatarAddress.Derive(LegacyQuestListKey),
                        previousAvatarState.questList.Serialize());
            }

            var action = new HackAndSlash10
            {
                costumes = costumes,
                equipments = equipments,
                foods = new List<Guid>(),
                worldId = worldId,
                stageId = stageId,
                playCount = playCount,
                avatarAddress = _avatarAddress,
                rankingMapAddress = _rankingMapAddress,
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

            var rewardItem = nextAvatarState.inventory.Items.Where(
                x => x.item.ItemSubType != ItemSubType.FoodMaterial &&
                     x.item is IFungibleItem ownedFungibleItem &&
                     x.item.Id != 400000 && x.item.Id != 500000);

            Assert.Equal(stageRow.Rewards.Count(), rewardItem.Count());

            var worldQuestSheet = state.GetSheet<WorldQuestSheet>();
            var questRow = worldQuestSheet.OrderedList.FirstOrDefault(e => e.Goal == stageId);
            var questRewardSheet = state.GetSheet<QuestRewardSheet>();
            var rewardIds = questRewardSheet.First(x => x.Key == questRow.QuestRewardId).Value
                .RewardIds;
            var questItemRewardSheet = state.GetSheet<QuestItemRewardSheet>();
            var materialItemSheet = state.GetSheet<MaterialItemSheet>();
            var sortedMaterialItemSheet = materialItemSheet
                .Where(x =>
                    x.Value.ItemSubType == ItemSubType.EquipmentMaterial ||
                    x.Value.ItemSubType == ItemSubType.MonsterPart).ToList();

            var selectedIdn = new Dictionary<int, int>();
            foreach (var row in questItemRewardSheet)
            {
                if (sortedMaterialItemSheet.Exists(x => x.Key.Equals(row.ItemId)))
                {
                    selectedIdn.Add(row.Key, row.Count);
                }
            }

            var questSum = rewardIds.Where(rewardId => selectedIdn.ContainsKey(rewardId))
                .Sum(rewardId => selectedIdn[rewardId]);
            var min = stageRow.Rewards.OrderBy(x => x.Min).First().Min;
            var max = stageRow.Rewards.OrderBy(x => x.Max).First().Max;
            var totalMin = (min * playCount * stageRow.DropItemMin) + questSum;
            var totalMax = (max * playCount * stageRow.DropItemMax) + questSum;
            var totalCount = rewardItem.Sum(x => x.count);
            Assert.InRange(totalCount, totalMin, totalMax);
        }

        [Fact]
        public void Rehearsal()
        {
            var action = new HackAndSlash10
            {
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                foods = new List<Guid>(),
                worldId = 1,
                stageId = 1,
                playCount = 1,
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
