using System;
using System.Collections;
using System.Collections.Generic;

namespace Nekoyume.Model.BattleStatus
{
    [Serializable]
    public class SpecialAttack : Skill
    {
        public int SkillId;

        public SpecialAttack(CharacterBase character, int skillId, IEnumerable<SkillInfo> skillInfos,
            IEnumerable<SkillInfo> buffInfos) : base(character, skillInfos, buffInfos)
        {
            SkillId = skillId;
        }

        public override IEnumerator CoExecute(IStage stage)
        {
            yield return stage.CoSpecialAttack(Character, SkillId, SkillInfos, BuffInfos);
        }
    }
}
