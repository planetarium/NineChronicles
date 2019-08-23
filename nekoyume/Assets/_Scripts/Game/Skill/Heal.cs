using System;
using System.Collections.Generic;
using Nekoyume.Model;
using Nekoyume.TableData;

namespace Nekoyume.Game
{
    [Serializable]
    public class Heal : Skill
    {
        public Heal(SkillSheet.Row skillRow, int power, decimal chance) : base(skillRow, power, chance)
        {
        }

        public override Model.Skill Use(CharacterBase caster)
        {
            var infos = new List<Model.Skill.SkillInfo>();
            foreach (var target in GetTarget(caster))
            {
                target.Heal(power);
                infos.Add(new Model.Skill.SkillInfo(target, power, caster.IsCritical(), effect.skillCategory));
            }

            return new Model.Heal
            {
                character = (CharacterBase) caster.Clone(),
                skillInfos = infos,
            };
        }
    }
}
