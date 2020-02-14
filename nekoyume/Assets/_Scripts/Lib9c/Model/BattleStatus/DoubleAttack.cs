using System;
using System.Collections;
using System.Collections.Generic;

namespace Nekoyume.Model.BattleStatus
{
    [Serializable]
    public class DoubleAttack : Skill
    {
        public DoubleAttack(CharacterBase character, IEnumerable<SkillInfo> skillInfos, IEnumerable<SkillInfo> buffInfos)
            : base(character, skillInfos, buffInfos)
        {
        }

        public override IEnumerator CoExecute(IStage stage)
        {
            yield return stage.CoDoubleAttack(Character, SkillInfos, BuffInfos);
        }
    }
}
