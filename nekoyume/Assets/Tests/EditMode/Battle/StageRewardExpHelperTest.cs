using Nekoyume.Battle;
using NUnit.Framework;

namespace Tests.EditMode.Battle
{
    public class StageRewardExpHelperTest
    {
        [Test]
        public void GetExp()
        {
            const int stageNumber = 100;
            // 스테이지와 캐릭터 레벨 차가 최저일 때.
            var characterLevel = stageNumber + StageRewardExpHelper.DifferLowerLimit;
            var exp = StageRewardExpHelper.GetExp(characterLevel, stageNumber);
            Assert.AreEqual(StageRewardExpHelper.RewardExpMax, exp);
            characterLevel--;
            exp = StageRewardExpHelper.GetExp(characterLevel, stageNumber);
            Assert.AreEqual(StageRewardExpHelper.RewardExpMax, exp);

            // 스테이지와 캐릭터 레벨 차가 최고일 때.
            characterLevel = stageNumber + StageRewardExpHelper.DifferUpperLimit;
            exp = StageRewardExpHelper.GetExp(characterLevel, stageNumber);
            Assert.AreEqual(StageRewardExpHelper.RewardExpMin, exp);
            characterLevel++;
            exp = StageRewardExpHelper.GetExp(characterLevel, stageNumber);
            Assert.AreEqual(StageRewardExpHelper.RewardExpMin, exp);

            // 스테이지와 캐릭터 레벨 차가 0일 때.
            const int differ = 0;
            Assert.IsTrue(StageRewardExpHelper.CachedExp.ContainsKey(differ));
            characterLevel = stageNumber + differ;
            exp = StageRewardExpHelper.GetExp(characterLevel, stageNumber);
            Assert.AreEqual(StageRewardExpHelper.CachedExp[differ], exp);
        }
    }
}
