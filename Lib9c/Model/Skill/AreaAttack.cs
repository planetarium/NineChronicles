using System;
using System.Collections.Generic;
using Nekoyume.TableData;

namespace Nekoyume.Model.Skill
{
    [Serializable]
    public class AreaAttack : AttackSkill
    {
        public AreaAttack(SkillSheet.Row skillRow, int power, int chance) : base(skillRow, power, chance)
        {
        }

        public override Model.BattleStatus.Skill Use(
            CharacterBase caster, 
            int simulatorWaveTurn, 
            IEnumerable<Buff.Buff> buffs)
        {
            var clone = (CharacterBase) caster.Clone();
            var damage = ProcessDamage(caster, simulatorWaveTurn);
            var buff = ProcessBuff(caster, simulatorWaveTurn, buffs);

            return new Model.BattleStatus.AreaAttack(SkillRow.Id, clone, damage, buff);
        }
    }
}
