using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Data.Table;
using Nekoyume.Model;
using Unity.Mathematics;

namespace Nekoyume.Game.Skill
{
    [Serializable]
    public class AttackBase : SkillBase
    {
        protected AttackBase(decimal chance, SkillEffect effect,
            Data.Table.Elemental.ElementalType elemental, int power) : base(chance, effect, elemental, power)
        {
        }

        protected List<Model.Skill.SkillInfo> ProcessDamage(CharacterBase caster)
        {
            var targets = GetTarget(caster);
            var infos = new List<Model.Skill.SkillInfo>();
            var targetList = targets.ToArray();
            var elemental = Elemental.Create(elementalType);
            var multiplier = GetMultiplier(effect.hitCount, 1);
            var skillPower = CalcSkillPower(caster);
            for (var i = 0; i < effect.hitCount; i++)
            {
                foreach (var target in targetList)
                {
                    var multiply = multiplier[i];
                    var critical = caster.IsCritical();
                    var dmg = elemental.CalculateDmg(skillPower, target.defElement);
                    // https://gamedev.stackexchange.com/questions/129319/rpg-formula-attack-and-defense
                    dmg = (int) ((long) dmg * dmg / (dmg + target.def));
                    dmg = (int) (dmg * multiply);
                    dmg = math.max(dmg, 1);
                    if (critical)
                    {
                        dmg = (int) (dmg * CharacterBase.CriticalMultiplier);
                    }

                    target.OnDamage(dmg);

                    infos.Add(new Model.Skill.SkillInfo((CharacterBase) target.Clone(), dmg, critical, effect.category,
                        elementalType));
                }
            }

            return infos;
        }

        private int CalcSkillPower(CharacterBase caster)
        {
            // 플레이어가 사용하는 스킬은 기본 공격력 + 스킬 위력으로 스킬이 나가도록 설정합니다.
            if (caster is Player)
            {
                if (effect.category != SkillEffect.Category.Normal)
                {
                    return power + caster.atk;
                }
            }

            return power;
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
    public class NormalAttack : AttackBase
    {
        public NormalAttack(decimal chance, SkillEffect effect,
            Data.Table.Elemental.ElementalType elemental, int power) : base(chance, effect, elemental, power)
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
