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
        public List<Buff> buffs;

        protected Skill(SkillSheet.Row skillRow, int power, decimal chance)
        {
            this.skillRow = skillRow;
            this.power = power;
            this.chance = chance;
            buffs = skillRow.GetBuffs().Select(BuffFactory.Get).ToList();

            if (!Tables.instance.SkillEffect.TryGetValue(skillRow.SkillEffectId, out effect))
            {
                throw new KeyNotFoundException(nameof(skillRow.SkillEffectId));
            }
        }

        public abstract Model.Skill Use(CharacterBase caster);

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

        protected IEnumerable<Model.Skill.SkillInfo> ProcessBuff(CharacterBase caster)
        {
            var infos = new List<Model.Skill.SkillInfo>();
            foreach (var buff in buffs)
            {
                var targets = buff.GetTarget(caster);
                foreach (var target in targets.Where(target => target.GetChance(buff.RowData.Chance)))
                {
                    target.AddBuff(buff);
                    infos.Add(new Model.Skill.SkillInfo((CharacterBase) target.Clone(), 0, false,
                        effect.skillCategory, ElementalType.Normal, buff));
                }
            }

            return infos;
        }
    }
}
