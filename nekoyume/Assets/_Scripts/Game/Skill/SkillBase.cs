using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Data.Table;
using Nekoyume.Model;

namespace Nekoyume.Game.Skill
{
    public interface ISkill
    {
        Model.Skill Use(CharacterBase caster);
    }

    [Serializable]
    public abstract class SkillBase: ISkill
    {
        public readonly float chance;
        public readonly SkillEffect effect;
        public readonly Data.Table.Elemental.ElementalType elementalType;

        public abstract Model.Skill Use(CharacterBase caster);
        protected SkillBase(float chance, SkillEffect effect, Data.Table.Elemental.ElementalType elementalType)
        {
            this.effect = effect;
            this.chance = chance;
            this.elementalType = elementalType;
        }
        
        protected IEnumerable<CharacterBase> GetTarget(CharacterBase caster)
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
