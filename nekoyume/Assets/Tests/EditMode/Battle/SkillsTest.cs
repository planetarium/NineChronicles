using System.Linq;
using Nekoyume.Helper;
using Nekoyume.Model;
using Nekoyume.Model.Buff;
using Nekoyume.Model.Skill;
using Nekoyume.TableData;
using NUnit.Framework;

namespace Tests.EditMode.Battle
{
    public class SkillsTest
    {
        private TableSheets _tableSheets;
        private SkillSheet _skillSheet;
        private SkillBuffSheet _skillBuffSheet;

        [SetUp]
        public void SetUp()
        {
            _tableSheets = TableSheetsHelper.MakeTableSheets();
            Assert.IsNotNull(_tableSheets);
            _skillSheet = _tableSheets.SkillSheet;
            Assert.IsNotNull(_skillSheet);
            Assert.IsTrue(_skillSheet.Any());
            _skillBuffSheet = _tableSheets.SkillBuffSheet;
            Assert.IsNotNull(_skillBuffSheet);
            Assert.IsTrue(_skillBuffSheet.Any());
        }

        [TearDown]
        public void TearDown()
        {
            _tableSheets = null;
        }

        [Test]
        public void SelectTest()
        {
            var skillRow = _skillSheet.First().Value;
            Assert.IsNotNull(skillRow);
            
            var firstSkill = SkillFactory.Get(skillRow, 100, 100);
            Assert.IsNotNull(firstSkill);
            
            var skills = new Skills {firstSkill};
            var selectedSkill = skills.Select(new Random());
            Assert.IsNotNull(selectedSkill);
            Assert.AreEqual(firstSkill, selectedSkill.Skill);

            selectedSkill.Cooldown = 1;
            selectedSkill = skills.Select(new Random());
            Assert.IsNull(selectedSkill);

            foreach (var skill in skills)
            {
                skill.Cooldown = 0;
            }
            
            selectedSkill = skills.Select(new Random());
            Assert.IsNotNull(selectedSkill);
            Assert.AreEqual(firstSkill, selectedSkill.Skill);
        }

        [Test]
        public void SelectWithBuffsTest()
        {
            var skillRow = _skillSheet.First().Value;
            var firstSkill = SkillFactory.Get(skillRow, 100, 100);

            var skillBuffRow = _skillBuffSheet.First();
            skillRow = _skillSheet.Values.FirstOrDefault(row => row.Id == skillBuffRow.Value.SkillId);
            Assert.NotNull(skillRow);
            var firstBuffSkill = SkillFactory.Get(skillRow, 100, 100);
            var buffs = BuffFactory.GetBuffs(firstBuffSkill, _tableSheets.SkillBuffSheet, _tableSheets.BuffSheet)
                .ToDictionary(e => e.RowData.GroupId, e => e);

            Assert.IsFalse(firstSkill.Equals(firstBuffSkill));

            var skills = new Skills {firstSkill};
            var selectedSkill = skills.Select(new Random(), null, _tableSheets.SkillBuffSheet, _tableSheets.BuffSheet);
            Assert.IsTrue(firstSkill.Equals(selectedSkill.Skill));

            skills.Add(firstBuffSkill);
            selectedSkill = skills.Select(new Random(), buffs, _tableSheets.SkillBuffSheet, _tableSheets.BuffSheet);
            Assert.IsTrue(firstSkill.Equals(selectedSkill.Skill));
        }
    }
}
