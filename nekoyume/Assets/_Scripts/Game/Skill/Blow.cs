using System;
using Nekoyume.Data.Table;
using Nekoyume.Model;

namespace Nekoyume.Game.Skill
{
    [Serializable]
    public class Blow : AttackBase
    {
        public Blow(float chance, SkillEffect effect, Data.Table.Elemental.ElementalType elemental) : base(chance, effect, elemental)
        {
        }

        public override Model.Skill Use()
        {
            var target = GetTarget();
            var info = ProcessDamage(target);

            return new Model.Blow
            {
                character = (CharacterBase) caster.Clone(),
                skillInfos = info,
            };
        }
    }
}
