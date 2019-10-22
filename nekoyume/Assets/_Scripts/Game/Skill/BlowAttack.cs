using System;
using Nekoyume.Model;
using Nekoyume.TableData;

namespace Nekoyume.Game
{
    [Serializable]
    public class BlowAttack : AttackSkill
    {
        public BlowAttack(SkillSheet.Row skillRow, int power, int chance) : base(skillRow, power, chance)
        {
        }

        public override Model.Skill Use(CharacterBase caster)
        {
            return new Model.BlowAttack((CharacterBase) caster.Clone(), ProcessDamage(caster), ProcessBuff(caster));
        }
    }
}
