using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Data.Table;
using Nekoyume.Model;

namespace Nekoyume.Game.Skill
{
    public interface ISkill
    {
        SkillEffect.SkillType GetSkillType();
        EventBase Use();
    }

    [Serializable]
    public abstract class SkillBase: ISkill
    {
        protected readonly CharacterBase Caster;
        protected readonly SkillEffect Effect;

        public SkillEffect.SkillType GetSkillType()
        {
            return Effect.type;
        }

        public abstract EventBase Use();
        protected SkillBase(CharacterBase caster, SkillEffect effect)
        {
            Caster = caster;
            Effect = effect;
        }

        protected IEnumerable<CharacterBase> GetTarget()
        {
            var targets = Caster.targets;
            switch (Effect.target)
            {
                case SkillEffect.Target.Enemy:
                    return new[] {targets.First()};
                case SkillEffect.Target.Enemies:
                    return targets;
                case SkillEffect.Target.Self:
                    return new[] {Caster};
                case SkillEffect.Target.Ally:
                    break;
            }

            return new[] {targets.First()};
        }
    }
}
