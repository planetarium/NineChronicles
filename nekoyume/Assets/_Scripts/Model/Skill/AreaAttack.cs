using System;
using Nekoyume.Model;
using Nekoyume.Model.Skill;
using Nekoyume.TableData;

namespace Nekoyume.Game
{
    [Serializable]
    public class AreaAttack : AttackSkill
    {
        public AreaAttack(SkillSheet.Row skillRow, int power, int chance) : base(skillRow, power, chance)
        {
        }

        public override Model.BattleStatus.Skill Use(CharacterBase caster, int simulatorWaveTurn)
        {
            return new Model.BattleStatus.AreaAttack((CharacterBase) caster.Clone(), ProcessDamage(caster, simulatorWaveTurn), ProcessBuff(caster, simulatorWaveTurn));
        }
    }
}
