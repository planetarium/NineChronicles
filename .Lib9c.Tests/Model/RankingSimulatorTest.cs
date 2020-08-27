namespace Lib9c.Tests.Model
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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
            _tableSheets = TableSheets.FromTableSheetsState(TableSheetsImporter.ImportTableSheets());
            _random = new ItemEnhancementTest.TestRandom();
        }

        [Theory]
        [InlineData(1, 1, true)]
        [InlineData(2, 1, true)]
        [InlineData(1, 2, false)]
        public void Simulate(int level, int requiredLevel, bool expected)
        {
            _tableSheets.SetToSheet(nameof(WeeklyArenaRewardSheet), $"id,item_id,ratio,min,max,required_level\n1,302000,0.1,1,1,{requiredLevel}");
            var avatarState = new AvatarState(default, default, 0, _tableSheets, new GameConfigState())
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

            var simulator = new RankingSimulator(_random, avatarState, avatarState, new List<Guid>(), _tableSheets, 1);
            simulator.Simulate();

            Assert.Equal(expected, simulator.Reward.Any());
        }
    }
}
