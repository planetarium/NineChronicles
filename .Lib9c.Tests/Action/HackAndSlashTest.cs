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
    using Nekoyume.Model;
    using Nekoyume.Model.BattleStatus;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class HackAndSlashTest
    {
        private readonly Dictionary<string, string> _sheets;
        private readonly TableSheets _tableSheets;

        private readonly Address _agentAddress;
        private readonly AgentState _agentState;

        private readonly Address _avatarAddress;
        private readonly AvatarState _avatarState;

        private readonly Address _rankingMapAddress;

        private readonly WeeklyArenaState _weeklyArenaState;
        private readonly IAccountStateDelta _initialState;

        public HackAndSlashTest()
        {
            _sheets = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(_sheets);

            var privateKey = new PrivateKey();
            _agentAddress = privateKey.PublicKey.ToAddress();
            _agentState = new AgentState(_agentAddress);

            _avatarAddress = _agentAddress.Derive("avatar");
            _rankingMapAddress = _avatarAddress.Derive("ranking_map");
            _avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(_sheets[nameof(GameConfigSheet)]),
                _rankingMapAddress
            )
            {
                level = 100,
            };
            _agentState.avatarAddresses.Add(0, _avatarAddress);

            _weeklyArenaState = new WeeklyArenaState(0);

            _initialState = new State()
                .SetState(_weeklyArenaState.address, _weeklyArenaState.Serialize())
                .SetState(_agentAddress, _agentState.Serialize())
                .SetState(_avatarAddress, _avatarState.Serialize())
                .SetState(_rankingMapAddress, new RankingMapState(_rankingMapAddress).Serialize());

            foreach (var (key, value) in _sheets)
            {
                _initialState = _initialState
                    .SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }
        }

        [Theory]
        [InlineData(1, 1, 1, false)]
        [InlineData(100, 1, GameConfig.RequireClearedStageLevel.ActionsInRankingBoard, true)]
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

            var state = _initialState.SetState(_avatarAddress, previousAvatarState.Serialize());

            var action = new HackAndSlash()
            {
                costumes = new List<int>(),
                equipments = new List<Guid>(),
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
                Random = new ItemEnhancementTest.TestRandom(),
                Rehearsal = false,
            });

            var nextAvatarState = nextState.GetAvatarState(_avatarAddress);
            var newWeeklyState = nextState.GetWeeklyArenaState(0);

            Assert.NotNull(action.Result);

            Assert.NotEmpty(action.Result.OfType<GetReward>());
            Assert.Equal(BattleLog.Result.Win, action.Result.result);
            Assert.Equal(contains, newWeeklyState.ContainsKey(_avatarAddress));
            Assert.True(nextAvatarState.worldInformation.IsStageCleared(stageId));

            var value = nextState.GetState(_rankingMapAddress);

            var rankingMapState = new RankingMapState((Dictionary)value);
            var info = rankingMapState.GetRankingInfos(null).First();

            Assert.Equal(info.AgentAddress, _agentAddress);
            Assert.Equal(info.AvatarAddress, _avatarAddress);
        }

        [Fact]
        public void ExecuteInvalidRankingMapAddress()
        {
            var action = new HackAndSlash()
            {
                costumes = new List<int>(),
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
                    Random = new ItemEnhancementTest.TestRandom(),
                    Rehearsal = false,
                })
            );

            var formatter = new BinaryFormatter();
            using var ms = new MemoryStream();
            formatter.Serialize(ms, exec);
            ms.Seek(0, SeekOrigin.Begin);

            var deserialized = (InvalidAddressException)formatter.Deserialize(ms);
            Assert.Equal(exec.Message, deserialized.Message);
        }

        [Fact]
        public void SerializeWithDotnetAPI()
        {
            var action = new HackAndSlash()
            {
                costumes = new List<int>(),
                equipments = new List<Guid>(),
                foods = new List<Guid>(),
                worldId = 1,
                stageId = 1,
                avatarAddress = _avatarAddress,
                WeeklyArenaAddress = _weeklyArenaState.address,
                RankingMapAddress = _rankingMapAddress,
            };

            action.Execute(new ActionContext()
            {
                PreviousStates = _initialState,
                Signer = _agentAddress,
                Random = new ItemEnhancementTest.TestRandom(),
                Rehearsal = false,
            });

            var formatter = new BinaryFormatter();
            using var ms = new MemoryStream();
            formatter.Serialize(ms, action);
            ms.Seek(0, SeekOrigin.Begin);

            var deserialized = (HackAndSlash)formatter.Deserialize(ms);
            Assert.Equal(action.PlainValue, deserialized.PlainValue);
        }

        [Fact]
        public void PlainValue()
        {
            var action = new HackAndSlash()
            {
                costumes = new List<int>(),
                equipments = new List<Guid>(),
                foods = new List<Guid>(),
                worldId = 1,
                stageId = 1,
                avatarAddress = _avatarAddress,
                WeeklyArenaAddress = _weeklyArenaState.address,
                RankingMapAddress = _rankingMapAddress,
            };

            var deserialized = new HackAndSlash();
            deserialized.LoadPlainValue(action.PlainValue);

            Assert.Equal(action.PlainValue, deserialized.PlainValue);
        }
    }
}
