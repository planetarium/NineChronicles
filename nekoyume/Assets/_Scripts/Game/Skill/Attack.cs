using System;
using Nekoyume.Model;

namespace Nekoyume.Game.Skill
{
    [Serializable]
    public class Attack : SkillBase
    {
        public Attack(CharacterBase caster, CharacterBase target, int effect) : base(caster, target, effect)
        {
        }

        public override SkillType GetSkillType()
        {
            return SkillType.Attack;
        }

        public override Model.Attack Use()
        {
            var target = GetTarget();
            var critical = Caster.IsCritical();
            var dmg = Caster.ATKElement.CalculateDmg(Effect, target.DEFElement);
            dmg = Math.Max(dmg - target.def, 1);
            if (critical)
            {
                dmg = Convert.ToInt32(dmg * CharacterBase.CriticalMultiplier);
            }
            target.OnDamage(dmg);

            return new Model.Attack
            {
                character = CharacterBase.Copy(Caster),
                target = CharacterBase.Copy(target),
                atk = dmg,
                critical = critical,
            };
        }
    }
}
