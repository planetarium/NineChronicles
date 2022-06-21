using System.Collections;

namespace Nekoyume.Model.BattleStatus.Arena
{
    public class ArenaSpawnCharacter : ArenaEventBase
    {
        public ArenaSpawnCharacter(ArenaCharacter character) : base(character)
        {
        }

        public override IEnumerator CoExecute(IArena arena)
        {
            yield return arena.CoSpawnCharacter(Character);
        }
    }
}
