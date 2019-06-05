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
        protected readonly SkillEffect Effect;
        public float chance;

        public SkillEffect.SkillType GetSkillType()
        {
            return Effect.type;
        }

        public SkillEffect.Category GetCategory()
        {
            return Effect.category;
        }

        public abstract Model.Skill Use();
        protected SkillBase(float chance, SkillEffect effect)
        {
            Effect = effect;
            this.chance = chance;
        }


        protected IEnumerable<CharacterBase> GetTarget()
        {
            var targets = caster.targets;
            IEnumerable<CharacterBase> target;
            switch (Effect.target)
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
