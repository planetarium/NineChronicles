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
            IEnumerable<CharacterBase> target;
            switch (Effect.target)
            {
                case SkillEffect.Target.Enemy:
                    target = new[] {targets.First()};
                    break;
                case SkillEffect.Target.Enemies:
                    target = Caster.targets;
                    break;
                case SkillEffect.Target.Self:
                    target = new[] {Caster};
                    break;
                case SkillEffect.Target.Ally:
                    target = new[] {Caster};
                    break;
                default:
                    target = new[] {targets.First()};
                    break;
            }

            return target;
        }
    }
}
