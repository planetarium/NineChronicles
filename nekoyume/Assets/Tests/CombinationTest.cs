using System.Linq;
using Nekoyume.Action;
using Nekoyume.Data;
using NUnit.Framework;

namespace Tests
{
    public class CombinationTest
    {
        [Test]
        public void GetEquipmentWithoutSkill()
        {
            var equipment = Tables.instance.ItemEquipment.First().Value;
            var parts = Tables.instance.Item.Select(i => i.Value).First(r => r.skillId == 0);

            var result = Combination.GetEquipment(equipment, parts, 0);
            Assert.NotNull(result);
            Assert.Null(result.SkillBase);
        }

        [Test]
        public void GetEquipmentWithSkill()
        {
            var equipment = Tables.instance.ItemEquipment.First().Value;
            var parts = Tables.instance.Item.Select(i => i.Value)
                .First(r => r.skillId != 0 && r.minChance > 0.01f);

            var result = Combination.GetEquipment(equipment, parts, 0);
            Assert.NotNull(result);
            Assert.AreEqual(parts.minChance, result.SkillBase.chance);
            Assert.AreEqual(parts.elemental, result.SkillBase.elementalType);
            Assert.AreEqual(parts.minDamage, result.SkillBase.power);
        }

    }
}
