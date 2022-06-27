namespace Lib9c.Tests.Action
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;
    using static Lib9c.SerializeKeys;

    public class DailyReward5Test
    {
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly IAccountStateDelta _initialState;

        public DailyReward5Test(ITestOutputHelper outputHelper)
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
            var gameConfigState = new GameConfigState();
            gameConfigState.Set(tableSheets.GameConfigSheet);
            _agentAddress = new PrivateKey().ToAddress();
            var agentState = new AgentState(_agentAddress);
            _avatarAddress = new PrivateKey().ToAddress();
            var rankingMapAddress = new PrivateKey().ToAddress();
            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                tableSheets.GetAvatarSheets(),
                gameConfigState,
                rankingMapAddress)
            {
                actionPoint = 0,
            };
            agentState.avatarAddresses[0] = _avatarAddress;

            _initialState = _initialState
                .SetState(Addresses.GameConfig, gameConfigState.Serialize())
                .SetState(_agentAddress, agentState.Serialize())
                .SetState(_avatarAddress, avatarState.Serialize());
        }

        [Fact]
        public void Rehearsal()
        {
            var action = new DailyReward5
            {
                avatarAddress = _avatarAddress,
            };

            var nextState = action.Execute(new ActionContext
            {
                BlockIndex = 0,
                PreviousStates = new State(),
                Random = new TestRandom(),
                Rehearsal = true,
                Signer = _agentAddress,
            });

            var updatedAddresses = new List<Address>
            {
                _avatarAddress,
            };

            Assert.Equal(updatedAddresses.ToImmutableHashSet(), nextState.UpdatedAddresses);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        public void Execute(int avatarStateSerializedVersion)
        {
            IAccountStateDelta previousStates = null;
            switch (avatarStateSerializedVersion)
            {
                case 1:
                    previousStates = _initialState;
                    break;
                case 2:
                    var avatarState = _initialState.GetAvatarState(_avatarAddress);
                    previousStates = SetAvatarStateAsV2To(_initialState, avatarState);
                    break;
            }

            var nextState = ExecuteInternal(previousStates, 1800);
            var nextGameConfigState = nextState.GetGameConfigState();
            var nextAvatarState = avatarStateSerializedVersion switch
            {
                1 => nextState.GetAvatarState(_avatarAddress),
                2 => nextState.GetAvatarStateV2(_avatarAddress),
                _ => null,
            };
            if (nextAvatarState is null)
            {
                return;
            }

            Assert.Equal(nextGameConfigState.ActionPointMax, nextAvatarState.actionPoint);
        }

        [Fact]
        public void Execute_Throw_FailedLoadStateException() =>
            Assert.Throws<FailedLoadStateException>(() => ExecuteInternal(new State()));

        [Theory]
        [InlineData(0, 0, true)]
        [InlineData(0, 1799, true)]
        [InlineData(0, 1800, false)]
        [InlineData(1800, 1800, true)]
        [InlineData(1800, 1800 + 1799, true)]
        [InlineData(1800, 1800 + 1800, false)]
        public void Execute_Throw_RequiredBlockIndexException(
            long dailyRewardReceivedIndex,
            long executeBlockIndex,
            bool throwsException)
        {
            var avatarState = _initialState.GetAvatarState(_avatarAddress);
            avatarState.dailyRewardReceivedIndex = dailyRewardReceivedIndex;
            var previousStates = SetAvatarStateAsV2To(_initialState, avatarState);
            try
            {
                ExecuteInternal(previousStates, executeBlockIndex);
            }
            catch (RequiredBlockIndexException)
            {
                Assert.True(throwsException);
            }
        }

        private IAccountStateDelta SetAvatarStateAsV2To(IAccountStateDelta state, AvatarState avatarState) =>
            state
                .SetState(_avatarAddress.Derive(LegacyInventoryKey), avatarState.inventory.Serialize())
                .SetState(_avatarAddress.Derive(LegacyWorldInformationKey), avatarState.worldInformation.Serialize())
                .SetState(_avatarAddress.Derive(LegacyQuestListKey), avatarState.questList.Serialize())
                .SetState(_avatarAddress, avatarState.SerializeV2());

        private IAccountStateDelta ExecuteInternal(IAccountStateDelta previousStates, long blockIndex = 0)
        {
            var dailyRewardAction = new DailyReward5
            {
                avatarAddress = _avatarAddress,
            };

            return dailyRewardAction.Execute(new ActionContext
            {
                BlockIndex = blockIndex,
                PreviousStates = previousStates,
                Random = new TestRandom(),
                Rehearsal = false,
                Signer = _agentAddress,
            });
        }
    }
}
