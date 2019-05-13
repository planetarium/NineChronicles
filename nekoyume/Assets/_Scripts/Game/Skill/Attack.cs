using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Model;

namespace Nekoyume.Game.Skill
{
    [Serializable]
    public class AttackBase: SkillBase
    {
        protected AttackBase(CharacterBase caster, IEnumerable<CharacterBase> target, int effect)
            : base(caster, target, effect)
        {
        }

        protected Model.Attack.AttackInfo ProcessDamage(CharacterBase target)
        {
            var critical = Caster.IsCritical();
            var dmg = Caster.ATKElement.CalculateDmg(Effect, target.DEFElement);
            dmg = Math.Max(dmg - target.def, 1);
            if (critical)
            {
                dmg = Convert.ToInt32(dmg * CharacterBase.CriticalMultiplier);
            }

            target.OnDamage(dmg);

            return new Model.Attack.AttackInfo(CharacterBase.Copy(target), dmg, critical);
        }

        public override SkillType GetSkillType()
        {
            return SkillType.Attack;
        }

        public override EventBase Use()
        {
            throw new NotImplementedException();
        }
    }

    [Serializable]
    public class Attack : AttackBase, ISingleTargetSkill
    {
        public Attack(CharacterBase caster, IEnumerable<CharacterBase> target, int effect)
            : base(caster, target, effect)
        {
        }

        public CharacterBase GetTarget()
        {
            return Target.First();
        }

        public override EventBase Use()
        {
            var target = GetTarget();
            var info = ProcessDamage(target);

            return new Model.Attack
            {
                character = CharacterBase.Copy(Caster),
                info = info,
            };
        }
    }
}
