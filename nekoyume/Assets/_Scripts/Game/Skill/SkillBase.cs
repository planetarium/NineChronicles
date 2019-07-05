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
    public abstract class SkillBase : ISkill
    {
        public readonly float chance;
        public readonly SkillEffect effect;
        public readonly Data.Table.Elemental.ElementalType elementalType;
        public readonly int value;

        public abstract Model.Skill Use(CharacterBase caster);

        protected SkillBase(float chance, SkillEffect effect, Data.Table.Elemental.ElementalType elementalType,
            int value)
        {
            this.chance = chance;
            this.effect = effect;
            this.elementalType = elementalType;
            this.value = value;
        }

        protected bool Equals(SkillBase other)
        {
            return chance.Equals(other.chance) && Equals(effect, other.effect) &&
                   elementalType == other.elementalType && value == other.value;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SkillBase) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = chance.GetHashCode();
                hashCode = (hashCode * 397) ^ (effect != null ? effect.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (int) elementalType;
                hashCode = (hashCode * 397) ^ value;
                return hashCode;
            }
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
