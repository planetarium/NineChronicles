using System;
using Nekoyume.TableData;

namespace Nekoyume.Model.Skill
{
    [Serializable]
    public class BlowAttack : AttackSkill
    {
        public BlowAttack(SkillSheet.Row skillRow, int power, int chance) : base(skillRow, power, chance)
        {
        }

        public override BattleStatus.Skill Use(CharacterBase caster, int simulatorWaveTurn)
        {
            return new BattleStatus.BlowAttack((CharacterBase)caster.Clone(), ProcessDamage(caster, simulatorWaveTurn), ProcessBuff(caster, simulatorWaveTurn));
        }
    }
}
