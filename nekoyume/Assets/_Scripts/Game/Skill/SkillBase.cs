using System;
using System.Collections.Generic;
using Nekoyume.Model;

namespace Nekoyume.Game.Skill
{
    public enum SkillType
    {
        Attack,
        Buff,
        Debuff,
    }

    public interface ISkill
    {
        SkillType GetSkillType();
        EventBase Use();
    }

    public interface ISingleTargetSkill: ISkill
    {
        CharacterBase GetTarget();
    }

    public interface IMultipleTargetSkill : ISkill
    {
        List<CharacterBase> GetTarget();
    }

    [Serializable]
    public abstract class SkillBase: ISkill
    {
        protected readonly CharacterBase Caster;
        protected readonly IEnumerable<CharacterBase> Target;
        protected readonly int Effect;

        public abstract SkillType GetSkillType();

        public abstract EventBase Use();
        protected SkillBase(CharacterBase caster, IEnumerable<CharacterBase> target, int effect)
        {
            Caster = caster;
            Target = target;
            Effect = effect;
        }
    }
}
