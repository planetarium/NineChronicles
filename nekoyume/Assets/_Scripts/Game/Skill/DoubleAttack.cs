using System;
using System.Linq;
using Nekoyume.Data.Table;
using Nekoyume.Model;

namespace Nekoyume.Game.Skill
{
    [Serializable]
    public class DoubleAttack : AttackBase
    {
        public DoubleAttack(float chance, SkillEffect effect,
            Data.Table.Elemental.ElementalType elemental) : base(chance, effect, elemental)
        {
        }

        public override Model.Skill Use()
        {
            var target = GetTarget().ToArray();
            var infos = ProcessDamage(target);
            infos.AddRange(ProcessDamage(target));
            return new Model.DoubleAttack
            {
                character = (CharacterBase) caster.Clone(),
                skillInfos = infos,
            };
        }
    }
}
