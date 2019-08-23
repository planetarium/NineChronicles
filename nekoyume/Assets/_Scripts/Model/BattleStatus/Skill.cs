using System;
using System.Collections.Generic;
using Nekoyume.Data.Table;
using Nekoyume.EnumType;

namespace Nekoyume.Model
{
    [Serializable]
    public abstract class Skill : EventBase
    {
        public IEnumerable<SkillInfo> skillInfos;

        [Serializable]
        public class SkillInfo
        {
            public readonly CharacterBase Target;
            public readonly int Effect;
            public readonly bool Critical;
            public readonly SkillCategory skillCategory;
            public Elemental.ElementalType? Elemental;

            public SkillInfo(CharacterBase character, int effect, bool critical, SkillCategory skillCategory,
                Elemental.ElementalType? elemental = null)
            {
                Target = character;
                Effect = effect;
                Critical = critical;
                this.skillCategory = skillCategory;
                Elemental = elemental;
            }
        }
    }
}
