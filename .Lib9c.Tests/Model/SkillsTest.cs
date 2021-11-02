namespace Lib9c.Tests.Model
{
    using System;
    using System.Linq;
    using Lib9c.Tests.Action;
    using Libplanet.Action;
    using Nekoyume;
    using Nekoyume.Model;
    using Nekoyume.Model.Skill;
    using Nekoyume.TableData;
    using Xunit;

    public class SkillsTest
    {
        private readonly SkillSheet _skillSheet;
        private readonly IRandom _random;

        public SkillsTest()
        {
            _skillSheet = new SkillSheet();
            _skillSheet.Set(TableSheetsImporter.ImportSheets()[nameof(SkillSheet)]);
            _random = new TestRandom();
        }

        public Nekoyume.Model.Skill.Skill GetDefaultAttackSkill()
        {
            var skillRow = _skillSheet.First().Value;
            Assert.NotNull(skillRow);

            Assert.Equal(GameConfig.DefaultAttackId, skillRow.Id);

            var defaultAttack = SkillFactory.Get(skillRow, 100, 100);
            Assert.NotNull(defaultAttack);
            return defaultAttack;
        }

        [Fact]
        public void CheckSkillCooldown()
        {
            var skills = new Skills();
            var defaultAttack = GetDefaultAttackSkill();
            skills.Add(defaultAttack);

            const int skillId = 130005;
            _skillSheet.TryGetValue(skillId, out var skillRow);
            var skill = SkillFactory.Get(skillRow, 100, 100);
            skills.Add(skill);

            var selectedSkill = skills.Select(_random);
            Assert.NotNull(selectedSkill);
            // When you have a skill, make sure that the skill is pulled out.
            Assert.Equal(skill, selectedSkill);

            skills.SetCooldown(skillId, 2);
            skills.ReduceCooldown();
            selectedSkill = skills.Select(_random);
            Assert.NotNull(selectedSkill);
            //When the cooldown is 1 or more, check if the skill is not selected
            Assert.Equal(defaultAttack, selectedSkill);
        }

        [Fact]
        public void ExecuteThrowException()
        {
            var skills = new Skills();
            const int skillId = 130005;
            _skillSheet.TryGetValue(skillId, out var skillRow);
            var skill = SkillFactory.Get(skillRow, 100, 100);
            skills.Add(skill);
            Assert.Throws<Exception>(() => skills.Select(_random));
        }

        [Fact]
        public void ExecuteDefaultAttack()
        {
            var skills = new Skills();

            // add default attack
            var defaultAttack = GetDefaultAttackSkill();
            skills.Add(defaultAttack);

            // add skill
            const int skillId = 130005;
            _skillSheet.TryGetValue(skillId, out var skillRow);
            var skill = SkillFactory.Get(skillRow, 100, 0);
            skills.Add(skill);

            // select skill
            var selectedSkill = skills.Select(_random);

            Assert.Equal(defaultAttack.SkillRow.Id, selectedSkill.SkillRow.Id);
        }

        [Fact]
        public void ExecuteSkill()
        {
            var skills = new Skills();

            // add default attack
            var defaultAttack = GetDefaultAttackSkill();
            skills.Add(defaultAttack);

            // add skill
            const int skillId = 130005;
            _skillSheet.TryGetValue(skillId, out var skillRow);
            var skill = SkillFactory.Get(skillRow, 100, 100);
            skills.Add(skill);

            // select skill
            var selectedSkill = skills.Select(_random);

            Assert.Equal(skill.SkillRow.Id, selectedSkill.SkillRow.Id);
        }

        [Fact]
        public void ExecuteAnySkill()
        {
            var skills = new Skills();

            // add default attack
            var defaultAttack = GetDefaultAttackSkill();
            skills.Add(defaultAttack);

            // add skill A
            _skillSheet.TryGetValue(130005, out var rowA);
            var skillA = SkillFactory.Get(rowA, 100, 50);
            skills.Add(skillA);

            // add skill B
            _skillSheet.TryGetValue(140000, out var rowB);
            var skillB = SkillFactory.Get(rowB, 100, 50);
            skills.Add(skillB);

            // select skill
            var selectedSkill = skills.Select(_random);

            Assert.NotEqual(defaultAttack.SkillRow.Id, selectedSkill.SkillRow.Id);
        }
    }
}
