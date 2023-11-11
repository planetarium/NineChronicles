namespace Lib9c.Tests.Action
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Helper;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;
    using static Lib9c.SerializeKeys;

    public class DailyReward6Test
    {
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly IAccount _initialState;

        public DailyReward6Test(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();

            _initialState = new Account(MockState.Empty);
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

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        public void Execute(int avatarStateSerializedVersion)
        {
            IAccount previousStates = null;
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

            var nextState = ExecuteInternal(previousStates, 2040);
            var nextGameConfigState = nextState.GetGameConfigState();
            var nextAvatarState = nextState.GetAvatarStateV2(_avatarAddress);
            Assert.NotNull(nextAvatarState);
            Assert.NotNull(nextAvatarState.inventory);
            Assert.NotNull(nextAvatarState.questList);
            Assert.NotNull(nextAvatarState.worldInformation);
            Assert.Equal(nextGameConfigState.ActionPointMax, nextAvatarState.actionPoint);

            var avatarRuneAmount = nextState.GetBalance(_avatarAddress, RuneHelper.DailyRewardRune);
            var expectedRune = RuneHelper.DailyRewardRune * nextGameConfigState.DailyRuneRewardAmount;
            Assert.Equal(expectedRune, avatarRuneAmount);
        }

        [Fact]
        public void Execute_Throw_FailedLoadStateException() =>
            Assert.Throws<FailedLoadStateException>(() => ExecuteInternal(new Account(MockState.Empty)));

        [Theory]
        [InlineData(0, 0, true)]
        [InlineData(0, 2039, true)]
        [InlineData(0, 2040, false)]
        [InlineData(2040, 2040, true)]
        [InlineData(2040, 2040 + 2039, true)]
        [InlineData(2040, 2040 + 2040, false)]
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

        [Fact]
        private void Execute_Without_Runereward()
        {
            var gameConfigSheet = new GameConfigSheet();
            var csv = @"key,value
hourglass_per_block,3
action_point_max,120
daily_reward_interval,1
daily_arena_interval,5040
weekly_arena_interval,56000
required_appraise_block,10
battle_arena_interval,4
rune_stat_slot_unlock_cost,50
rune_skill_slot_unlock_cost,500";
            gameConfigSheet.Set(csv);
            var gameConfigState = new GameConfigState();
            gameConfigState.Set(gameConfigSheet);

            var state = _initialState
                .SetState(Addresses.GameConfig, gameConfigState.Serialize());
            var nextState = ExecuteInternal(state, 1800);
            var avatarRuneAmount = nextState.GetBalance(_avatarAddress, RuneHelper.DailyRewardRune);
            Assert.Equal(0, (int)avatarRuneAmount.MajorUnit);
        }

        private IAccount SetAvatarStateAsV2To(IAccount state, AvatarState avatarState) =>
            state
                .SetState(_avatarAddress.Derive(LegacyInventoryKey), avatarState.inventory.Serialize())
                .SetState(_avatarAddress.Derive(LegacyWorldInformationKey), avatarState.worldInformation.Serialize())
                .SetState(_avatarAddress.Derive(LegacyQuestListKey), avatarState.questList.Serialize())
                .SetState(_avatarAddress, avatarState.SerializeV2());

        private IAccount ExecuteInternal(IAccount previousStates, long blockIndex = 0)
        {
            var dailyRewardAction = new DailyReward6
            {
                avatarAddress = _avatarAddress,
            };

            return dailyRewardAction.Execute(new ActionContext
            {
                BlockIndex = blockIndex,
                PreviousState = previousStates,
                RandomSeed = 0,
                Rehearsal = false,
                Signer = _agentAddress,
            });
        }
    }
}
