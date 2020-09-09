namespace Lib9c.Tests.Model
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Lib9c.Tests.Action;
    using Libplanet.Action;
    using Nekoyume;
    using Nekoyume.Battle;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class RankingSimulatorTest
    {
        private readonly TableSheets _tableSheets;
        private readonly IRandom _random;

        public RankingSimulatorTest()
        {
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            _random = new ItemEnhancementTest.TestRandom();
        }

        [Theory]
        [InlineData(1, 1, true)]
        [InlineData(2, 1, true)]
        [InlineData(1, 2, false)]
        public void SimulateRequiredLevel(int level, int requiredLevel, bool expected)
        {
            var rewardSheet = new WeeklyArenaRewardSheet();
            rewardSheet.Set($"id,item_id,ratio,min,max,required_level\n1,302000,0.1,1,1,{requiredLevel}");
            _tableSheets.WeeklyArenaRewardSheet = rewardSheet;
            var avatarState = new AvatarState(
                default,
                default,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                default
            )
            {
                level = level,
            };
            avatarState.worldInformation.ClearStage(
                1,
                GameConfig.RequireClearedStageLevel.ActionsInRankingBoard,
                1,
                _tableSheets.WorldSheet,
                _tableSheets.WorldUnlockSheet
            );

            var simulator = new RankingSimulator(
                _random,
                avatarState,
                avatarState,
                new List<Guid>(),
                _tableSheets.GetRankingSimulatorSheets(),
                1,
                new ArenaInfo(avatarState, _tableSheets.CharacterSheet, false),
                new ArenaInfo(avatarState, _tableSheets.CharacterSheet, false)
            );
            simulator.Simulate();

            Assert.Equal(expected, simulator.Reward.Any());
        }

        [Theory]
        [InlineData(900, 1)]
        [InlineData(1030, 2)]
        [InlineData(1150, 3)]
        [InlineData(1370, 4)]
        [InlineData(1600, 5)]
        [InlineData(1900, 6)]
        public void SimulateRankingScore(int score, int expected)
        {
            var avatarState = new AvatarState(
                default,
                default,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                default
            );
            avatarState.worldInformation.ClearStage(
                1,
                GameConfig.RequireClearedStageLevel.ActionsInRankingBoard,
                1,
                _tableSheets.WorldSheet,
                _tableSheets.WorldUnlockSheet
            );

            var serialized = (Dictionary)new ArenaInfo(avatarState, _tableSheets.CharacterSheet, false).Serialize();
            serialized = serialized.SetItem("score", score.Serialize());
            var info = new ArenaInfo(serialized);

            var simulator = new RankingSimulator(
                _random,
                avatarState,
                avatarState,
                new List<Guid>(),
                _tableSheets.GetRankingSimulatorSheets(),
                1,
                info,
                new ArenaInfo(avatarState, _tableSheets.CharacterSheet, false)
            );
            simulator.Simulate();

            Assert.Equal(expected, simulator.Reward.Count());
        }
    }
}
