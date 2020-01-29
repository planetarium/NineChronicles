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

        public override Model.BattleStatus.Skill Use(CharacterBase caster, int simulatorWaveTurn)
        {
            return new Model.BattleStatus.BlowAttack((CharacterBase) caster.Clone(), ProcessDamage(caster, simulatorWaveTurn), ProcessBuff(caster, simulatorWaveTurn));
        }
    }
}
