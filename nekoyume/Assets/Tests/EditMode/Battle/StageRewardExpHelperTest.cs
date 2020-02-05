using Nekoyume.Battle;
using NUnit.Framework;

namespace Tests.EditMode.Battle
{
    public class StageRewardExpHelperTest
    {
        [Test]
        public void GetExp()
        {
            var stageNumber = 100;
            var characterLevel = stageNumber + StageRewardExpHelper.DifferLowerLimit;
            var exp = StageRewardExpHelper.GetExp(characterLevel, stageNumber);
            Assert.AreEqual(StageRewardExpHelper.RewardExpMax, exp);
            characterLevel--;
            exp = StageRewardExpHelper.GetExp(characterLevel, stageNumber);
            Assert.AreEqual(StageRewardExpHelper.RewardExpMax, exp);
            
            characterLevel = stageNumber + StageRewardExpHelper.DifferUpperLimit;
            exp = StageRewardExpHelper.GetExp(characterLevel, stageNumber);
            Assert.AreEqual(StageRewardExpHelper.RewardExpMin, exp);
            characterLevel++;
            exp = StageRewardExpHelper.GetExp(characterLevel, stageNumber);
            Assert.AreEqual(StageRewardExpHelper.RewardExpMin, exp);
            
            var differ = 0;
            Assert.IsTrue(StageRewardExpHelper.CachedExp.ContainsKey(differ));
            characterLevel = stageNumber + differ;
            exp = StageRewardExpHelper.GetExp(characterLevel, stageNumber);
            Assert.AreEqual(StageRewardExpHelper.CachedExp[differ], exp);
        }
    }
}
