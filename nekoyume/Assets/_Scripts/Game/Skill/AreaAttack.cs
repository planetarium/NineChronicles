using System;
using Nekoyume.Data.Table;
using Nekoyume.Model;

namespace Nekoyume.Game.Skill
{
    [Serializable]
    public class AreaAttack: AttackBase
    {
        public AreaAttack(float chance, SkillEffect effect,
            Data.Table.Elemental.ElementalType elemental, int value) : base(chance, effect, elemental, value)
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
