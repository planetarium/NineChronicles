namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
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
        private readonly TableSheetsState _tableSheetsState;

        public RankingBattleTest()
        {
            _tableSheetsState = TableSheetsImporter.ImportTableSheets();
        }

        [Fact]
        public void Execute()
        {
            var tableSheets = TableSheets.FromTableSheetsState(_tableSheetsState);
            var itemId = tableSheets.WeeklyArenaRewardSheet.Values.First().Reward.ItemId;
            var privateKey = new PrivateKey();
            var agentAddress = privateKey.PublicKey.ToAddress();
            var agent = new AgentState(agentAddress);

            var avatarAddress = agentAddress.Derive("avatar");
            var avatarState = new AvatarState(avatarAddress, agentAddress, 0, tableSheets, new GameConfigState());
            avatarState.worldInformation.ClearStage(
                1,
                GameConfig.RequireClearedStageLevel.ActionsInRankingBoard,
                1,
                tableSheets.WorldSheet,
                tableSheets.WorldUnlockSheet
            );
            agent.avatarAddresses.Add(0, avatarAddress);

            Assert.False(avatarState.inventory.HasItem(itemId));

            var avatarAddress2 = agentAddress.Derive("avatar2");
            var avatarState2 = new AvatarState(avatarAddress2, agentAddress, 0, tableSheets, new GameConfigState());
            avatarState2.worldInformation.ClearStage(
                1,
                GameConfig.RequireClearedStageLevel.ActionsInRankingBoard,
                1,
                tableSheets.WorldSheet,
                tableSheets.WorldUnlockSheet
            );
            agent.avatarAddresses.Add(1, avatarAddress);

            var weekly = new WeeklyArenaState(0);
            weekly.Set(avatarState, tableSheets.CharacterSheet);
            weekly[avatarAddress].Activate();
            weekly.Set(avatarState2, tableSheets.CharacterSheet);
            weekly[avatarAddress2].Activate();

            var state = new State(ImmutableDictionary<Address, IValue>.Empty
                .Add(_tableSheetsState.address, _tableSheetsState.Serialize())
                .Add(weekly.address, weekly.Serialize())
                .Add(agentAddress, agent.Serialize())
                .Add(avatarAddress, avatarState.Serialize())
                .Add(avatarAddress2, avatarState2.Serialize()));

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

            Assert.True(newState.inventory.HasItem(itemId));
            Assert.NotNull(action.Result);
            Assert.Contains(typeof(GetReward), action.Result.Select(e => e.GetType()));
        }
    }
}
