using System;
using System.Collections;
using System.Collections.Generic;
using Nekoyume.Model.BattleStatus.Arena;

namespace Nekoyume.Game.Character
{
    public class ArenaActionParams
    {
        public readonly ArenaCharacter ArenaCharacter;
        public readonly IEnumerable<ArenaSkill.ArenaSkillInfo> skillInfos;
        public readonly IEnumerable<ArenaSkill.ArenaSkillInfo> buffInfos;
        public readonly Func<IReadOnlyList<ArenaSkill.ArenaSkillInfo>, IEnumerator> func;

        public ArenaActionParams(ArenaCharacter arenaCharacter,
            IEnumerable<ArenaSkill.ArenaSkillInfo> skills,
            IEnumerable<ArenaSkill.ArenaSkillInfo> buffs,
            Func<IReadOnlyList<ArenaSkill.ArenaSkillInfo>, IEnumerator> coNormalAttack)
        {
            ArenaCharacter = arenaCharacter;
            skillInfos = skills;
            buffInfos = buffs;
            func = coNormalAttack;
        }
    }
}
