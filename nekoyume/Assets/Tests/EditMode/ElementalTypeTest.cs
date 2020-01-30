using Nekoyume.EnumType;
using NUnit.Framework;

namespace Tests.EditMode
{
    public class ElementalTypeTest
    {
        [Test]
        public void WinDamage()
        {
            var attackerElementalType = ElementalType.Fire;
            var defenderElementalType = ElementalType.Wind;
            Assert.AreEqual(attackerElementalType.GetBattleResult(defenderElementalType), ElementalResult.Win);
            var beforeDamage = 100;
            var afterDamage = attackerElementalType.GetDamage(defenderElementalType, beforeDamage);
            Assert.AreEqual(afterDamage, beforeDamage * ElementalTypeExtension.WinMultiplier);
        }
        
        [Test]
        public void DrawDamage()
        {
            var attackerElementalType = ElementalType.Fire;
            var defenderElementalType = ElementalType.Fire;
            Assert.AreEqual(attackerElementalType.GetBattleResult(defenderElementalType), ElementalResult.Draw);
            var beforeDamage = 100;
            var afterDamage = attackerElementalType.GetDamage(defenderElementalType, beforeDamage);
            Assert.AreEqual(afterDamage, beforeDamage);
        }
        
        [Test]
        public void LoseDamage()
        {
            var attackerElementalType = ElementalType.Fire;
            var defenderElementalType = ElementalType.Water;
            Assert.AreEqual(attackerElementalType.GetBattleResult(defenderElementalType), ElementalResult.Lose);
            var beforeDamage = 100;
            var afterDamage = attackerElementalType.GetDamage(defenderElementalType, beforeDamage);
            Assert.AreEqual(afterDamage, beforeDamage);
        }
    }
}
