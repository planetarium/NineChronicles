using System;
using System.Collections;

namespace Nekoyume.Model.BattleStatus.Arena
{
    [Serializable]
    public class ArenaRemoveBuffs : ArenaEventBase
    {
        public ArenaRemoveBuffs(ArenaCharacter character) : base(character)
        {
        }

        public override IEnumerator CoExecute(IArena arena)
        {
            yield return arena.CoRemoveBuffs(Character);
        }
    }
}
