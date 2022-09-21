using System.Collections.Generic;
using Nekoyume.Model;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.Item;
using Priority_Queue;

namespace Nekoyume.Battle
{
    public interface ISimulator
    {
        Player Player { get; }
        BattleLog Log { get; }
        SimplePriorityQueue<CharacterBase, decimal> Characters { get; }
        IEnumerable<ItemBase> Reward { get; }
        int WaveNumber { get; }
        int WaveTurn { get; }
    }
}
