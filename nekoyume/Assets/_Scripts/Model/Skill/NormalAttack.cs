using System;
using System.Collections.Generic;
using Nekoyume.TableData;

namespace Nekoyume.Model.Skill
{
    [Serializable]
    public class NormalAttack : AttackSkill
    {
        public NormalAttack(SkillSheet.Row skillRow, int power, int chance) : base(skillRow, power, chance)
        {
        }

        public override Model.BattleStatus.Skill Use(
            CharacterBase caster,
            int simulatorWaveTurn,
            IEnumerable<Buff.Buff> buffs)
        {
            return new Model.BattleStatus.NormalAttack((CharacterBase) caster.Clone(),
                ProcessDamage(caster, simulatorWaveTurn, true), ProcessBuff(caster, simulatorWaveTurn, buffs));
        }
    }
}
