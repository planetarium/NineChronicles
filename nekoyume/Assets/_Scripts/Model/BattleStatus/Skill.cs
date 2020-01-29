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
            public readonly SkillTargetType SkillTargetType;
            public readonly int WaveTurn;
            [CanBeNull] public readonly Game.Buff Buff;

            public SkillInfo(CharacterBase character, int effect, bool critical, SkillCategory skillCategory,
                int turn, ElementalType elementalType = ElementalType.Normal,
                SkillTargetType targetType = SkillTargetType.Enemy, [CanBeNull] Game.Buff buff = null)
            {
                Target = character;
                Effect = effect;
                Critical = critical;
                SkillCategory = skillCategory;
                ElementalType = elementalType;
                SkillTargetType = targetType;
                Buff = buff;
                WaveTurn = turn;
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
