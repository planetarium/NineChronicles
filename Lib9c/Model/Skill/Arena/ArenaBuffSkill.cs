using System;
using System.Collections.Generic;
using Nekoyume.TableData;

namespace Nekoyume.Model.Skill.Arena
{
    [Serializable]
    public class ArenaBuffSkill : ArenaSkill
    {
        public ArenaBuffSkill(SkillSheet.Row skillRow, int power, int chance)
            : base(skillRow, power, chance)
        {
        }

        public override BattleStatus.Arena.ArenaSkill Use(
            ArenaCharacter caster,
            ArenaCharacter target,
            int turn,
            IEnumerable<Buff.Buff> buffs)
        {
            var clone = (ArenaCharacter)caster.Clone();
            var buff = ProcessBuff(caster, target, turn, buffs);

            return new BattleStatus.Arena.ArenaBuff(clone, buff);
        }

        [Obsolete("Use Use")]
        public override BattleStatus.Arena.ArenaSkill UseV1(
            ArenaCharacter caster,
            ArenaCharacter target,
            int turn,
            IEnumerable<Buff.Buff> buffs)
        {
            var clone = (ArenaCharacter)caster.Clone();
            var buff = ProcessBuffV1(caster, target, turn, buffs);

            return new BattleStatus.Arena.ArenaBuff(clone, buff);
        }
    }
}
