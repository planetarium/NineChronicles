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
        Model.Skill Use();
    }

    [Serializable]
    public abstract class SkillBase: ISkill
    {
        protected readonly CharacterBase Caster;
        protected readonly SkillEffect Effect;
        public float chance;

        public SkillEffect.SkillType GetSkillType()
        {
            return Effect.type;
        }

        public abstract Model.Skill Use();
        protected SkillBase(CharacterBase caster, float chance, SkillEffect effect)
        {
            Caster = caster;
            Effect = effect;
            this.chance = chance;
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
