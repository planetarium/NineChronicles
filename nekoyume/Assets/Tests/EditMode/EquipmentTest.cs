using System.Linq;
using Bencodex.Types;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.Model.Skill;
using Nekoyume.Model.Stat;
using NUnit.Framework;

namespace Tests.EditMode
{
    public class EquipmentTest
    {
        private TableSheets _tableSheets;

        [OneTimeSetUp]
        public void Init()
        {
            _tableSheets = TableSheetsHelper.MakeTableSheets();
        }

        [Test]
        public void LevelUp()
        {
            var row = _tableSheets.EquipmentItemSheet.Values.First();
            var equipment = (Equipment) ItemFactory.CreateItemUsable(row, default, default);
            var stat = equipment.StatsMap.GetStat(equipment.UniqueStatType);
            Assert.AreEqual(0, equipment.level);
            Assert.IsEmpty(equipment.GetOptions());
            equipment.LevelUp();
            Assert.AreEqual(1, equipment.level);
            Assert.AreEqual(decimal.ToInt32(stat + stat * 0.1m),
                equipment.StatsMap.GetStat(equipment.UniqueStatType));
            equipment.LevelUp();
            Assert.AreEqual(2, equipment.level);
            Assert.AreEqual(decimal.ToInt32(stat + stat * 0.2m),
                equipment.StatsMap.GetStat(equipment.UniqueStatType));
            equipment.LevelUp();
            Assert.AreEqual(3, equipment.level);
            Assert.AreEqual(decimal.ToInt32(stat + stat * 0.3m),
                equipment.StatsMap.GetStat(equipment.UniqueStatType));
        }

        [Test]
        public void LevelUpWithAdditionalStats(
            [Values(4, 7, 10)] int level
        )
        {
            var expectedHp = 130;
            if (level == 7)
            {
                expectedHp = 169;
            }
            if (level == 10)
            {
                expectedHp = 219;
            }
            var row = _tableSheets.EquipmentItemSheet.Values.First();
            var equipment = (Equipment) ItemFactory.CreateItemUsable(row, default, default);
            equipment.StatsMap.AddStatAdditionalValue(StatType.HP, 100);
            var stat = equipment.StatsMap.GetStat(equipment.UniqueStatType);
            Assert.AreEqual(0, equipment.level);
            Assert.IsNotEmpty(equipment.GetOptions());
            while (equipment.level < level)
            {
                equipment.LevelUp();
            }
            Assert.AreEqual(level, equipment.level);
            Assert.AreEqual(decimal.ToInt32(stat + stat * 0.1m * level),
                equipment.StatsMap.GetStat(equipment.UniqueStatType));
            Assert.AreEqual(expectedHp, equipment.StatsMap.AdditionalHP);
            var serialized = (Dictionary) equipment.Serialize();
            var actual = (Weapon) ItemFactory.Deserialize(serialized);
            Assert.AreEqual(equipment, actual);
        }

        [Test, Sequential]
        public void LevelUpWithSkills(
            [Values(SkillType.Attack, SkillType.Debuff, SkillType.Buff)] SkillType skillType,
            [Values(4, 7, 10)] int level,
            [Values(13, 16, 20)] int expectedSkillChance,
            [Values(13, 16, 20)] int expectedPower
        )
        {
            var row = _tableSheets.EquipmentItemSheet.Values.First();
            var equipment = (Equipment) ItemFactory.CreateItemUsable(row, default, default);
            var skillRow = _tableSheets.SkillSheet.Values.First(i => i.SkillType == skillType);
            var skill = SkillFactory.Get(skillRow, 10, 10);
            if (skillType == SkillType.Debuff || skillType == SkillType.Buff)
            {
                equipment.BuffSkills.Add((BuffSkill) skill);
            }
            else
            {
                equipment.Skills.Add(skill);
            }
            Assert.AreEqual(0, equipment.level);
            Assert.IsNotEmpty(equipment.GetOptions());
            Assert.AreEqual(10, skill.Chance);
            Assert.AreEqual(10, skill.Power);
            while (equipment.level < level)
            {
                equipment.LevelUp();
            }
            Assert.AreEqual(level, equipment.level);
            Assert.AreEqual(expectedSkillChance, skill.Chance);
            Assert.AreEqual(expectedPower, skill.Power);
            var serialized = (Dictionary) equipment.Serialize();
            var actual = (Weapon) ItemFactory.Deserialize(serialized);
            Assert.AreEqual(equipment, actual);
        }
    }
}
