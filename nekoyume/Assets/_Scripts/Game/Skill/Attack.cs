using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Data.Table;
using Nekoyume.Model;

namespace Nekoyume.Game.Skill
{
    [Serializable]
    public class AttackBase : SkillBase
    {
        protected AttackBase(float chance, SkillEffect effect,
            Data.Table.Elemental.ElementalType elemental, int value) : base(chance, effect, elemental, value)
        {
        }

        protected List<Model.Skill.SkillInfo> ProcessDamage(CharacterBase caster)
        {
            var targets = GetTarget(caster);
            var infos = new List<Model.Skill.SkillInfo>();
            var targetList = targets.ToArray();
            var elemental = Elemental.Create(elementalType);
            var multiplier = GetMultiplier(effect.hitCount, 1);
            for (var i = 0; i < effect.hitCount; i++)
            {
                foreach (var target in targetList)
                {
                    var multiply = multiplier[i];
                    var critical = caster.IsCritical();
                    var dmg = elemental.CalculateDmg(value, target.defElement);
                    // https://gamedev.stackexchange.com/questions/129319/rpg-formula-attack-and-defense
                    dmg = (dmg * dmg) / (dmg + target.def);
                    dmg = Convert.ToInt32(dmg * multiply);
                    dmg = Math.Max(dmg, 1);
                    if (critical)
                    {
                        dmg = Convert.ToInt32(dmg * CharacterBase.CriticalMultiplier);
                    }

                    target.OnDamage(dmg);

                    infos.Add(new Model.Skill.SkillInfo((CharacterBase) target.Clone(), dmg, critical, effect.category,
                        elementalType));
                }
            }

            return infos;
        }

        private float[] GetMultiplier(int count, float total)
        {
            if (count == 1) return new[] {total};
            var multiplier = new List<float>();
            var avg = total / count;
            var last = avg * 1.3f;
            var remain = count - 1;
            var dist = (total - last) / remain;
            for (int i = 0; i < count; i++)
            {
                var result = i == remain ? last : dist;
                multiplier.Add(result);
            }

            return multiplier.ToArray();
        }

        public override Model.Skill Use(CharacterBase caster)
        {
            throw new NotImplementedException();
        }
    }

    [Serializable]
    public class Attack : AttackBase
    {
        public Attack(float chance, SkillEffect effect,
            Data.Table.Elemental.ElementalType elemental, int value) : base(chance, effect, elemental, value)
        {
        }

        public override Model.Skill Use(CharacterBase caster)
        {
            var info = ProcessDamage(caster);

            return new Model.Attack
            {
                character = (CharacterBase) caster.Clone(),
                skillInfos = info,
            };
        }
    }
}
