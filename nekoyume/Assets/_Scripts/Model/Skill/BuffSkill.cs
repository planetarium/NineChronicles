using System;
using Nekoyume.Model;
using Nekoyume.TableData;

namespace Nekoyume.Model.Skill
{
    [Serializable]
    public class BuffSkill : Skill
    {
        public BuffSkill(SkillSheet.Row skillRow, int power, int chance) : base(skillRow, power, chance)
        {
        }

        public override BattleStatus.Skill Use(CharacterBase caster, int simulatorWaveTurn)
        {
            return new BattleStatus.Buff((CharacterBase)caster.Clone(), ProcessBuff(caster, simulatorWaveTurn));
        }
    }
}
