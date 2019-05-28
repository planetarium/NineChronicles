using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Data.Table;
using Nekoyume.Model;

namespace Nekoyume.Game.Skill
{
    [Serializable]
    public class AttackBase: SkillBase
    {
        protected AttackBase(CharacterBase caster, float chance, SkillEffect effect) : base(caster, chance, effect)
        {
            this.chance = chance;
        }

        protected List<Model.Skill.SkillInfo> ProcessDamage(IEnumerable<CharacterBase> targets)
        {
            var infos = new List<Model.Skill.SkillInfo>();
            foreach (var target in targets.ToList())
            {
                var critical = Caster.IsCritical();
                var dmg = Caster.atkElement.CalculateDmg(Caster.atk, target.defElement);
                // https://gamedev.stackexchange.com/questions/129319/rpg-formula-attack-and-defense
                dmg = Math.Max((dmg * dmg) / (dmg + target.def), 1);
                dmg = Convert.ToInt32(dmg * Effect.multiplier);
                if (critical)
                {
                    dmg = Convert.ToInt32(dmg * CharacterBase.CriticalMultiplier);
                }

                target.OnDamage(dmg);

                infos.Add(new Model.Skill.SkillInfo((CharacterBase) target.Clone(), dmg, critical, Effect.category));
            }

            return infos;
        }

        public override Model.Skill Use()
        {
            throw new NotImplementedException();
        }
    }

    [Serializable]
    public class Attack : AttackBase
    {
        public Attack(CharacterBase caster, float chance, SkillEffect effect) : base(caster, chance, effect)
        {
        }

        public override Model.Skill Use()
        {
            var target = GetTarget();
            var info = ProcessDamage(target);

            return new Model.Attack
            {
                character = (CharacterBase) Caster.Clone(),
                skillInfos = info,
            };
        }
    }
}
