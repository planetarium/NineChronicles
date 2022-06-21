using System;
using System.Collections;
using System.Collections.Generic;

namespace Nekoyume.Model.BattleStatus.Arena
{
    [Serializable]
    public class ArenaBuff : ArenaSkill
    {
        public ArenaBuff(ArenaCharacter character, IEnumerable<ArenaSkillInfo> skillInfos)
            : base(character, skillInfos, null)
        {
        }

        public override IEnumerator CoExecute(IArena arena)
        {
            yield return arena.CoBuff(Character, SkillInfos, BuffInfos);
        }
    }
}
