using System;
using Nekoyume.Data.Table;
using Nekoyume.Model;

namespace Nekoyume.Game.Skill
{
    [Serializable]
    public class AreaAttack: AttackBase
    {
        public AreaAttack(float chance, SkillEffect effect,
            Data.Table.Elemental.ElementalType elemental) : base(chance, effect, elemental)
        {
        }

        public override Model.Skill Use(CharacterBase caster)
        {

            return new Model.AreaAttack
            {
                character = (CharacterBase) caster.Clone(),
                skillInfos = ProcessDamage(caster),
            };
        }
    }
}
