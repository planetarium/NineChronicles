using System;
using System.Collections;
using System.Collections.Generic;

namespace Nekoyume.Model.BattleStatus
{
    [Serializable]
    public class NormalAttack : Skill
    {
        public NormalAttack(CharacterBase character, IEnumerable<SkillInfo> skillInfos,
            IEnumerable<SkillInfo> buffInfos) : base(character, skillInfos, buffInfos)
        {
        }

        public override IEnumerator CoExecute(IStage stage)
        {
            yield return stage.CoNormalAttack(Character, SkillInfos, BuffInfos);
        }
    }
}
