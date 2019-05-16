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
        protected AttackBase(CharacterBase caster, SkillEffect effect) : base(caster, effect)
        {
        }

        protected List<Model.Skill.SkillInfo> ProcessDamage(IEnumerable<CharacterBase> targets)
        {
            var infos = new List<Model.Skill.SkillInfo>();
            foreach (var target in targets.ToList())
            {
                var critical = Caster.IsCritical();
                var dmg = Caster.atkElement.CalculateDmg(Caster.atk, target.defElement);
                dmg = Math.Max(dmg - target.def, 1);
                dmg = Convert.ToInt32(dmg * Effect.multiplier);
                if (critical)
                {
                    dmg = Convert.ToInt32(dmg * CharacterBase.CriticalMultiplier);
                }

                target.OnDamage(dmg);

                infos.Add(new Model.Skill.SkillInfo(CharacterBase.Copy(target), dmg, critical));
            }

            return infos;
        }

        public override EventBase Use()
        {
            throw new NotImplementedException();
        }
    }

    [Serializable]
    public class Attack : AttackBase
    {
        public Attack(CharacterBase caster, SkillEffect effect) : base(caster, effect)
        {
        }

        public override EventBase Use()
        {
            var target = GetTarget();
            var info = ProcessDamage(target);

            return new Model.Attack
            {
                character = CharacterBase.Copy(Caster),
                skillInfos = info,
            };
        }
    }
}
