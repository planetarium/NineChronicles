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

        public override Model.Skill Use(CharacterBase caster)
        {
            return new Model.HealSkill((CharacterBase) caster.Clone(), ProcessHeal(caster), ProcessBuff(caster));
        }

        protected IEnumerable<Model.Skill.SkillInfo> ProcessHeal(CharacterBase caster)
        {
            var infos = new List<Model.Skill.SkillInfo>();
            var healPoint = caster.ATK + power;
            foreach (var target in skillRow.SkillTargetType.GetTarget(caster))
            {
                target.Heal(healPoint);
                infos.Add(new Model.Skill.SkillInfo((CharacterBase) target.Clone(), healPoint, caster.IsCritical(),
                    skillRow.SkillCategory));
            }

            return infos;
        }
    }
}
