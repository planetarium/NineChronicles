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

        public override BattleStatus.Skill Use(
            CharacterBase caster, 
            int simulatorWaveTurn,
            IEnumerable<Buff.Buff> buffs)
        {
            var clone = (CharacterBase) caster.Clone();
            var heal = ProcessHeal(caster, simulatorWaveTurn);
            var buff = ProcessBuff(caster, simulatorWaveTurn, buffs);
            
            return new BattleStatus.HealSkill(SkillRow.Id, clone, heal, buff);
        }

        protected IEnumerable<BattleStatus.Skill.SkillInfo> ProcessHeal(CharacterBase caster, int simulatorWaveTurn)
        {
            var infos = new List<BattleStatus.Skill.SkillInfo>();
            var healPoint = caster.ATK + Power;
            foreach (var target in SkillRow.SkillTargetType.GetTarget(caster))
            {
                target.Heal(healPoint);
                infos.Add(new BattleStatus.Skill.SkillInfo((CharacterBase)target.Clone(), healPoint, caster.IsCritical(false),
                    SkillRow.SkillCategory, simulatorWaveTurn));
            }

            return infos;
        }
    }
}
