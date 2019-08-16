using System;
using Nekoyume.Data.Table;
using Nekoyume.Model;

namespace Nekoyume.Game.Skill
{
    [Serializable]
    public class AreaAttack: Attack
    {
        public AreaAttack(decimal chance, SkillEffect effect,
            Data.Table.Elemental.ElementalType elemental, int power) : base(chance, effect, elemental, power)
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
