using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Data;
using Nekoyume.Data.Table;
using Nekoyume.EnumType;
using Nekoyume.Model;
using Nekoyume.TableData;

namespace Nekoyume.Game
{
    [Serializable]
    public abstract class Skill
    {
        public readonly SkillSheet.Row skillRow;
        public readonly int power;
        public readonly decimal chance;
        public readonly SkillEffect effect;

        public abstract Model.Skill Use(CharacterBase caster);

        protected Skill(SkillSheet.Row skillRow, int power, decimal chance)
        {
            this.skillRow = skillRow;
            this.power = power;
            this.chance = chance;

            if (!Tables.instance.SkillEffect.TryGetValue(skillRow.SkillEffectId, out effect))
            {
                throw new KeyNotFoundException(nameof(skillRow.SkillEffectId));
            }
        }

        protected bool Equals(Skill other)
        {
            return skillRow.Equals(other.skillRow) && power == other.power && chance.Equals(other.chance) &&
                   Equals(effect, other.effect);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Skill) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = skillRow.GetHashCode();
                hashCode = (hashCode * 397) ^ power;
                hashCode = (hashCode * 397) ^ chance.GetHashCode();
                hashCode = (hashCode * 397) ^ (effect != null ? effect.GetHashCode() : 0);
                return hashCode;
            }
        }

        protected IEnumerable<CharacterBase> GetTarget(CharacterBase caster)
        {
            var targets = caster.targets;
            IEnumerable<CharacterBase> target;
            switch (effect.skillTargetType)
            {
                case SkillTargetType.Enemy:
                    target = new[] {targets.First()};
                    break;
                case SkillTargetType.Enemies:
                    target = caster.targets;
                    break;
                case SkillTargetType.Self:
                    target = new[] {caster};
                    break;
                case SkillTargetType.Ally:
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
