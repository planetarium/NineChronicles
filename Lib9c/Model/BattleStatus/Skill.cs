#nullable enable
using System;
using System.Collections.Generic;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.Skill;

namespace Nekoyume.Model.BattleStatus
{
    [Serializable]
    public abstract class Skill : EventBase
    {
        [Serializable]
        public class SkillInfo
        {
            public readonly CharacterBase? Target;
            public readonly int Effect;
            public readonly bool Critical;
            public readonly SkillCategory SkillCategory;
            public readonly ElementalType ElementalType;
            public readonly SkillTargetType SkillTargetType;
            public readonly int WaveTurn;
            public readonly int Thorn;
            public readonly bool IsDead;
            public readonly Guid CharacterId;


            public readonly Model.Buff.Buff? Buff;

            public SkillInfo(Guid characterId, bool isDead, int thorn, int effect, bool critical, SkillCategory skillCategory,
                int waveTurn, ElementalType elementalType = ElementalType.Normal,
                SkillTargetType targetType = SkillTargetType.Enemy, Model.Buff.Buff? buff = null, CharacterBase? target = null)
            {
                CharacterId = characterId;
                IsDead = isDead;
                Thorn = thorn;
                Effect = effect;
                Critical = critical;
                SkillCategory = skillCategory;
                ElementalType = elementalType;
                SkillTargetType = targetType;
                Buff = buff;
                WaveTurn = waveTurn;
                Target = target;
            }
        }

        public readonly int SkillId;

        public readonly IEnumerable<SkillInfo> SkillInfos;


        public readonly IEnumerable<SkillInfo>? BuffInfos;

        protected Skill(int skillId, CharacterBase character, IEnumerable<SkillInfo> skillInfos,
            IEnumerable<SkillInfo> buffInfos) : base(character)
        {
            SkillId = skillId;
            SkillInfos = skillInfos;
            BuffInfos = buffInfos;
        }
    }
}
