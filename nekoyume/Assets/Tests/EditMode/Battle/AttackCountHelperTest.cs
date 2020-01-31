using System;
using Nekoyume.Battle;
using NUnit.Framework;

namespace Tests.EditMode.Battle
{
    public class AttackCountHelperTest
    {
        [Test]
        public void GetCountMax()
        {
            var level = 1;
            Assert.AreEqual(AttackCountHelper.CountMaxLowerLimit, AttackCountHelper.GetCountMax(level));
            level = 20;
            Assert.AreEqual(AttackCountHelper.CountMaxLowerLimit + 1, AttackCountHelper.GetCountMax(level));
            level = 100;
            Assert.AreEqual(AttackCountHelper.CountMaxLowerLimit + 2, AttackCountHelper.GetCountMax(level));
            level = 250;
            Assert.AreEqual(AttackCountHelper.CountMaxLowerLimit + 3, AttackCountHelper.GetCountMax(level));
            level = 999;
            Assert.AreEqual(AttackCountHelper.CountMaxUpperLimit, AttackCountHelper.GetCountMax(level));
        }

        [Test]
        public void GetDamageMultiplier()
        {
            for (var i = 0; i < 2; i++)
            {
                var level = i == 0
                    ? 1
                    : 999;
                var attackCount = 0;
                var attackCountMax = AttackCountHelper.GetCountMax(level);

                switch (level)
                {
                    case 1:
                        Assert.AreEqual(AttackCountHelper.CountMaxLowerLimit, attackCountMax);
                        break;
                    case 999:
                        Assert.AreEqual(AttackCountHelper.CountMaxUpperLimit, attackCountMax);
                        break;
                }
                
                for (var j = 0; j < attackCountMax + 1; j++)
                {
                    attackCount++;
                    if (attackCount <= attackCountMax)
                    {
                        var info = AttackCountHelper.CachedInfo[attackCountMax][attackCount];
                        Assert.AreEqual(info.DamageMultiplier, AttackCountHelper.GetDamageMultiplier(attackCount, attackCountMax));
                        
                        continue;
                    }
                
                    Assert.Throws<ArgumentOutOfRangeException>(() => AttackCountHelper.GetDamageMultiplier(attackCount, attackCountMax));
                }
            }
        }

        [Test]
        public void GetAdditionalCriticalChance()
        {
            for (var i = 0; i < 2; i++)
            {
                var level = i == 0
                    ? 1
                    : 999;
                var attackCount = 0;
                var attackCountMax = AttackCountHelper.GetCountMax(level);

                switch (level)
                {
                    case 1:
                        Assert.AreEqual(AttackCountHelper.CountMaxLowerLimit, attackCountMax);
                        break;
                    case 999:
                        Assert.AreEqual(AttackCountHelper.CountMaxUpperLimit, attackCountMax);
                        break;
                }
                
                for (var j = 0; j < attackCountMax + 1; j++)
                {
                    attackCount++;
                    if (attackCount <= attackCountMax)
                    {
                        var info = AttackCountHelper.CachedInfo[attackCountMax][attackCount];
                        Assert.AreEqual(info.AdditionalCriticalChance, AttackCountHelper.GetAdditionalCriticalChance(attackCount, attackCountMax));
                        
                        continue;
                    }
                
                    Assert.Throws<ArgumentOutOfRangeException>(() => AttackCountHelper.GetAdditionalCriticalChance(attackCount, attackCountMax));
                }
            }
        }
    }
}
