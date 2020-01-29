using System;
using System.Collections;
using System.Collections.Generic;

namespace Nekoyume.Model.BattleStatus
{
    [Serializable]
    public class BlowAttack : Skill
    {
        public BlowAttack(CharacterBase character, IEnumerable<SkillInfo> skillInfos, IEnumerable<SkillInfo> buffInfos)
            : base(character, skillInfos, buffInfos)
        {
        }

        public override IEnumerator CoExecute(IStage stage)
        {
            yield return stage.CoBlowAttack(Character, SkillInfos, BuffInfos);
        }
    }
}
