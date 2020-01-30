using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Nekoyume.EnumType;
using Nekoyume.Model.Buff;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.Model.Skill
{
    [Serializable]
    public abstract class Skill : IState
    {
        public readonly SkillSheet.Row skillRow;
        public readonly int power;
        public readonly int chance;
        public List<Buff.Buff> buffs;

        protected Skill(SkillSheet.Row skillRow, int power, int chance)
        {
            this.skillRow = skillRow;
            this.power = power;
            this.chance = chance;
            buffs = skillRow.GetBuffs().Select(BuffFactory.Get).ToList();
        }

        public abstract Model.BattleStatus.Skill Use(CharacterBase caster, int simulatorWaveTurn);

        protected bool Equals(Skill other)
        {
            return skillRow.Equals(other.skillRow) && power == other.power && chance.Equals(other.chance);
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
                return hashCode;
            }
        }

        protected IEnumerable<Model.BattleStatus.Skill.SkillInfo> ProcessBuff(CharacterBase caster, int simulatorWaveTurn)
        {
            var infos = new List<Model.BattleStatus.Skill.SkillInfo>();
            foreach (var buff in buffs)
            {
                var targets = buff.GetTarget(caster);
                foreach (var target in targets.Where(target => target.GetChance(buff.RowData.Chance)))
                {
                    target.AddBuff(buff);
                    infos.Add(new Model.BattleStatus.Skill.SkillInfo((CharacterBase) target.Clone(), 0, false,
                        skillRow.SkillCategory, simulatorWaveTurn, ElementalType.Normal, skillRow.SkillTargetType, buff));
                }
            }

            return infos;
        }

        public IValue Serialize() =>
            new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "skillRow"] = skillRow.Serialize(),
                [(Text) "power"] = (Integer) power,
                [(Text) "chance"] = (Integer) chance,
            });
    }
}
