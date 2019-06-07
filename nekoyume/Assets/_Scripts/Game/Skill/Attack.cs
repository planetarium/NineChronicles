using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Data.Table;
using Nekoyume.Model;

namespace Nekoyume.Game.Skill
{
    [Serializable]
    public class AttackBase: SkillBase
    {
        private readonly Data.Table.Elemental.ElementalType _elemental;
        protected AttackBase(float chance, SkillEffect effect,
            Data.Table.Elemental.ElementalType elemental) : base(chance, effect)
        {
            this.chance = chance;
            _elemental = elemental;
        }

        protected List<Model.Skill.SkillInfo> ProcessDamage(IEnumerable<CharacterBase> targets)
        {
            var infos = new List<Model.Skill.SkillInfo>();
            var targetList = targets.ToArray();
            var elemental = Elemental.Create(_elemental);
            for (var i = 0; i < Effect.hitCount; i++)
            {
                foreach (var target in targetList)
                {
                    var critical = caster.IsCritical();
                    var dmg = elemental.CalculateDmg(caster.atk, target.defElement);
                    // https://gamedev.stackexchange.com/questions/129319/rpg-formula-attack-and-defense
                    dmg = Math.Max((dmg * dmg) / (dmg + target.def), 1);
                    dmg = Convert.ToInt32(dmg * Effect.multiplier);
                    if (critical)
                    {
                        dmg = Convert.ToInt32(dmg * CharacterBase.CriticalMultiplier);
                    }

                    target.OnDamage(dmg);

                    infos.Add(new Model.Skill.SkillInfo((CharacterBase) target.Clone(), dmg, critical, Effect.category,
                        _elemental));
                }
            }

            return infos;
        }

        public override Model.Skill Use()
        {
            throw new NotImplementedException();
        }
    }

    [Serializable]
    public class Attack : AttackBase
    {
        public Attack(float chance, SkillEffect effect,
            Data.Table.Elemental.ElementalType elemental) : base(chance, effect, elemental)
        {
        }

        public override Model.Skill Use()
        {
            var target = GetTarget();
            var info = ProcessDamage(target);

            return new Model.Attack
            {
                character = (CharacterBase) caster.Clone(),
                skillInfos = info,
            };
        }
    }
}
