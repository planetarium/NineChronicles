using System;
using Nekoyume.Data.Table;
using Nekoyume.Model;

namespace Nekoyume.Game.Skill
{
    [Serializable]
    public class AreaAttack: AttackBase
    {
        public AreaAttack(CharacterBase caster, SkillEffect effect) : base(caster, effect)
        {
        }

        public override EventBase Use()
        {

            return new Model.Attack
            {
                character = CharacterBase.Copy(Caster),
                skillInfos = ProcessDamage(GetTarget()),
            };
        }
    }
}
