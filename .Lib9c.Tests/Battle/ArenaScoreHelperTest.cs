namespace Lib9c.Tests
{
    using System;
    using Nekoyume.Battle;
    using Nekoyume.Model.BattleStatus;
    using Xunit;

    public class ArenaScoreHelperTest
    {
        [Fact]
        public void GetScoreV1()
        {
            var challengerRating = 10000;
            var defenderRating = challengerRating + Math.Abs(ArenaScoreHelper.DifferLowerLimit);
            var score = ArenaScoreHelper.GetScoreV1(challengerRating, defenderRating, BattleLog.Result.Win);
            Assert.Equal(ArenaScoreHelper.WinScoreMax, score);
            score = ArenaScoreHelper.GetScoreV1(challengerRating, defenderRating, BattleLog.Result.Lose);
            Assert.Equal(ArenaScoreHelper.LoseScoreMin, score);
            defenderRating++;
            score = ArenaScoreHelper.GetScoreV1(challengerRating, defenderRating, BattleLog.Result.Win);
            Assert.Equal(ArenaScoreHelper.WinScoreMax, score);
            score = ArenaScoreHelper.GetScoreV1(challengerRating, defenderRating, BattleLog.Result.Lose);
            Assert.Equal(ArenaScoreHelper.LoseScoreMin, score);

            defenderRating = challengerRating - Math.Abs(ArenaScoreHelper.DifferUpperLimit);
            score = ArenaScoreHelper.GetScoreV1(challengerRating, defenderRating, BattleLog.Result.Win);
            Assert.Equal(ArenaScoreHelper.WinScoreMin, score);
            score = ArenaScoreHelper.GetScoreV1(challengerRating, defenderRating, BattleLog.Result.Lose);
            Assert.Equal(ArenaScoreHelper.LoseScoreMax, score);
            defenderRating--;
            score = ArenaScoreHelper.GetScoreV1(challengerRating, defenderRating, BattleLog.Result.Win);
            Assert.Equal(ArenaScoreHelper.WinScoreMin, score);
            score = ArenaScoreHelper.GetScoreV1(challengerRating, defenderRating, BattleLog.Result.Lose);
            Assert.Equal(ArenaScoreHelper.LoseScoreMax, score);
        }
    }
}
