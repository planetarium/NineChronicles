using System;
using System.Collections.Generic;
using Nekoyume.TableData;

namespace Nekoyume.Model.Skill
{
    [Serializable]
    public class BuffRemovalAttack : AttackSkill
    {
        public BuffRemovalAttack(SkillSheet.Row skillRow, int power, int chance) : base(skillRow, power, chance)
        {
        }

        public override BattleStatus.Skill Use(
            CharacterBase caster, 
            int simulatorWaveTurn,
            IEnumerable<Buff.Buff> buffs)
        {
            var clone = (CharacterBase) caster.Clone();
            var damage = ProcessDamage(caster, simulatorWaveTurn);
            var buff = ProcessBuff(caster, simulatorWaveTurn, buffs);
            var targets = SkillRow.SkillTargetType.GetTarget(caster);
            foreach (var target in targets)
            {
                target.RemoveRecentStatBuff();
            }

            return new Model.BattleStatus.BuffRemovalAttack(SkillRow.Id, clone, damage, buff);
        }
    }
}
