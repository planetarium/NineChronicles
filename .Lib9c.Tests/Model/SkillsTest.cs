namespace Lib9c.Tests.Model
{
    using System.Collections.Generic;
    using Lib9c.Tests.Action;
    using Nekoyume.Model;
    using Nekoyume.Model.Skill;
    using Xunit;
    using Xunit.Abstractions;

    public class SkillsTest
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public SkillsTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Theory]
        [InlineData(1000)]
        public void PostSelectTest(int count)
        {
            var sheets = TableSheetsImporter.ImportSheets();
            var tableSheets = new TableSheets(sheets);

            var skills = new Skills();
            if (!tableSheets.SkillSheet.TryGetValue(100000, out var defaultAttackRow))
            {
                throw new KeyNotFoundException("100000");
            }

            skills.Add(SkillFactory.Get(defaultAttackRow, 0, 100));

            if (!tableSheets.SkillSheet.TryGetValue(110005, out var skillRow))
            {
                throw new KeyNotFoundException("110005");
            }

            skills.Add(SkillFactory.Get(skillRow, 0, 100));

            var result = new Dictionary<int, int>();
            var rand = new TestRandom();
            for (var i = 0; i < count; i++)
            {
                var skill = skills.Select(rand);
                if (result.ContainsKey(skill.SkillRow.Id))
                {
                    result[skill.SkillRow.Id] += 1;
                }
                else
                {
                    result.Add(skill.SkillRow.Id, 1);
                }
            }

            _testOutputHelper.WriteLine($"[Result - count : {count}]");
            foreach (var (key, value) in result)
            {
                _testOutputHelper.WriteLine($"{key} / {value}");
            }
        }
    }
}
