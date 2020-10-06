namespace Lib9c.Tests.Action
{
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;

    public class DeleteAvatarTest
    {
        private readonly IAccountStateDelta _initialState;
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;

        public DeleteAvatarTest(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();

            _initialState = new State();
            var sheets = TableSheetsImporter.ImportSheets();
            foreach (var (key, value) in sheets)
            {
                _initialState = _initialState
                    .SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            var tableSheets = new TableSheets(sheets);

            _agentAddress = new PrivateKey().ToAddress();
            var agentState = new AgentState(_agentAddress);
            _avatarAddress = new PrivateKey().ToAddress();
            var rankingMapAddress = new PrivateKey().ToAddress();
            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                rankingMapAddress);
            agentState.avatarAddresses[0] = _avatarAddress;

            _initialState = _initialState
                .SetState(Addresses.GameConfig, new GameConfigState().Serialize())
                .SetState(_agentAddress, agentState.Serialize())
                .SetState(_avatarAddress, avatarState.Serialize());
        }

        [Fact]
        public void Execute()
        {
            var deleteAvatarAction = new DeleteAvatar
            {
                avatarAddress = _avatarAddress,
                index = 0,
            };
            var actionContext = new ActionContext
            {
                BlockIndex = 0,
                PreviousStates = _initialState,
                Rehearsal = false,
                Signer = _agentAddress,
            };
            var nextState = deleteAvatarAction.Execute(actionContext);

            var nextAgentState = nextState.GetAgentState(_agentAddress);
            Assert.Empty(nextAgentState.avatarAddresses);

            var nextAvatarValue = nextState.GetState(_avatarAddress);
            Assert.NotNull(nextAvatarValue);

            var nextDeletedAvatarState =
                new DeletedAvatarState((Bencodex.Types.Dictionary)nextAvatarValue);
            Assert.Equal(actionContext.BlockIndex, nextDeletedAvatarState.deletedAt);
        }
    }
}
