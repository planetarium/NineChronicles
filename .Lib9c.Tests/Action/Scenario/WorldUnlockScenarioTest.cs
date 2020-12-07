namespace Lib9c.Tests.Action.Scenario
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class WorldUnlockScenarioTest
    {
        private TableSheets _tableSheets;
        private IAccountStateDelta _initialState;
        private Address _agentAddress;
        private Address _avatarAddress;
        private Address _rankingMapAddress;
        private WeeklyArenaState _weeklyArenaState;

        public WorldUnlockScenarioTest()
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
                level = 100,
            };
            agentState.avatarAddresses.Add(0, _avatarAddress);

            _weeklyArenaState = new WeeklyArenaState(0);

            _initialState = new Lib9c.Tests.Action.State()
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
        [InlineData(1, 1, 1, 2)]
        [InlineData(400, 3, 101, 4)]
        public void UnlockWorldByHackAndSlashAfterPatchTable(
            int avatarLevel,
            int worldIdToClear,
            int stageIdToClear,
            int worldIdToUnlock)
        {
            Assert.True(_tableSheets.CharacterLevelSheet.ContainsKey(avatarLevel));
            Assert.True(_tableSheets.WorldSheet.ContainsKey(worldIdToClear));
            Assert.True(_tableSheets.StageSheet.ContainsKey(stageIdToClear));
            Assert.True(_tableSheets.WorldSheet.ContainsKey(worldIdToUnlock));
            Assert.False(_tableSheets.WorldUnlockSheet.TryGetUnlockedInformation(
                worldIdToClear,
                stageIdToClear,
                out _));

            var avatarState = _initialState.GetAvatarState(_avatarAddress);
            avatarState.level = avatarLevel;
            avatarState.worldInformation = new WorldInformation(0, _tableSheets.WorldSheet, stageIdToClear);
            Assert.True(avatarState.worldInformation.IsWorldUnlocked(worldIdToClear));
            Assert.False(avatarState.worldInformation.IsWorldUnlocked(worldIdToUnlock));

            var nextState = _initialState.SetState(_avatarAddress, avatarState.Serialize());
            var hackAndSlash = new HackAndSlash3
            {
                worldId = worldIdToClear,
                stageId = stageIdToClear,
                avatarAddress = _avatarAddress,
                costumes = new List<int>(),
                equipments = new List<Guid>(),
                foods = new List<Guid>(),
                WeeklyArenaAddress = _weeklyArenaState.address,
                RankingMapAddress = _rankingMapAddress,
            };
            nextState = hackAndSlash.Execute(new ActionContext
            {
                PreviousStates = nextState,
                Signer = _agentAddress,
                Random = new TestRandom(),
                Rehearsal = false,
            });
            Assert.True(hackAndSlash.Result.IsClear);

            avatarState = nextState.GetAvatarState(_avatarAddress);
            Assert.True(avatarState.worldInformation.IsStageCleared(stageIdToClear));
            Assert.False(avatarState.worldInformation.IsWorldUnlocked(worldIdToUnlock));

            var tableCsv = _initialState.GetSheetCsv<WorldUnlockSheet>();
            var worldUnlockSheet = _initialState.GetSheet<WorldUnlockSheet>();
            var newId = worldUnlockSheet.Last?.Id + 1 ?? 1;
            var newLine = $"{newId},{worldIdToClear},{stageIdToClear},{worldIdToUnlock}";
            tableCsv = new StringBuilder(tableCsv).AppendLine(newLine).ToString();

            var patchTableSheet = new PatchTableSheet
            {
                TableName = nameof(WorldUnlockSheet),
                TableCsv = tableCsv,
            };
            nextState = patchTableSheet.Execute(new ActionContext
            {
                PreviousStates = nextState,
                Signer = AdminState.Address,
                Random = new TestRandom(),
                Rehearsal = false,
            });

            var nextTableCsv = nextState.GetSheetCsv<WorldUnlockSheet>();
            Assert.Equal(nextTableCsv, tableCsv);

            nextState = hackAndSlash.Execute(new ActionContext
            {
                PreviousStates = nextState,
                Signer = _agentAddress,
                Random = new TestRandom(),
                Rehearsal = false,
            });
            Assert.True(hackAndSlash.Result.IsClear);

            avatarState = nextState.GetAvatarState(_avatarAddress);
            Assert.True(avatarState.worldInformation.IsWorldUnlocked(worldIdToUnlock));
        }

        [Theory]
        [InlineData(400, 2, 80, 10001, 10000001)]
        [InlineData(400, 2, 81, 10001, 10000001)]
        public void UnlockWorldByMimisbrunnrBttleAfterPatchTable(
            int avatarLevel,
            int worldIdToClear,
            int stageIdToClear,
            int worldIdToUnlock,
            int stageIdToUnlock)
        {
            Assert.True(_tableSheets.CharacterLevelSheet.ContainsKey(avatarLevel));
            Assert.True(_tableSheets.WorldSheet.ContainsKey(worldIdToClear));
            Assert.True(_tableSheets.StageSheet.ContainsKey(stageIdToClear));
            Assert.True(_tableSheets.WorldSheet.ContainsKey(worldIdToUnlock));
            Assert.False(_tableSheets.WorldUnlockSheet.TryGetUnlockedInformation(
                worldIdToClear,
                stageIdToClear,
                out _));

            var avatarState = _initialState.GetAvatarState(_avatarAddress);
            avatarState.level = avatarLevel;

            avatarState.worldInformation = new WorldInformation(0, _tableSheets.WorldSheet, stageIdToClear);
            Assert.True(avatarState.worldInformation.IsWorldUnlocked(worldIdToClear));
            Assert.False(avatarState.worldInformation.IsWorldUnlocked(worldIdToUnlock));

            var nextState = _initialState.SetState(_avatarAddress, avatarState.Serialize());
            var hackAndSlash = new HackAndSlash3
            {
                worldId = worldIdToClear,
                stageId = stageIdToClear,
                avatarAddress = _avatarAddress,
                costumes = new List<int>(),
                equipments = new List<Guid>(),
                foods = new List<Guid>(),
                WeeklyArenaAddress = _weeklyArenaState.address,
                RankingMapAddress = _rankingMapAddress,
            };
            nextState = hackAndSlash.Execute(new ActionContext
            {
                PreviousStates = nextState,
                Signer = _agentAddress,
                Random = new TestRandom(),
                Rehearsal = false,
            });
            Assert.True(hackAndSlash.Result.IsClear);

            var mimisbrunnrBattle = new MimisbrunnrBattle()
            {
                worldId = worldIdToUnlock,
                stageId = stageIdToUnlock,
                avatarAddress = _avatarAddress,
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                foods = new List<Guid>(),
                WeeklyArenaAddress = _weeklyArenaState.address,
                RankingMapAddress = _rankingMapAddress,
            };

            Assert.Null(mimisbrunnrBattle.Result);

            Assert.Throws<InvalidWorldException>(() =>
            {
                mimisbrunnrBattle.Execute(new ActionContext
                {
                    PreviousStates = nextState,
                    Signer = _agentAddress,
                    Random = new TestRandom(),
                    Rehearsal = false,
                });
            });

            avatarState = nextState.GetAvatarState(_avatarAddress);
            Assert.True(avatarState.worldInformation.IsStageCleared(stageIdToClear));
            Assert.False(avatarState.worldInformation.IsWorldUnlocked(worldIdToUnlock));

            var tableCsv = _initialState.GetSheetCsv<WorldUnlockSheet>();
            var newTable = new StringBuilder(tableCsv).Replace("4,2,100,10001", "4,2,80,10001").ToString();

            var patchTableSheet = new PatchTableSheet
            {
                TableName = nameof(WorldUnlockSheet),
                TableCsv = newTable,
            };

            nextState = patchTableSheet.Execute(new ActionContext
            {
                PreviousStates = nextState,
                Signer = AdminState.Address,
                Random = new TestRandom(),
                Rehearsal = false,
            });

            var nextTableCsv = nextState.GetSheetCsv<WorldUnlockSheet>();
            Assert.Equal(nextTableCsv, newTable);

            nextState = hackAndSlash.Execute(new ActionContext
            {
                PreviousStates = nextState,
                Signer = _agentAddress,
                Random = new TestRandom(),
                Rehearsal = false,
            });

            avatarState = nextState.GetAvatarState(_avatarAddress);
            Assert.True(hackAndSlash.Result.IsClear);
            Assert.True(avatarState.worldInformation.IsWorldUnlocked(worldIdToUnlock));

            nextState = mimisbrunnrBattle.Execute(new ActionContext
            {
                PreviousStates = nextState,
                Signer = _agentAddress,
                Random = new TestRandom(),
                Rehearsal = false,
            });

            avatarState = nextState.GetAvatarState(_avatarAddress);
            Assert.True(mimisbrunnrBattle.Result.IsClear);
            Assert.True(avatarState.worldInformation.IsWorldUnlocked(worldIdToUnlock));
        }
    }
}
