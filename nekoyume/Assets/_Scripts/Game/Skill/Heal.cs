using System;
using System.Collections.Generic;
using Nekoyume.Data.Table;
using Nekoyume.Model;

namespace Nekoyume.Game.Skill
{
    [Serializable]
    public class Heal : SkillBase
    {
        public Heal(float chance, SkillEffect effect) : base(chance, effect)
        {
        }

        public override Model.Skill Use(CharacterBase caster)
        {
            var infos = new List<Model.Skill.SkillInfo>();
            foreach (var target in GetTarget(caster))
            {
                var maxHp = target.hp;
                var healHp = Convert.ToInt32(maxHp * effect.multiplier);
                target.Heal(healHp);
                infos.Add(new Model.Skill.SkillInfo(target, healHp, caster.IsCritical(), effect.category));
            }

            return new Model.Heal
            {
                character = (CharacterBase) caster.Clone(),
                skillInfos = infos,
            };
        }
    }
}
