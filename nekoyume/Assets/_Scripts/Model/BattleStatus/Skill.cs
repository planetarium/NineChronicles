using System;
using System.Collections.Generic;
using Nekoyume.Data.Table;

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
            public readonly SkillEffect.Category Category;
            public readonly Elemental.ElementalType Elemental;

            public SkillInfo(CharacterBase character, int effect, bool critical, SkillEffect.Category category)
            {
                Target = character;
                Effect = effect;
                Critical = critical;
                Category = category;
                Elemental = Data.Table.Elemental.ElementalType.Normal;
            }
        }
    }
}
