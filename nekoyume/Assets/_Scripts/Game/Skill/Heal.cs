using System;
using System.Collections.Generic;
using Nekoyume.Data.Table;
using Nekoyume.Model;

namespace Nekoyume.Game.Skill
{
    [Serializable]
    public class Heal : SkillBase
    {
        public Heal(decimal chance, SkillEffect effect, int power)
            : base(chance, effect, Data.Table.Elemental.ElementalType.Normal, power)
        {
        }

        public override Model.Skill Use(CharacterBase caster)
        {
            var infos = new List<Model.Skill.SkillInfo>();
            foreach (var target in GetTarget(caster))
            {
                target.Heal(power);
                infos.Add(new Model.Skill.SkillInfo(target, power, caster.IsCritical(), effect.category));
            }

            return new Model.Heal
            {
                character = (CharacterBase) caster.Clone(),
                skillInfos = infos,
            };
        }
    }
}
