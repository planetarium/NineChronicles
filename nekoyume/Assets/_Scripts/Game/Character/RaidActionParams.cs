using System;
using System.Collections;
using System.Collections.Generic;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.BattleStatus.Arena;

namespace Nekoyume.Game.Character
{
    public class RaidActionParams
    {
        public readonly RaidCharacter RaidCharacter;
        public readonly int SkillId;
        public readonly IEnumerable<Skill.SkillInfo> SkillInfos;
        public readonly IEnumerable<Skill.SkillInfo> BuffInfos;
        public readonly Func<IReadOnlyList<Skill.SkillInfo>, IEnumerator> ActionCoroutine;

        public RaidActionParams(RaidCharacter raidCharacter,
            int skillId,
            IEnumerable<Skill.SkillInfo> skills,
            IEnumerable<Skill.SkillInfo> buffs,
            Func<IReadOnlyList<Skill.SkillInfo>, IEnumerator> actionCoroutine)
        {
            RaidCharacter = raidCharacter;
            SkillId = skillId;
            SkillInfos = skills;
            BuffInfos = buffs;
            ActionCoroutine = actionCoroutine;
        }
    }
}
