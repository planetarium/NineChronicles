using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Model.BattleStatus;

namespace Nekoyume.Battle
{
    public static class ArenaScoreHelper
    {
        #region Obsolete

        [Obsolete("Only to used for V1")]
        public const int DifferLowerLimit = -1000;

        [Obsolete("Only to used for V1")]
        public const int DifferUpperLimit = 1000;

        [Obsolete("Only to used for V1")]
        public const int WinScoreMin = 1;

        [Obsolete("Only to used for V1")]
        public const int WinScoreMax = 60;

        [Obsolete("Only to used for V1")]
        public const int LoseScoreMin = -5;

        [Obsolete("Only to used for V1")]
        public const int LoseScoreMax = -30;

        /// <summary>
        /// key: differ (challenger rate - defender rate)
        /// value: tuple (win score, lose score)
        /// </summary>
        [Obsolete("Use CachedScore")]
        private static readonly IReadOnlyDictionary<int, (int, int)> CachedScoreV1 = new Dictionary<int, (int, int)>
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

        #endregion

        /// <summary>
        /// differ: (challenger rate - defender rate)
        /// winScore
        /// loseScore
        /// </summary>
        private static readonly IOrderedEnumerable<(int differ, int winScore, int loseScore)> CachedScore =
            new List<(int differ, int winScore, int loseScore)>
            {
                (-500, 60, -5),
                (-400, 50, -5),
                (-300, 40, -6),
                (-200, 30, -6),
                (-100, 25, -8),
                (0, 20, -8),
                (100, 15, -10),
                (200, 15, -10),
                (300, 8, -20),
                (400, 4, -25),
                (500, 2, -30),
            }.OrderBy(tuple => tuple.differ);

        public static int GetScore(int challengerRating, int defenderRating, BattleLog.Result result)
        {
            if (challengerRating < 0 ||
                defenderRating < 0 ||
                result == BattleLog.Result.TimeOver)
            {
                return 0;
            }

            var differ = challengerRating - defenderRating;
            foreach (var (differ2, winScore, loseScore) in CachedScore)
            {
                if (differ >= differ2)
                {
                    continue;
                }

                return result == BattleLog.Result.Win
                    ? winScore
                    : loseScore;
            }

            return result == BattleLog.Result.Win
                ? 1
                : -30;
        }

        [Obsolete("Use GetScore()")]
        public static int GetScoreV1(int challengerRating, int defenderRating, BattleLog.Result result)
        {
            if (result == BattleLog.Result.TimeOver)
            {
                return 0;
            }

            var differ = challengerRating - defenderRating;
            if (differ < 0)
            {
                foreach (var pair in CachedScoreV1.Where(pair => pair.Key < 0).OrderBy(kv => kv.Key))
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

            foreach (var pair in CachedScoreV1.Where(pair => pair.Key >= 0).OrderBy(kv => kv.Key))
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
