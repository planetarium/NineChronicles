using System;
using Nekoyume.Data.Table;
using Nekoyume.Model;

namespace Nekoyume.Game.Skill
{
    [Serializable]
    public class Blow : AttackBase
    {
        public Blow(CharacterBase caster, float chance, SkillEffect effect, Data.Table.Elemental.ElementalType elemental) : base(caster, chance, effect, elemental)
        {
        }

        public override Model.Skill Use()
        {
            var target = GetTarget();
            var info = ProcessDamage(target);

            return new Model.Blow
            {
                character = (CharacterBase) Caster.Clone(),
                skillInfos = info,
            };
        }
    }
}
