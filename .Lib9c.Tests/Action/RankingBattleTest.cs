namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.BattleStatus;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class RankingBattleTest
    {
        private readonly Dictionary<string, string> _sheets;
        private readonly TableSheets _tableSheets;

        public RankingBattleTest()
        {
            _sheets = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(_sheets);
        }

        [Fact]
        public void Execute()
        {
            var itemId = _tableSheets.WeeklyArenaRewardSheet.Values.First().Reward.ItemId;
            var privateKey = new PrivateKey();
            var agentAddress = privateKey.PublicKey.ToAddress();
            var agent = new AgentState(agentAddress);

            var avatarAddress = agentAddress.Derive("avatar");
            var avatarState = new AvatarState(
                avatarAddress,
                agentAddress,
                0,
                _tableSheets.WorldSheet,
                _tableSheets.QuestSheet,
                _tableSheets.QuestRewardSheet,
                _tableSheets.QuestItemRewardSheet,
                _tableSheets.EquipmentItemRecipeSheet,
                _tableSheets.EquipmentItemSubRecipeSheet,
                new GameConfigState()
            )
            {
                level = 10,
            };
            avatarState.worldInformation.ClearStage(
                1,
                GameConfig.RequireClearedStageLevel.ActionsInRankingBoard,
                1,
                _tableSheets.WorldSheet,
                _tableSheets.WorldUnlockSheet
            );
            agent.avatarAddresses.Add(0, avatarAddress);

            Assert.False(avatarState.inventory.HasItem(itemId));

            var avatarAddress2 = agentAddress.Derive("avatar2");
            var avatarState2 = new AvatarState(
                avatarAddress2,
                agentAddress,
                0,
                _tableSheets.WorldSheet,
                _tableSheets.QuestSheet,
                _tableSheets.QuestRewardSheet,
                _tableSheets.QuestItemRewardSheet,
                _tableSheets.EquipmentItemRecipeSheet,
                _tableSheets.EquipmentItemSubRecipeSheet,
                new GameConfigState()
            );
            avatarState2.worldInformation.ClearStage(
                1,
                GameConfig.RequireClearedStageLevel.ActionsInRankingBoard,
                1,
                _tableSheets.WorldSheet,
                _tableSheets.WorldUnlockSheet
            );
            agent.avatarAddresses.Add(1, avatarAddress);

            var weekly = new WeeklyArenaState(0);
            weekly.Set(avatarState, _tableSheets.CharacterSheet);
            weekly[avatarAddress].Activate();
            weekly.Set(avatarState2, _tableSheets.CharacterSheet);
            weekly[avatarAddress2].Activate();

            var state = new State()
                .SetState(weekly.address, weekly.Serialize())
                .SetState(agentAddress, agent.Serialize())
                .SetState(avatarAddress, avatarState.Serialize())
                .SetState(avatarAddress2, avatarState2.Serialize());

            foreach (var (key, value) in _sheets)
            {
                state = state.SetState(Addresses.TableSheet.Derive(key), Dictionary.Empty.Add("csv", value));
            }

            var action = new RankingBattle
            {
                AvatarAddress = avatarAddress,
                EnemyAddress = avatarAddress2,
                WeeklyArenaAddress = weekly.address,
                costumeIds = new List<int>(),
                equipmentIds = new List<Guid>(),
                consumableIds = new List<Guid>(),
            };

            Assert.Null(action.Result);

            var nextState = action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Signer = agentAddress,
                Random = new ItemEnhancementTest.TestRandom(),
                Rehearsal = false,
            });

            var newState = nextState.GetAvatarState(avatarAddress);

            var newWeeklyState = nextState.GetWeeklyArenaState(0);

            Assert.True(newState.inventory.HasItem(itemId));
            Assert.NotNull(action.Result);
            Assert.Contains(typeof(GetReward), action.Result.Select(e => e.GetType()));
            Assert.Equal(BattleLog.Result.Win, action.Result.result);
            Assert.True(newWeeklyState[avatarAddress].Score > weekly[avatarAddress].Score);
        }
    }
}
