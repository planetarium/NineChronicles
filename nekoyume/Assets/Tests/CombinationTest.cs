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
            var parts = Tables.instance.Item.Select(i => i.Value).First(r => r.skillId != 0);

            var result = Combination.GetEquipment(equipment, parts, 0);
            Assert.NotNull(result);
            Assert.GreaterOrEqual(result.SkillBase.chance, parts.minChance);
            Assert.LessOrEqual(result.SkillBase.chance, parts.maxChance);
            Assert.AreEqual(parts.elemental, result.SkillBase.elementalType);
        }

    }
}
