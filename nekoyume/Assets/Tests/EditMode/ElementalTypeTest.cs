using Nekoyume.Model.Elemental;
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
            Assert.AreEqual(ElementalResult.Win, attackerElementalType.GetBattleResult(defenderElementalType));
            var beforeDamage = 100;
            var afterDamage = attackerElementalType.GetDamage(defenderElementalType, beforeDamage);
            Assert.AreEqual(beforeDamage * ElementalTypeExtension.WinMultiplier, afterDamage);
        }
        
        [Test]
        public void DrawDamage()
        {
            var attackerElementalType = ElementalType.Fire;
            var defenderElementalType = ElementalType.Fire;
            Assert.AreEqual(ElementalResult.Draw, attackerElementalType.GetBattleResult(defenderElementalType));
            var beforeDamage = 100;
            var afterDamage = attackerElementalType.GetDamage(defenderElementalType, beforeDamage);
            Assert.AreEqual(beforeDamage, afterDamage);
        }
        
        [Test]
        public void LoseDamage()
        {
            var attackerElementalType = ElementalType.Fire;
            var defenderElementalType = ElementalType.Water;
            Assert.AreEqual(ElementalResult.Lose, attackerElementalType.GetBattleResult(defenderElementalType));
            var beforeDamage = 100;
            var afterDamage = attackerElementalType.GetDamage(defenderElementalType, beforeDamage);
            Assert.AreEqual(beforeDamage, afterDamage);
        }
    }
}
