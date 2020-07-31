using System;
using System.Collections.Generic;

namespace Nekoyume.Battle
{
    public static class AttackCountHelper
    {
        public struct Info
        {
            public decimal DamageMultiplier;
            public decimal AdditionalCriticalChance;
        }

        public const int CountMaxLowerLimit = 2;
        public const int CountMaxUpperLimit = 5;

        /// <summary>
        /// key: attack count max
        /// value: attack count, info
        /// </summary>
        public static readonly IReadOnlyDictionary<int, IReadOnlyDictionary<int, Info>> CachedInfo =
            new Dictionary<int, IReadOnlyDictionary<int, Info>>
            {
                {
                    1, new Dictionary<int, Info>
                    {
                        {1, new Info {DamageMultiplier = 1m, AdditionalCriticalChance = 0m}}
                    }
                },
                {
                    2, new Dictionary<int, Info>
                    {
                        {1, new Info {DamageMultiplier = 1m, AdditionalCriticalChance = 0m}},
                        {2, new Info {DamageMultiplier = 2m, AdditionalCriticalChance = 25m}}
                    }
                },
                {
                    3, new Dictionary<int, Info>
                    {
                        {1, new Info {DamageMultiplier = 1m, AdditionalCriticalChance = 0m}},
                        {2, new Info {DamageMultiplier = 2m, AdditionalCriticalChance = 10m}},
                        {3, new Info {DamageMultiplier = 3m, AdditionalCriticalChance = 35m}}
                    }
                },
                {
                    4, new Dictionary<int, Info>
                    {
                        {1, new Info {DamageMultiplier = 1m, AdditionalCriticalChance = 0m}},
                        {2, new Info {DamageMultiplier = 2m, AdditionalCriticalChance = 10m}},
                        {3, new Info {DamageMultiplier = 3m, AdditionalCriticalChance = 20m}},
                        {4, new Info {DamageMultiplier = 4m, AdditionalCriticalChance = 45m}}
                    }
                },
                {
                    5, new Dictionary<int, Info>
                    {
                        {1, new Info {DamageMultiplier = 1m, AdditionalCriticalChance = 0m}},
                        {2, new Info {DamageMultiplier = 2m, AdditionalCriticalChance = 10m}},
                        {3, new Info {DamageMultiplier = 3m, AdditionalCriticalChance = 20m}},
                        {4, new Info {DamageMultiplier = 4m, AdditionalCriticalChance = 30m}},
                        {5, new Info {DamageMultiplier = 5m, AdditionalCriticalChance = 55m}}
                    }
                }
            };

        public static int GetCountMax(int level)
        {
            if (level < 11)
                return CountMaxLowerLimit;

            if (level < 100)
                return 3;

            return level < 250
                ? 4
                : CountMaxUpperLimit;
        }

        public static decimal GetDamageMultiplier(int attackCount, int attackCountMax)
        {
            if (attackCount > attackCountMax)
                throw new ArgumentOutOfRangeException(
                    $"{nameof(attackCount)}: {attackCount} / {nameof(attackCountMax)}: {attackCountMax}");

            var info = GetInfo(attackCount, attackCountMax);
            return info.DamageMultiplier;
        }

        public static decimal GetAdditionalCriticalChance(int attackCount, int attackCountMax)
        {
            if (attackCount > attackCountMax)
                throw new ArgumentOutOfRangeException(
                    $"{nameof(attackCount)}: {attackCount} / {nameof(attackCountMax)}: {attackCountMax}");

            var info = GetInfo(attackCount, attackCountMax);
            return info.AdditionalCriticalChance;
        }

        private static Info GetInfo(int attackCount, int attackCountMax)
        {
            if (attackCount > attackCountMax)
                throw new ArgumentOutOfRangeException(
                    $"{nameof(attackCount)}: {attackCount} / {nameof(attackCountMax)}: {attackCountMax}");

            if (!CachedInfo.ContainsKey(attackCountMax))
                throw new ArgumentOutOfRangeException($"{nameof(attackCountMax)}: {attackCountMax}");

            if (!CachedInfo[attackCountMax].ContainsKey(attackCount))
                throw new ArgumentOutOfRangeException(
                    $"{nameof(attackCountMax)}: {attackCountMax} / {nameof(attackCount)}: {attackCount}");

            return CachedInfo[attackCountMax][attackCount];
        }
    }
}
