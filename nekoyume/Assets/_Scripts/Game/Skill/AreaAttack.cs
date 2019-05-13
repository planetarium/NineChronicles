using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Model;

namespace Nekoyume.Game.Skill
{
    [Serializable]
    public class AreaAttack: AttackBase, IMultipleTargetSkill
    {
        public AreaAttack(CharacterBase caster, IEnumerable<CharacterBase> target, int effect) : base(caster, target, effect)
        {
        }

        public override EventBase Use()
        {
            var events = new List<Model.Attack.AttackInfo>();
            foreach (var target in GetTarget())
            {
                var attack = ProcessDamage(target);
                events.Add(attack);
            }

            return new Model.AreaAttack
            {
                character = CharacterBase.Copy(Caster),
                infos = events,
            };
        }

        public List<CharacterBase> GetTarget()
        {
            return Target.ToList();
        }
    }
}
