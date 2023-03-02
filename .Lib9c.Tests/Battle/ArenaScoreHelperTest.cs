namespace Lib9c.Tests
{
    using System;
    using Nekoyume.Battle;
    using Xunit;
    using Result = Nekoyume.Model.BattleStatus.BattleLog.Result;

    public class ArenaScoreHelperTest
    {
        [Fact]
        public void GetScoreV1()
        {
            const int challengerRating = 10000;
            var defenderRating = challengerRating + Math.Abs(ArenaScoreHelper.DifferLowerLimit);
            var score = ArenaScoreHelper.GetScoreV1(challengerRating, defenderRating, Result.Win);
            Assert.Equal(ArenaScoreHelper.WinScoreMax, score);
            score = ArenaScoreHelper.GetScoreV1(challengerRating, defenderRating, Result.Lose);
            Assert.Equal(ArenaScoreHelper.LoseScoreMin, score);
            defenderRating++;
            score = ArenaScoreHelper.GetScoreV1(challengerRating, defenderRating, Result.Win);
            Assert.Equal(ArenaScoreHelper.WinScoreMax, score);
            score = ArenaScoreHelper.GetScoreV1(challengerRating, defenderRating, Result.Lose);
            Assert.Equal(ArenaScoreHelper.LoseScoreMin, score);

            defenderRating = challengerRating - Math.Abs(ArenaScoreHelper.DifferUpperLimit);
            score = ArenaScoreHelper.GetScoreV1(challengerRating, defenderRating, Result.Win);
            Assert.Equal(ArenaScoreHelper.WinScoreMin, score);
            score = ArenaScoreHelper.GetScoreV1(challengerRating, defenderRating, Result.Lose);
            Assert.Equal(ArenaScoreHelper.LoseScoreMax, score);
            defenderRating--;
            score = ArenaScoreHelper.GetScoreV1(challengerRating, defenderRating, Result.Win);
            Assert.Equal(ArenaScoreHelper.WinScoreMin, score);
            score = ArenaScoreHelper.GetScoreV1(challengerRating, defenderRating, Result.Lose);
            Assert.Equal(ArenaScoreHelper.LoseScoreMax, score);
        }

        [Theory]
        [InlineData(1000, int.MaxValue, 60, 0, -5)]
        [InlineData(1000, 1501, 60, 0, -5)]
        [InlineData(1000, 1401, 50, 0, -5)]
        [InlineData(1000, 1301, 40, 0, -6)]
        [InlineData(1000, 1201, 30, 0, -6)]
        [InlineData(1000, 1101, 25, 0, -8)]
        [InlineData(1000, 1001, 20, 0, -8)]
        [InlineData(1000, 1000, 15, 0, -10)]
        [InlineData(1000, 900, 15, 0, -10)]
        [InlineData(1000, 800, 8, 0, -20)]
        [InlineData(1000, 700, 4, 0, -25)]
        [InlineData(1000, 600, 2, 0, -30)]
        [InlineData(1000, 500, 1, 0, -30)]
        [InlineData(1000, 0, 1, 0, -30)]
        [InlineData(1000, int.MinValue, 0, 0, 0)]
        public void GetScoreV2(int challengerScore, int defenderScore, int winScore, int timeOver, int loseScore)
        {
            Assert.Equal(winScore, ArenaScoreHelper.GetScoreV2(challengerScore, defenderScore, Result.Win));
            Assert.Equal(timeOver, ArenaScoreHelper.GetScoreV2(challengerScore, defenderScore, Result.TimeOver));
            Assert.Equal(loseScore, ArenaScoreHelper.GetScoreV2(challengerScore, defenderScore, Result.Lose));
        }

        [Theory]
        [InlineData(1000, int.MaxValue, 60, -2, -5)]
        [InlineData(1000, 1501, 60, -2, -5)]
        [InlineData(1000, 1401, 45, -2, -5)]
        [InlineData(1000, 1301, 35, -2, -3)]
        [InlineData(1000, 1201, 25, -1, -2)]
        [InlineData(1000, 1101, 22, -1, -2)]
        [InlineData(1000, 1001, 20, -1, -1)]
        [InlineData(1000, 1000, 15, -1, -2)]
        [InlineData(1000, 900, 10, 0, -2)]
        [InlineData(1000, 800, 8, 0, -5)]
        [InlineData(1000, 700, 4, 0, -5)]
        [InlineData(1000, 600, 2, 0, -5)]
        [InlineData(1000, 500, 1, 0, -5)]
        [InlineData(1000, 0, 1, 0, -5)]
        [InlineData(1000, int.MinValue, 0, 0, 0)]
        public void GetScoreV3(
            int challengerScore,
            int defenderScore,
            int winScore,
            int defenderLoseScore,
            int loseScore)
        {
            var result = ArenaScoreHelper.GetScoreV3(challengerScore, defenderScore, Result.Win);
            Assert.Equal(winScore, result.challengerScore);
            Assert.Equal(defenderLoseScore, result.defenderScore);
            result = ArenaScoreHelper.GetScoreV3(challengerScore, defenderScore, Result.Lose);
            Assert.Equal(loseScore, result.challengerScore);
        }

        [Theory]
        [InlineData(1000, int.MaxValue, 24, -3, -1)]
        [InlineData(1000, 1201, 24, -3, -1)]
        [InlineData(1000, 1101, 22, -2, -1)]
        [InlineData(1000, 1001, 20, -1, -1)]
        [InlineData(1000, 1000, 18, -1, -1)]
        [InlineData(1000, 901, 18, -1, -1)]
        [InlineData(1000, 801, 16, -1, -1)]
        [InlineData(1000, 800, 1, -1, 0)]
        [InlineData(int.MaxValue, 0, 1, -1, 0)]
        [InlineData(1000, int.MinValue, 0, 0, 0)]
        [InlineData(int.MinValue, 1000, 0, 0, 0)]
        [InlineData(int.MinValue, int.MinValue, 0, 0, 0)]
        public void GetScoreV4(int challengerScore, int defenderScore, int winPoint, int losePoint, int defenderLosePoint)
        {
            var (challengerScoreDelta, defenderScoreDelta) =
                ArenaScoreHelper.GetScoreV4(challengerScore, defenderScore, Result.Win);
            Assert.Equal(winPoint, challengerScoreDelta);
            Assert.Equal(defenderLosePoint, defenderScoreDelta);
            (challengerScoreDelta, _) = ArenaScoreHelper.GetScoreV4(challengerScore, defenderScore, Result.Lose);
            Assert.Equal(losePoint, challengerScoreDelta);
        }

        [Theory]
        [InlineData(1000, int.MaxValue, 20, -1, -1)]
        [InlineData(1000, 1201, 20, -1, -1)]
        [InlineData(1000, 1101, 20, -1, -1)]
        [InlineData(1000, 1001, 20, -1, -1)]
        [InlineData(1000, 1000, 18, -1, -1)]
        [InlineData(1000, 901, 18, -1, -1)]
        [InlineData(1000, 801, 16, -1, -1)]
        [InlineData(1000, 800, 1, -1, 0)]
        [InlineData(int.MaxValue, 0, 1, -1, 0)]
        [InlineData(1000, int.MinValue, 0, 0, 0)]
        [InlineData(int.MinValue, 1000, 0, 0, 0)]
        [InlineData(int.MinValue, int.MinValue, 0, 0, 0)]
        public void GetScore(int challengerScore, int defenderScore, int winPoint, int losePoint, int defenderLosePoint)
        {
            var (challengerScoreDelta, defenderScoreDelta) =
                ArenaScoreHelper.GetScore(challengerScore, defenderScore, Result.Win);
            Assert.Equal(winPoint, challengerScoreDelta);
            Assert.Equal(defenderLosePoint, defenderScoreDelta);
            (challengerScoreDelta, _) = ArenaScoreHelper.GetScore(challengerScore, defenderScore, Result.Lose);
            Assert.Equal(losePoint, challengerScoreDelta);
        }
    }
}
