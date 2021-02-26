using System.Collections.Generic;

namespace Nekoyume.Battle
{
    public static class StageRewardExpHelper
    {
        public const int DifferLowerLimit = -15;
        public const int DifferUpperLimit = 10;
        public const int RewardExpMin = 0;
        public const int RewardExpMax = 300;

        /// <summary>
        /// key: differ (stage number - character level)
        /// value: exp
        /// </summary>
        public static readonly IReadOnlyDictionary<int, int> CachedExp = new Dictionary<int, int>
        {
            {DifferLowerLimit, RewardExpMax},
            {-14, 300},
            {-13, 300},
            {-12, 300},
            {-11, 250},
            {-10, 200},
            {-9, 150},
            {-8, 100},
            {-7, 80},
            {-6, 60},
            {-5, 50},
            {-4, 40},
            {-3, 30},
            {-2, 30},
            {-1, 15},
            {0, 15},
            {1, 8},
            {2, 4},
            {3, 2},
            {4, 1},
            {5, 1},
            {6, 1},
            {7, 1},
            {8, 1},
            {9, 1},
            {DifferUpperLimit, RewardExpMin},
        };

        public static int GetExp(int characterLevel, int stageNumber)
        {
            if (stageNumber >= GameConfig.MimisbrunnrStartStageId)
            {
                return 0;
            }
                
            var differ = characterLevel - stageNumber;
            if (differ <= DifferLowerLimit)
            {
                return RewardExpMax;
            }

            if (differ >= DifferUpperLimit)
            {
                return RewardExpMin;
            }

            if (!CachedExp.TryGetValue(differ, out var exp))
            {
                throw new KeyNotFoundException($"[{nameof(StageRewardExpHelper)}] {nameof(CachedExp)} not contains {differ}");
            }

            return exp;
        }
    }
}
