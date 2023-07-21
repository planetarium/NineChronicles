using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Nekoyume.Battle
{
    public static class AttackCountHelper
    {
        public const int CountMaxLowerLimit = 2;
        public const int CountMaxUpperLimit = 5;

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

        public static int GetDamageMultiplier(int attackCount, int attackCountMax)
        {
            if (attackCount > attackCountMax)
                throw new ArgumentOutOfRangeException(
                    $"{nameof(attackCount)}: {attackCount} / {nameof(attackCountMax)}: {attackCountMax}");

            if (attackCountMax <= 5)
            {
                return Math.Max(1, attackCount);
            }

            throw new ArgumentOutOfRangeException();
        }

        public static int GetAdditionalCriticalChance(int attackCount, int attackCountMax)
        {
            if (attackCount > attackCountMax)
                throw new ArgumentOutOfRangeException(
                    $"{nameof(attackCount)}: {attackCount} / {nameof(attackCountMax)}: {attackCountMax}");
            switch (attackCount)
            {
                case 1:
                    return 0;
                case 2:
                    switch (attackCountMax)
                    {
                        case 2:
                            return 25;
                        case 3:
                        case 4:
                        case 5:
                            return 10;
                    }
                    break;
                case 3:
                    switch (attackCountMax)
                    {
                        case 3:
                            return 35;
                        case 4:
                        case 5:
                            return 20;
                    }
                    break;
                case 4:
                    switch (attackCountMax)
                    {
                        case 4:
                            return 45;
                        case 5:
                            return 30;
                    }
                    break;
                case 5:
                    switch (attackCountMax)
                    {
                        case 5:
                            return 55;
                    }
                    break;
            }

            throw new ArgumentOutOfRangeException();
        }
    }
}
