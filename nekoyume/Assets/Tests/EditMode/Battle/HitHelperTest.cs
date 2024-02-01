using System;
using Nekoyume.Battle;
using NUnit.Framework;

namespace Tests.EditMode.Battle
{
    public class HitHelperTest
    {
        [Test]
        public void IsHit()
        {
            var attackerLevel = 1;
            var attackerHit = 100;
            var defenderLevel = 1;
            var defenderHit = 100;
            var chance = 100;
            var correction = HitHelper.GetHitStep1(attackerLevel, defenderLevel);
            correction += HitHelper.GetHitStep2(attackerHit, defenderHit);
            correction = HitHelper.GetHitStep3(correction);
            var hit = HitHelper.GetHitStep4(chance, correction);
            Assert.AreEqual(hit, HitHelper.IsHit(attackerLevel, attackerHit, defenderLevel, defenderHit, chance));
        }

        [Test]
        public void GetHitStep1()
        {
            var attackerLevel = 1L;
            var defenderLevel = 1 + Math.Abs(HitHelper.GetHitStep1LevelDiffMin);
            Assert.AreEqual(HitHelper.GetHitStep1CorrectionMin, HitHelper.GetHitStep1(attackerLevel, defenderLevel));
            attackerLevel = 1 + Math.Abs(HitHelper.GetHitStep1LevelDiffMax);
            defenderLevel = 1;
            Assert.AreEqual(HitHelper.GetHitStep1CorrectionMax, HitHelper.GetHitStep1(attackerLevel, defenderLevel));
        }

        [Test]
        public void GetHitStep2()
        {
            var attackerHit = 1;
            var defenderHit = 100;
            var correction = HitHelper.GetHitStep2(attackerHit, defenderHit);
            Assert.AreEqual(HitHelper.GetHitStep2AdditionalCorrectionMin, correction);
            attackerHit = 100;
            defenderHit = 1;
            correction = HitHelper.GetHitStep2(attackerHit, defenderHit);
            Assert.AreEqual(HitHelper.GetHitStep2AdditionalCorrectionMax, correction);
        }

        [Test]
        public void GetHitStep3()
        {
            var correction = 1L;
            correction = HitHelper.GetHitStep3(correction);
            Assert.AreEqual(HitHelper.GetHitStep3CorrectionMin, correction);
            correction = 100;
            correction = HitHelper.GetHitStep3(correction);
            Assert.AreEqual(HitHelper.GetHitStep3CorrectionMax, correction);
        }

        [Test]
        public void GetHitStep4()
        {
            var correction = 1;
            var lowLimitChance = 100;
            Assert.IsFalse(HitHelper.GetHitStep4(lowLimitChance, correction));
            correction = 100;
            lowLimitChance = 1;
            Assert.IsTrue(HitHelper.GetHitStep4(lowLimitChance, correction));
        }
    }
}
