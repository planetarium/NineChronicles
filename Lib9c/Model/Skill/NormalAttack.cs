using System;
using System.Collections.Generic;
using Nekoyume.Model.Stat;
using Nekoyume.TableData;

namespace Nekoyume.Model.Skill
{
    [Serializable]
    public class NormalAttack : AttackSkill
    {
        public NormalAttack(
            SkillSheet.Row skillRow,
            int power,
            int chance,
            int statPowerRatio,
            StatType referencedStatType) : base(skillRow, power, chance, statPowerRatio, referencedStatType)
        {
        }

        public override BattleStatus.Skill Use(CharacterBase caster,
            int simulatorWaveTurn,
            IEnumerable<Buff.Buff> buffs, bool copyCharacter)
        {
            var clone = copyCharacter ? (CharacterBase) caster.Clone() : null;
            var damage = ProcessDamage(caster, simulatorWaveTurn, true, copyCharacter);
            var buff = ProcessBuff(caster, simulatorWaveTurn, buffs, copyCharacter);

            return new Model.BattleStatus.NormalAttack(SkillRow.Id, clone, damage, buff);
        }
    }
}
