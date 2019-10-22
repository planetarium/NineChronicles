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
        public HealSkill(SkillSheet.Row skillRow, int power, decimal chance) : base(skillRow, power, chance)
        {
        }

        public override Model.Skill Use(CharacterBase caster)
        {
            var infos = new List<Model.Skill.SkillInfo>();
            foreach (var target in skillRow.skillTargetType.GetTarget(caster))
            {
                target.Heal(power);
                infos.Add(new Model.Skill.SkillInfo(target, power, caster.IsCritical(), skillRow.skillCategory));
            }

            return new Model.HealSkill((CharacterBase) caster.Clone(), infos, ProcessBuff(caster));
        }
    }
}
