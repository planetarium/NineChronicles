using System;
using Nekoyume.Battle;
using Nekoyume.Model.BattleStatus;
using NUnit.Framework;

namespace Tests.EditMode.Battle
{
    public class ArenaScoreHelperTest
    {
        [Test]
        public void GetScore()
        {
            var challengerRating = 10000;
            var defenderRating = challengerRating + Math.Abs(ArenaScoreHelper.DifferLowerLimit);
            var score = ArenaScoreHelper.GetScore(challengerRating, defenderRating, BattleLog.Result.Win);
            Assert.AreEqual(ArenaScoreHelper.WinScoreMax, score);
            score = ArenaScoreHelper.GetScore(challengerRating, defenderRating, BattleLog.Result.Lose);
            Assert.AreEqual(ArenaScoreHelper.LoseScoreMin, score);
            defenderRating++;
            score = ArenaScoreHelper.GetScore(challengerRating, defenderRating, BattleLog.Result.Win);
            Assert.AreEqual(ArenaScoreHelper.WinScoreMax, score);
            score = ArenaScoreHelper.GetScore(challengerRating, defenderRating, BattleLog.Result.Lose);
            Assert.AreEqual(ArenaScoreHelper.LoseScoreMin, score);

            defenderRating = challengerRating - Math.Abs(ArenaScoreHelper.DifferUpperLimit);
            score = ArenaScoreHelper.GetScore(challengerRating, defenderRating, BattleLog.Result.Win);
            Assert.AreEqual(ArenaScoreHelper.WinScoreMin, score);
            score = ArenaScoreHelper.GetScore(challengerRating, defenderRating, BattleLog.Result.Lose);
            Assert.AreEqual(ArenaScoreHelper.LoseScoreMax, score);
            defenderRating--;
            score = ArenaScoreHelper.GetScore(challengerRating, defenderRating, BattleLog.Result.Win);
            Assert.AreEqual(ArenaScoreHelper.WinScoreMin, score);
            score = ArenaScoreHelper.GetScore(challengerRating, defenderRating, BattleLog.Result.Lose);
            Assert.AreEqual(ArenaScoreHelper.LoseScoreMax, score);
        }
    }
}
