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
    }
}
