using System;
using System.Collections.Generic;
using Nekoyume.TableData;

namespace Nekoyume.Model.Skill
{
    [Serializable]
    public class HealSkill : Skill
    {
        public HealSkill(SkillSheet.Row skillRow, int power, int chance) : base(skillRow, power, chance)
        {
        }

        public override BattleStatus.Skill Use(CharacterBase caster, int simulatorWaveTurn)
        {
            return new BattleStatus.HealSkill((CharacterBase)caster.Clone(), ProcessHeal(caster, simulatorWaveTurn), ProcessBuff(caster, simulatorWaveTurn));
        }

        protected IEnumerable<BattleStatus.Skill.SkillInfo> ProcessHeal(CharacterBase caster, int simulatorWaveTurn)
        {
            var infos = new List<BattleStatus.Skill.SkillInfo>();
            var healPoint = caster.ATK + power;
            foreach (var target in skillRow.SkillTargetType.GetTarget(caster))
            {
                target.Heal(healPoint);
                infos.Add(new BattleStatus.Skill.SkillInfo((CharacterBase)target.Clone(), healPoint, caster.IsCritical(false),
                    skillRow.SkillCategory, simulatorWaveTurn));
            }

            return infos;
        }
    }
}
