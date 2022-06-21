using System;
using System.Collections;

namespace Nekoyume.Model.BattleStatus.Arena
{
    [Serializable]
    public class ArenaTurnEnd : ArenaEventBase
    {
        public readonly int TurnNumber;

        public ArenaTurnEnd(ArenaCharacter character,  int turnNumber) : base(character)
        {
            TurnNumber = turnNumber;
        }

        public override IEnumerator CoExecute(IArena arena)
        {
            yield return arena.CoTurnEnd(TurnNumber);
        }
    }
}
