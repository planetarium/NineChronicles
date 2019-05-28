using System;
using Nekoyume.Data.Table;
using Nekoyume.Model;

namespace Nekoyume.Game.Skill
{
    [Serializable]
    public class AreaAttack: AttackBase
    {
        public AreaAttack(CharacterBase caster, float chance, SkillEffect effect) : base(caster, chance, effect)
        {
        }

        public override Model.Skill Use()
        {

            return new Model.AreaAttack
            {
                character = (CharacterBase) Caster.Clone(),
                skillInfos = ProcessDamage(GetTarget()),
            };
        }
    }
}
