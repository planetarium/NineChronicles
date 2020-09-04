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
            _avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(_sheets[nameof(GameConfigSheet)])
            )
            {
                level = 10,
            };
            _agentState.avatarAddresses.Add(0, _avatarAddress);

            _weeklyArenaState = new WeeklyArenaState(0);

            _initialState = new State()
                .SetState(_weeklyArenaState.address, _weeklyArenaState.Serialize())
                .SetState(_agentAddress, _agentState.Serialize())
                .SetState(_avatarAddress, _avatarState.Serialize());

            foreach (var (key, value) in _sheets)
            {
                _initialState = _initialState
                    .SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }
        }

        [Fact]
        public void Execute()
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
            };

            Assert.Null(action.Result);

            var nextState = action.Execute(new ActionContext()
            {
                PreviousStates = _initialState,
                Signer = _agentAddress,
                Random = new ItemEnhancementTest.TestRandom(),
                Rehearsal = false,
            });

            var nextAvatarState = nextState.GetAvatarState(_avatarAddress);
            var newWeeklyState = nextState.GetWeeklyArenaState(0);

            Assert.NotNull(action.Result);
            Assert.NotEmpty(action.Result.OfType<GetReward>());
            Assert.Equal(BattleLog.Result.Win, action.Result.result);
            Assert.Contains(_avatarAddress, newWeeklyState);
            Assert.True(nextAvatarState.worldInformation.IsStageCleared(1));
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
    }
}
