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

        [SetUp]
        public void SetUp()
        {
            _tableSheets = TableSheetsHelper.MakeTableSheets();
        }

        [TearDown]
        public void TearDown()
        {
            _tableSheets = null;
        }
        
        [Test]
        public void SelectTest()
        {
            var skillSheet = _tableSheets.SkillSheet;
            Assert.IsTrue(skillSheet.Any());
            var skillRow = skillSheet.First().Value;
            var firstSkill = SkillFactory.Get(skillRow, 100, 100);
            
            var skillBuffSheet = _tableSheets.SkillBuffSheet;
            Assert.IsTrue(skillBuffSheet.Any());
            var skillBuffRow = skillBuffSheet.First();
            skillRow = skillSheet.Values.FirstOrDefault(row => row.Id == skillBuffRow.Value.SkillId);
            Assert.NotNull(skillRow);
            var firstBuffSkill = SkillFactory.Get(skillRow, 100, 100);
            var buffs = BuffFactory.GetBuffs(firstBuffSkill, _tableSheets.SkillBuffSheet, _tableSheets.BuffSheet)
                .ToDictionary(e => e.RowData.GroupId, e => e);
            
            Assert.IsFalse(firstSkill.Equals(firstBuffSkill));

            var skills = new Skills {firstSkill};
            var selectedSkill = skills.Select(new Random(), null, _tableSheets.SkillBuffSheet, _tableSheets.BuffSheet);
            Assert.IsTrue(firstSkill.Equals(selectedSkill));

            skills.Add(firstBuffSkill);
            selectedSkill = skills.Select(new Random(), buffs, _tableSheets.SkillBuffSheet, _tableSheets.BuffSheet);
            Assert.IsTrue(firstSkill.Equals(selectedSkill));
        }
    }
}
