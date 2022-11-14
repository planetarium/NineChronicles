
using Libplanet.Action;
using Nekoyume.Model.BattleStatus.Arena;

namespace Nekoyume.Arena
{
    public interface IArenaSimulator
    {
        public ArenaLog Log { get; }
        public IRandom Random { get; }
        public int Turn { get; }
    }
}
