using System;
using System.Collections.Generic;
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
            var infos = new List<Model.Skill.SkillInfo>();
            var healPoint = caster.ATK + power;
            foreach (var target in skillRow.SkillTargetType.GetTarget(caster))
            {
                target.Heal(healPoint);
                infos.Add(new Model.Skill.SkillInfo(target, healPoint, caster.IsCritical(), skillRow.SkillCategory));
            }

            return new Model.HealSkill((CharacterBase) caster.Clone(), infos, ProcessBuff(caster));
        }
    }
}
