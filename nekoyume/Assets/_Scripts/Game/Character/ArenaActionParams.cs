using System;
using System.Collections;
using System.Collections.Generic;

namespace Nekoyume.Game.Character
{
    public class ArenaActionParams
    {
        public readonly ArenaCharacter ArenaCharacter;
        public readonly IEnumerable<Model.BattleStatus.Skill.SkillInfo> skillInfos;
        public readonly IEnumerable<Model.BattleStatus.Skill.SkillInfo> buffInfos;
        public readonly Func<IReadOnlyList<Model.BattleStatus.Skill.SkillInfo>, IEnumerator> func;

        public ArenaActionParams(ArenaCharacter arenaCharacter,
            IEnumerable<Model.BattleStatus.Skill.SkillInfo> skills,
            IEnumerable<Model.BattleStatus.Skill.SkillInfo> buffs,
            Func<IReadOnlyList<Model.BattleStatus.Skill.SkillInfo>, IEnumerator> coNormalAttack)
        {
            ArenaCharacter = arenaCharacter;
            skillInfos = skills;
            buffInfos = buffs;
            func = coNormalAttack;
        }
    }
}
