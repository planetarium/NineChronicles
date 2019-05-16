using System;
using System.Collections.Generic;
using Nekoyume.Data.Table;
using Nekoyume.Model;

namespace Nekoyume.Game.Skill
{
    [Serializable]
    public class Heal : SkillBase
    {
        public Heal(CharacterBase caster, SkillEffect effect) : base(caster, effect)
        {
        }



        public override EventBase Use()
        {
            var type = GetSkillType();
            var infos = new List<Model.Skill.SkillInfo>();
            foreach (var target in GetTarget())
            {
                var maxHp = target.hp;
                var healHp = Convert.ToInt32(maxHp * Effect.multiplier);
                target.Heal(healHp);
                infos.Add(new Model.Skill.SkillInfo(target, healHp, Caster.IsCritical()));
            }

            return new Model.Heal
            {
                character = CharacterBase.Copy(Caster),
                skillInfos = infos,
            };
        }
    }
}
