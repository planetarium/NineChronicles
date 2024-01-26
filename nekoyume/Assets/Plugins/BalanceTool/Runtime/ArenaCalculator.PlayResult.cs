using System.Collections.Generic;

namespace BalanceTool
{
    public static partial class ArenaCalculator
    {
        public readonly struct PlayResult
        {
            public readonly int ArenaResultWin;
            public readonly int ArenaResultLose;

            /// <summary>
            /// Key: Item ID, Value: Count.
            /// </summary>
            public readonly IReadOnlyDictionary<int, int> TotalRewards;

            public PlayResult(
                int arenaResultWin = 0,
                int arenaResultLose = 0,
                IReadOnlyDictionary<int, int> totalRewards = null)
            {
                ArenaResultWin = arenaResultWin;
                ArenaResultLose = arenaResultLose;
                TotalRewards = totalRewards ?? new Dictionary<int, int>();
            }
        }
    }
}
