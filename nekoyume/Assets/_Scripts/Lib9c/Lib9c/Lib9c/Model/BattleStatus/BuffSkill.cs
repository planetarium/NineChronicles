using System;
using System.Collections;
using System.Collections.Generic;

namespace Nekoyume.Model.BattleStatus
{
    [Serializable]
    public class Buff : Skill
    {
        public Buff(CharacterBase character, IEnumerable<SkillInfo> skillInfos) : base(character, skillInfos, null)
        {
        }

        public override IEnumerator CoExecute(IStage stage)
        {
            yield return stage.CoBuff(Character, SkillInfos, BuffInfos);
        }
    }
}
