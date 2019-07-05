using System;
using Nekoyume.Data.Table;
using Nekoyume.Model;

namespace Nekoyume.Game.Skill
{
    [Serializable]
    public class Blow : AttackBase
    {
        public Blow(float chance, SkillEffect effect, Data.Table.Elemental.ElementalType elemental, int value)
            : base(chance, effect, elemental, value)
        {
        }

        public override Model.Skill Use(CharacterBase caster)
        {
            var info = ProcessDamage(caster);

            return new Model.Blow
            {
                character = (CharacterBase) caster.Clone(),
                skillInfos = info,
            };
        }
    }
}
