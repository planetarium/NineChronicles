namespace Lib9c.Tests.Action
{
    using System.Collections.Generic;
    using System.Linq;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;

    public class QuestRewardTest
    {
        private readonly IAccountStateDelta _initialState;
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;

        public QuestRewardTest(ITestOutputHelper outputHelper)
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
            var worldQuestRow = _initialState.GetSheet<WorldQuestSheet>().First;
            Assert.NotNull(worldQuestRow);

            var avatarState = _initialState.GetAvatarState(_avatarAddress);
            var stageMap = new CollectionMap
            {
                new KeyValuePair<int, int>(worldQuestRow.Goal, 1),
            };
            avatarState.questList.UpdateStageQuest(stageMap);

            var tempState = _initialState.SetState(_avatarAddress, avatarState.Serialize());
            var questRewardAction = new QuestReward
            {
                avatarAddress = _avatarAddress,
                questId = worldQuestRow.Id,
            };
            tempState = questRewardAction.Execute(new ActionContext
            {
                BlockIndex = 0,
                PreviousStates = tempState,
                Rehearsal = false,
                Signer = _agentAddress,
            });

            var questRewardSheet = tempState.GetSheet<QuestRewardSheet>();
            Assert.True(questRewardSheet.TryGetValue(
                worldQuestRow.QuestRewardId,
                out var worldQuestRewardRow));
            var nextAvatarState = tempState.GetAvatarState(_avatarAddress);
            var questItemRewardSheet = tempState.GetSheet<QuestItemRewardSheet>();
            foreach (var rewardId in worldQuestRewardRow.RewardIds.OrderBy(id => id))
            {
                Assert.True(questItemRewardSheet.TryGetValue(rewardId, out var questItemRewardRow));
                nextAvatarState.inventory.HasItem(questItemRewardRow.Id, questItemRewardRow.Count);
            }
        }
    }
}
