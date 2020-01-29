using System;
using Nekoyume.Model;
using Nekoyume.TableData;

namespace Nekoyume.Game
{
    [Serializable]
    public class DoubleAttack : AttackSkill
    {
        public DoubleAttack(SkillSheet.Row skillRow, int power, int chance) : base(skillRow, power, chance)
        {
        }

        public override Model.Skill Use(CharacterBase caster, int simulatorWaveTurn)
        {
            return new Model.DoubleAttack((CharacterBase) caster.Clone(), ProcessDamage(caster, simulatorWaveTurn), ProcessBuff(caster, simulatorWaveTurn));
        }
    }
}
