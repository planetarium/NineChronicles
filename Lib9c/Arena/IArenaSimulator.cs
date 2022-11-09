
using Libplanet.Action;

namespace Nekoyume.Arena
{
    public interface IArenaSimulator
    {
        public IRandom Random { get; }
        public int Turn { get; }
    }
}
