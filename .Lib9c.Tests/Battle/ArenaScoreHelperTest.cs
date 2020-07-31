namespace Lib9c.Tests
{
    using System;
    using Nekoyume.Battle;
    using Nekoyume.Model.BattleStatus;
    using Xunit;

    public class ArenaScoreHelperTest
    {
        [Fact]
        public void GetScore()
        {
            var challengerRating = 10000;
            var defenderRating = challengerRating + Math.Abs(ArenaScoreHelper.DifferLowerLimit);
            var score = ArenaScoreHelper.GetScore(challengerRating, defenderRating, BattleLog.Result.Win);
            Assert.Equal(ArenaScoreHelper.WinScoreMax, score);
            score = ArenaScoreHelper.GetScore(challengerRating, defenderRating, BattleLog.Result.Lose);
            Assert.Equal(ArenaScoreHelper.LoseScoreMin, score);
            defenderRating++;
            score = ArenaScoreHelper.GetScore(challengerRating, defenderRating, BattleLog.Result.Win);
            Assert.Equal(ArenaScoreHelper.WinScoreMax, score);
            score = ArenaScoreHelper.GetScore(challengerRating, defenderRating, BattleLog.Result.Lose);
            Assert.Equal(ArenaScoreHelper.LoseScoreMin, score);

            defenderRating = challengerRating - Math.Abs(ArenaScoreHelper.DifferUpperLimit);
            score = ArenaScoreHelper.GetScore(challengerRating, defenderRating, BattleLog.Result.Win);
            Assert.Equal(ArenaScoreHelper.WinScoreMin, score);
            score = ArenaScoreHelper.GetScore(challengerRating, defenderRating, BattleLog.Result.Lose);
            Assert.Equal(ArenaScoreHelper.LoseScoreMax, score);
            defenderRating--;
            score = ArenaScoreHelper.GetScore(challengerRating, defenderRating, BattleLog.Result.Win);
            Assert.Equal(ArenaScoreHelper.WinScoreMin, score);
            score = ArenaScoreHelper.GetScore(challengerRating, defenderRating, BattleLog.Result.Lose);
            Assert.Equal(ArenaScoreHelper.LoseScoreMax, score);
        }
    }
}
