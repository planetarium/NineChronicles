using System;
using System.Collections.Generic;
using Nekoyume.Model.Stat;
using Nekoyume.TableData;

namespace Nekoyume.Model.Skill
{
    [Serializable]
    public class HealSkill : Skill
    {
        public HealSkill(
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
            var heal = ProcessHeal(caster, simulatorWaveTurn, copyCharacter);
            var buff = ProcessBuff(caster, simulatorWaveTurn, buffs, copyCharacter);

            return new BattleStatus.HealSkill(SkillRow.Id, clone, heal, buff);
        }

        protected IEnumerable<BattleStatus.Skill.SkillInfo> ProcessHeal(CharacterBase caster,
            int simulatorWaveTurn, bool copyCharacter)
        {
            var infos = new List<BattleStatus.Skill.SkillInfo>();

            // Apply stat power ratio
            var powerMultiplier = StatPowerRatio / 10000m;
            var statAdditionalPower = ReferencedStatType != StatType.NONE ?
                (int)(caster.Stats.GetStat(ReferencedStatType) * powerMultiplier) : default;

            var healPoint = caster.ATK + Power + statAdditionalPower;
            foreach (var target in SkillRow.SkillTargetType.GetTarget(caster))
            {
                target.Heal(healPoint);
                infos.Add(new BattleStatus.Skill.SkillInfo(target.Id, target.IsDead, target.Thorn, healPoint, caster.IsCritical(false),
                    SkillRow.SkillCategory, simulatorWaveTurn, target: copyCharacter ? (CharacterBase)target.Clone() : target));
            }

            return infos;
        }
    }
}
