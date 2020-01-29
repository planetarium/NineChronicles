using System;
using Nekoyume.Model;
using Nekoyume.TableData;

namespace Nekoyume.Model.Skill
{
    [Serializable]
    public class DoubleAttack : AttackSkill
    {
        public DoubleAttack(SkillSheet.Row skillRow, int power, int chance) : base(skillRow, power, chance)
        {
        }

        public override BattleStatus.Skill Use(CharacterBase caster, int simulatorWaveTurn)
        {
            return new BattleStatus.DoubleAttack((CharacterBase)caster.Clone(), ProcessDamage(caster, simulatorWaveTurn), ProcessBuff(caster, simulatorWaveTurn));
        }
    }
}
