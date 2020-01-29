using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.EnumType;
using Nekoyume.Model;
using Nekoyume.TableData;

namespace Nekoyume.Game
{
    [Serializable]
    public class HealSkill : Skill
    {
        public HealSkill(SkillSheet.Row skillRow, int power, int chance) : base(skillRow, power, chance)
        {
        }

        public override Model.BattleStatus.Skill Use(CharacterBase caster, int simulatorWaveTurn)
        {
            return new Model.BattleStatus.HealSkill((CharacterBase) caster.Clone(), ProcessHeal(caster, simulatorWaveTurn), ProcessBuff(caster, simulatorWaveTurn));
        }

        protected IEnumerable<Model.BattleStatus.Skill.SkillInfo> ProcessHeal(CharacterBase caster, int simulatorWaveTurn)
        {
            var infos = new List<Model.BattleStatus.Skill.SkillInfo>();
            var healPoint = caster.ATK + power;
            foreach (var target in skillRow.SkillTargetType.GetTarget(caster))
            {
                target.Heal(healPoint);
                infos.Add(new Model.BattleStatus.Skill.SkillInfo((CharacterBase) target.Clone(), healPoint, caster.IsCritical(),
                    skillRow.SkillCategory, simulatorWaveTurn));
            }

            return infos;
        }
    }
}
