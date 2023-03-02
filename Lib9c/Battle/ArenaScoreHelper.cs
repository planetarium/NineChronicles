using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Model.BattleStatus;

namespace Nekoyume.Battle
{
    public static class ArenaScoreHelper
    {
        private const int DEFAULT_WIN_POINT = 1;
        private const int DEFAULT_LOSE_POINT = -1;

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
        private static readonly IReadOnlyDictionary<int, (int, int)> CachedScoreV1 =
            new Dictionary<int, (int, int)>
            {
                { DifferLowerLimit, (WinScoreMax, LoseScoreMin) },
                { -900, (WinScoreMax, LoseScoreMin) },
                { -800, (WinScoreMax, LoseScoreMin) },
                { -700, (WinScoreMax, LoseScoreMin) },
                { -600, (WinScoreMax, LoseScoreMin) },
                { -500, (50, LoseScoreMin) },
                { -400, (40, -6) },
                { -300, (30, -6) },
                { -200, (25, -8) },
                { -100, (20, -8) },
                { 99, (15, -10) },
                { 199, (8, -10) },
                { 299, (4, -20) },
                { 399, (2, -25) },
                { 499, (1, LoseScoreMax) },
                { 599, (WinScoreMin, LoseScoreMax) },
                { 699, (WinScoreMin, LoseScoreMax) },
                { 799, (WinScoreMin, LoseScoreMax) },
                { 899, (WinScoreMin, LoseScoreMax) },
                { 999, (WinScoreMin, LoseScoreMax) },
                { DifferUpperLimit, (WinScoreMin, LoseScoreMax) },
            };

        /// <summary>
        /// differ: (challenger rate - defender rate)
        /// </summary>
        [Obsolete("Use CachedScore")]
        private static readonly IOrderedEnumerable<(int differ, int winScore, int loseScore)> CachedScoreV2 =
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

        /// <summary>
        /// differ: (challenger rate - defender rate)
        /// </summary>
        [Obsolete("Use CachedScore")]
        private static readonly IOrderedEnumerable<(int differ, int winScore, int defenderLoseScore, int loseScore)>
            CachedScoreV3 = new List<(int differ, int winScore, int defenderLoseScore, int loseScore)>
            {
                (-500, 60, -2, -5),
                (-400, 45, -2, -5),
                (-300, 35, -2, -3),
                (-200, 25, -1, -2),
                (-100, 22, -1, -2),
                (0, 20, -1, -1),
                (100, 15, -1, -2),
                (200, 10, 0, -2),
                (300, 8, 0, -5),
                (400, 4, 0, -5),
                (500, 2, 0, -5),
            }.OrderBy(tuple => tuple.differ);

        /// <summary>
        /// differ: (challenger rate - defender rate)
        /// </summary>
        [Obsolete("Use CachedScore")]
        private static readonly IOrderedEnumerable<(int scoreDiffer, int winPoint, int losePoint, int defenderLosePoint)>
            CachedScoreV4 = new List<(int scoreDiffer, int winPoint, int losePoint, int defenderLosePoint)>
            {
                (-200, 24, -3, -1),
                (-100, 22, -2, -1),
                (0, 20, -1, -1),
                (100, 18, -1, -1),
                (200, 16, -1, -1),
            }.OrderBy(tuple => tuple.scoreDiffer);

        #endregion

        /// <summary>
        /// differ: (challenger rate - defender rate)
        /// </summary>
        private static readonly
            IOrderedEnumerable<(int scoreDiffer, int winPoint, int losePoint, int defenderLosePoint)> CachedScore =
                new List<(int scoreDiffer, int winPoint, int losePoint, int defenderLosePoint)>
                {
                    (-200, 20, -1, -1),
                    (-100, 20, -1, -1),
                    (0, 20, -1, -1),
                    (100, 18, -1, -1),
                    (200, 16, -1, -1),
                }.OrderBy(tuple => tuple.scoreDiffer);

        public static (int challengerScoreDelta, int defenderScoreDelta) GetScore(
            int challengerRating,
            int defenderRating,
            BattleLog.Result result)
        {
            if (challengerRating < 0 ||
                defenderRating < 0 ||
                result == BattleLog.Result.TimeOver)
            {
                return (0, 0);
            }

            var scoreDiffer = challengerRating - defenderRating;
            foreach (var (cachedScoreDiffer, winPoint, losePoint, defenderLosePoint) in CachedScore)
            {
                if (scoreDiffer >= cachedScoreDiffer)
                {
                    continue;
                }

                return result == BattleLog.Result.Win
                    ? (winPoint, defenderLosePoint)
                    : (losePoint, 0);
            }

            return result == BattleLog.Result.Win
                ? (DEFAULT_WIN_POINT, 0)
                : (DEFAULT_LOSE_POINT, 0);
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

        [Obsolete("Use GetScore()")]
        public static int GetScoreV2(int challengerRating, int defenderRating, BattleLog.Result result)
        {
            if (challengerRating < 0 ||
                defenderRating < 0 ||
                result == BattleLog.Result.TimeOver)
            {
                return 0;
            }

            var differ = challengerRating - defenderRating;
            foreach (var (differ2, winScore, loseScore) in CachedScoreV2)
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
        public static (int challengerScore, int defenderScore) GetScoreV3(int challengerRating, int defenderRating, BattleLog.Result result)
        {
            if (challengerRating < 0 ||
                defenderRating < 0 ||
                result == BattleLog.Result.TimeOver)
            {
                return (0, 0);
            }

            var differ = challengerRating - defenderRating;
            foreach (var (differ2, winScore, defenderLoseScore, loseScore) in CachedScoreV3)
            {
                if (differ >= differ2)
                {
                    continue;
                }

                if (result == BattleLog.Result.Win)
                {
                    return (winScore, defenderLoseScore);
                }

                return (loseScore, 0);
            }

            return result == BattleLog.Result.Win
                ? (1, 0)
                : (-5, 0);
        }

        [Obsolete("Use GetScore()")]
        public static (int challengerScoreDelta, int defenderScoreDelta) GetScoreV4(
            int challengerRating,
            int defenderRating,
            BattleLog.Result result)
        {
            if (challengerRating < 0 ||
                defenderRating < 0 ||
                result == BattleLog.Result.TimeOver)
            {
                return (0, 0);
            }

            var scoreDiffer = challengerRating - defenderRating;
            foreach (var (cachedScoreDiffer, winPoint, losePoint, defenderLosePoint) in CachedScoreV4)
            {
                if (scoreDiffer >= cachedScoreDiffer)
                {
                    continue;
                }

                return result == BattleLog.Result.Win
                    ? (winPoint, defenderLosePoint)
                    : (losePoint, 0);
            }

            return result == BattleLog.Result.Win
                ? (DEFAULT_WIN_POINT, 0)
                : (DEFAULT_LOSE_POINT, 0);
        }
    }
}
