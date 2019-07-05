using System;
using System.Collections.Generic;
using Nekoyume.Data.Table;
using Nekoyume.Model;

namespace Nekoyume.Game.Skill
{
    [Serializable]
    public class Heal : SkillBase
    {
        public Heal(float chance, SkillEffect effect, int value)
            : base(chance, effect, Data.Table.Elemental.ElementalType.Normal, value)
        {
        }

        public override Model.Skill Use(CharacterBase caster)
        {
            var infos = new List<Model.Skill.SkillInfo>();
            foreach (var target in GetTarget(caster))
            {
                target.Heal(value);
                infos.Add(new Model.Skill.SkillInfo(target, value, caster.IsCritical(), effect.category));
            }

            return new Model.Heal
            {
                character = (CharacterBase) caster.Clone(),
                skillInfos = infos,
            };
        }
    }
}
