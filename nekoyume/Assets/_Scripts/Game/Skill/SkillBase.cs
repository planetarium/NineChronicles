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
        public CharacterBase caster;
        public float chance;
        public readonly SkillEffect effect;

        public SkillEffect.SkillType GetSkillType()
        {
            return effect.type;
        }

        public SkillEffect.Category GetCategory()
        {
            return effect.category;
        }

        public abstract Model.Skill Use();
        protected SkillBase(float chance, SkillEffect effect)
        {
            this.effect = effect;
            this.chance = chance;
        }


        protected IEnumerable<CharacterBase> GetTarget()
        {
            var targets = caster.targets;
            IEnumerable<CharacterBase> target;
            switch (effect.target)
            {
                case SkillEffect.Target.Enemy:
                    target = new[] {targets.First()};
                    break;
                case SkillEffect.Target.Enemies:
                    target = caster.targets;
                    break;
                case SkillEffect.Target.Self:
                    target = new[] {caster};
                    break;
                case SkillEffect.Target.Ally:
                    target = new[] {caster};
                    break;
                default:
                    target = new[] {targets.First()};
                    break;
            }

            return target;
        }
    }
}
