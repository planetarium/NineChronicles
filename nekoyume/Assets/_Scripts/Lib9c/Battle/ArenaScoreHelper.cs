using System.Collections.Generic;
using System.Linq;
using Nekoyume.Model.BattleStatus;

namespace Nekoyume.Battle
{
    public class ArenaScoreHelper
    {
        public const int DifferLowerLimit = -1000;
        public const int DifferUpperLimit = 1000;
        public const int WinScoreMin = 1;
        public const int WinScoreMax = 60;
        public const int LoseScoreMin = -5;
        public const int LoseScoreMax = -30;

        /// <summary>
        /// key: differ (challenger rate - defender rate)
        /// value: exp
        /// </summary>
        public static readonly IReadOnlyDictionary<int, (int, int)> CachedScore = new Dictionary<int, (int, int)>
        {
            {DifferLowerLimit, (WinScoreMax, LoseScoreMin)},
            {-900, (WinScoreMax, LoseScoreMin)},
            {-800, (WinScoreMax, LoseScoreMin)},
            {-700, (WinScoreMax, LoseScoreMin)},
            {-600, (WinScoreMax, LoseScoreMin)},
            {-500, (50, LoseScoreMin)},
            {-400, (40, -6)},
            {-300, (30, -6)},
            {-200, (25, -8)},
            {-100, (20, -8)},
            {99, (15, -10)},
            {199, (8, -10)},
            {299, (4, -20)},
            {399, (2, -25)},
            {499, (1, LoseScoreMax)},
            {599, (WinScoreMin, LoseScoreMax)},
            {699, (WinScoreMin, LoseScoreMax)},
            {799, (WinScoreMin, LoseScoreMax)},
            {899, (WinScoreMin, LoseScoreMax)},
            {999, (WinScoreMin, LoseScoreMax)},
            {DifferUpperLimit, (WinScoreMin, LoseScoreMax)},
        };

        public static int GetScore(int challengerRating, int defenderRating, BattleLog.Result result)
        {
            var differ = challengerRating - defenderRating;
            if (differ < 0)
            {
                foreach (var pair in CachedScore.Where(pair => pair.Key < 0))
                {
                    if (differ < pair.Key)
                    {
                        continue;
                    }

                    return result == BattleLog.Result.Win
                        ? pair.Value.Item1
                        : pair.Value.Item2;
                }

                return result == BattleLog.Result.Win
                    ? WinScoreMax
                    : LoseScoreMin;
            }

            foreach (var pair in CachedScore.Where(pair => pair.Key >= 0))
            {
                if (differ > pair.Key)
                {
                    continue;
                }

                return result == BattleLog.Result.Win
                    ? pair.Value.Item1
                    : pair.Value.Item2;
            }

            return result == BattleLog.Result.Win
                ? WinScoreMin
                : LoseScoreMax;
        }
    }
}
