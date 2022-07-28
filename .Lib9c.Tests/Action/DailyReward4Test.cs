namespace Lib9c.Tests.Action
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.Mail;
    using Nekoyume.Model.State;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;
    using static Lib9c.SerializeKeys;

    public class DailyReward4Test
    {
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private IAccountStateDelta _initialState;

        public DailyReward4Test(ITestOutputHelper outputHelper)
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
                rankingMapAddress)
            {
                actionPoint = 0,
            };
            agentState.avatarAddresses[0] = _avatarAddress;

            _initialState = _initialState
                .SetState(Addresses.GameConfig, new GameConfigState().Serialize())
                .SetState(_agentAddress, agentState.Serialize())
                .SetState(_avatarAddress, avatarState.Serialize());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Execute(bool backward)
        {
            var dailyRewardAction = new DailyReward4
            {
                avatarAddress = _avatarAddress,
            };
            if (!backward)
            {
                AvatarState avatarState = _initialState.GetAvatarState(_avatarAddress);
                _initialState = _initialState
                    .SetState(_avatarAddress.Derive(LegacyInventoryKey), avatarState.inventory.Serialize())
                    .SetState(_avatarAddress.Derive(LegacyWorldInformationKey), avatarState.worldInformation.Serialize())
                    .SetState(_avatarAddress.Derive(LegacyQuestListKey), avatarState.questList.Serialize())
                    .SetState(_avatarAddress, avatarState.SerializeV2());
            }

            var nextState = dailyRewardAction.Execute(new ActionContext
            {
                BlockIndex = 0,
                PreviousStates = _initialState,
                Random = new TestRandom(),
                Rehearsal = false,
                Signer = _agentAddress,
            });

            var gameConfigState = nextState.GetGameConfigState();
            var nextAvatarState = nextState.GetAvatarStateV2(_avatarAddress);
            Assert.Equal(gameConfigState.ActionPointMax, nextAvatarState.actionPoint);
            Assert.Single(nextAvatarState.mailBox);
            var mail = nextAvatarState.mailBox.First();
            var rewardMail = mail as DailyRewardMail;
            Assert.NotNull(rewardMail);
            var rewardResult = rewardMail.attachment as DailyReward2.DailyRewardResult;
            Assert.NotNull(rewardResult);
            Assert.Single(rewardResult.materials);
            var material = rewardResult.materials.First();
            Assert.Equal(400000, material.Key.Id);
            Assert.Equal(10, material.Value);
        }

        [Fact]
        public void ExecuteThrowFailedLoadStateException()
        {
            var action = new DailyReward4
            {
                avatarAddress = _avatarAddress,
            };

            Assert.Throws<FailedLoadStateException>(() => action.Execute(new ActionContext()
                {
                    PreviousStates = new State(),
                    Signer = _agentAddress,
                    BlockIndex = 0,
                })
            );
        }

        [Fact]
        public void Rehearsal()
        {
            var action = new DailyReward4
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

            var updatedAddresses = new List<Address>()
            {
                _avatarAddress,
                _avatarAddress.Derive(LegacyInventoryKey),
                _avatarAddress.Derive(LegacyWorldInformationKey),
                _avatarAddress.Derive(LegacyQuestListKey),
            };

            Assert.Equal(updatedAddresses.ToImmutableHashSet(), nextState.UpdatedAddresses);
        }
    }
}
