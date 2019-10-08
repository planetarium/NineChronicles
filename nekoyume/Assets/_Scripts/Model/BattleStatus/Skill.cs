using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Nekoyume.Data.Table;
using Nekoyume.EnumType;

namespace Nekoyume.Model
{
    [Serializable]
    public abstract class Skill : EventBase
    {
        [Serializable]
        public class SkillInfo
        {
            public readonly CharacterBase Target;
            public readonly int Effect;
            public readonly bool Critical;
            public readonly SkillCategory SkillCategory;
            public readonly ElementalType ElementalType;
            [CanBeNull] public readonly Game.Buff Buff;

            public SkillInfo(CharacterBase character, int effect, bool critical, SkillCategory skillCategory,
                ElementalType elementalType = ElementalType.Normal,
                [CanBeNull] Game.Buff buff = null)
            {
                Target = character;
                Effect = effect;
                Critical = critical;
                SkillCategory = skillCategory;
                ElementalType = elementalType;
                Buff = buff;
            }
        }

        public readonly IEnumerable<SkillInfo> SkillInfos;
        [CanBeNull] public readonly IEnumerable<SkillInfo> BuffInfos;

        protected Skill(CharacterBase character, IEnumerable<SkillInfo> skillInfos, IEnumerable<SkillInfo> buffInfos) : base(character)
        {
            SkillInfos = skillInfos;
            BuffInfos = buffInfos;
        }
    }
}
