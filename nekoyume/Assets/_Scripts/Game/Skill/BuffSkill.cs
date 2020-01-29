using System;
using Nekoyume.Model;
using Nekoyume.TableData;

namespace Nekoyume.Game
{
    [Serializable]
    public class BuffSkill : Skill
    {
        public BuffSkill(SkillSheet.Row skillRow, int power, int chance) : base(skillRow, power, chance)
        {
        }

        public override Model.BattleStatus.Skill Use(CharacterBase caster, int simulatorWaveTurn)
        {
            return new Model.BattleStatus.Buff((CharacterBase) caster.Clone(), ProcessBuff(caster, simulatorWaveTurn));
        }
    }
}
