using System;
using Nekoyume.Data.Table;
using Nekoyume.Model;

namespace Nekoyume.Game.Skill
{
    [Serializable]
    public class AreaAttack: AttackBase
    {
        public AreaAttack(CharacterBase caster, float chance, SkillEffect effect,
            Data.Table.Elemental.ElementalType elemental) : base(caster, chance, effect, elemental)
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
