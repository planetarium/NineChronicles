using System;
using System.Collections.Generic;
using Nekoyume.Data.Table;
using Nekoyume.Model;

namespace Nekoyume.Game.Skill
{
    public interface ISkill
    {
        SkillType GetSkillType();
        SkillEffect.SkillType GetSkillType();
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
        protected readonly SkillEffect Effect;

        public SkillEffect.SkillType GetSkillType()
        {
            return Effect.type;
        }

        public abstract EventBase Use();
        protected SkillBase(CharacterBase caster, IEnumerable<CharacterBase> target, int effect)
        {
            Caster = caster;
            Target = target;
            Effect = effect;
        }
    }
}
