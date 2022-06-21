using System;
using System.Collections;

namespace Nekoyume.Model.BattleStatus.Arena
{
    [Serializable]
    public class ArenaDead : ArenaEventBase
    {
        public ArenaDead(ArenaCharacter character) : base(character)
        {
        }

        public override IEnumerator CoExecute(IArena arena)
        {
            yield return arena.CoDead(Character);
        }
    }
}
