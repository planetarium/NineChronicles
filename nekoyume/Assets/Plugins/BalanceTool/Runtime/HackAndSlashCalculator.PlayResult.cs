using System.Collections.Generic;

namespace BalanceTool
{
    public static partial class HackAndSlashCalculator
    {
        public readonly struct PlayResult
        {
            /// <summary>
            /// Key: Wave number(1..), Value: Count.
            /// </summary>
            public readonly IReadOnlyDictionary<int, int> ClearedWaves;

            /// <summary>
            /// Key: Item ID, Value: Count.
            /// </summary>
            public readonly IReadOnlyDictionary<int, int> TotalRewards;

            public readonly int TotalExp;

            public PlayResult(
                IReadOnlyDictionary<int, int> clearedWaves = null,
                IReadOnlyDictionary<int, int> totalRewards = null,
                int totalExp = 0)
            {
                ClearedWaves = clearedWaves ?? new Dictionary<int, int>();
                TotalRewards = totalRewards ?? new Dictionary<int, int>();
                TotalExp = totalExp;
            }
        }
    }
}
