using System;
using Nekoyume.Model;
using Nekoyume.TableData;

namespace Nekoyume.Game
{
    [Serializable]
    public class NormalAttack : AttackSkill
    {
        public NormalAttack(SkillSheet.Row skillRow, int power, int chance) : base(skillRow, power, chance)
        {
        }

        public override Model.Skill Use(CharacterBase caster, int simulatorWaveTurn)
        {
            return new Model.NormalAttack((CharacterBase) caster.Clone(), ProcessDamage(caster, simulatorWaveTurn), ProcessBuff(caster, simulatorWaveTurn));
        }
    }
}
