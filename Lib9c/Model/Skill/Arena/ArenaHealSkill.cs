using System;
using System.Collections.Generic;
using Nekoyume.Model.Stat;
using Nekoyume.TableData;

namespace Nekoyume.Model.Skill.Arena
{
    [Serializable]
    public class ArenaHealSkill : ArenaSkill
    {
        public ArenaHealSkill(
            SkillSheet.Row skillRow,
            int power,
            int chance,
            int statPowerRatio,
            StatType referencedStatType) : base(skillRow, power, chance, statPowerRatio, referencedStatType)
        {
        }

        public override BattleStatus.Arena.ArenaSkill Use(
            ArenaCharacter caster,
            ArenaCharacter target,
            int turn,
            IEnumerable<Buff.Buff> buffs)
        {
            var clone = (ArenaCharacter)caster.Clone();
            var heal = ProcessHeal(caster, turn);
            var buff = ProcessBuff(caster, target, turn, buffs);

            return new BattleStatus.Arena.ArenaHeal(clone, heal, buff);
        }

        [Obsolete("Use Use")]
        public override BattleStatus.Arena.ArenaSkill UseV1(
            ArenaCharacter caster,
            ArenaCharacter target,
            int turn,
            IEnumerable<Buff.Buff> buffs)
        {
            var clone = (ArenaCharacter)caster.Clone();
            var heal = ProcessHeal(caster, turn);
            var buff = ProcessBuffV1(caster, target, turn, buffs);

            return new BattleStatus.Arena.ArenaHeal(clone, heal, buff);
        }

        private IEnumerable<BattleStatus.Arena.ArenaSkill.ArenaSkillInfo> ProcessHeal(
            ArenaCharacter caster,
            int turn)
        {
            var infos = new List<BattleStatus.Arena.ArenaSkill.ArenaSkillInfo>();

            // Apply stat power ratio
            var powerMultiplier = StatPowerRatio / 10000m;
            var statAdditionalPower = ReferencedStatType != StatType.NONE ?
                (int)(caster.Stats.GetStat(ReferencedStatType) * powerMultiplier) : default;

            var healPoint = caster.ATK + Power + statAdditionalPower;
            caster.Heal(healPoint);

            infos.Add(new BattleStatus.Arena.ArenaSkill.ArenaSkillInfo(
                (ArenaCharacter)caster.Clone(),
                healPoint,
                caster.IsCritical(false),
                SkillRow.SkillCategory,
                turn));

            return infos;
        }
    }
}
