using System.Collections.Generic;
using System.Linq;
using Nekoyume.Data;
using Nekoyume.Data.Table;
using Nekoyume.Game.Skill;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class SkillFixture
    {
        public SkillBase[] Skills;

        public SkillFixture()
        {
            var skillList = new List<SkillBase>();
            var table = Tables.instance.SkillEffect;
            var elementalType = Elemental.ElementalType.Normal;
            foreach (var effect in table.Values)
            {
                var skill = SkillFactory.Get(1f, effect, elementalType);
                skillList.Add(skill);
            }

            Skills = skillList.ToArray();
        }

        public SkillBase Get<T>() where T : SkillBase
        {
            return Skills.FirstOrDefault(s => s is T);
        }
    }
}
