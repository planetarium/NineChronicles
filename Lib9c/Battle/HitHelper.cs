// #define TEST_LOG

using System;
using System.Text;

namespace Nekoyume.Battle
{
    public static class HitHelper
    {
        public const int GetHitStep1LevelDiffMin = -14;
        public const int GetHitStep1LevelDiffMax = 10;
        public const int GetHitStep1CorrectionMin = -5;
        public const int GetHitStep1CorrectionMax = 50;
        public const int GetHitStep2AdditionalCorrectionMin = 0;
        public const int GetHitStep2AdditionalCorrectionMax = 50;
        public const int GetHitStep3CorrectionMin = 10;
        public const int GetHitStep3CorrectionMax = 90;

        public static bool IsHit(
            int attackerLevel, int attackerHit,
            int defenderLevel, int defenderHit,
            int lowLimitChance)
        {
            var correction = GetHitStep1(attackerLevel, defenderLevel);
            correction += GetHitStep2(attackerHit, defenderHit);
            correction = GetHitStep3(correction);
            var isHit = GetHitStep4(lowLimitChance, correction);
#if TEST_LOG
            var sb = new StringBuilder();
            sb.Append($"{nameof(attackerLevel)}: {attackerLevel}");
            sb.Append($" / {nameof(attackerHit)}: {attackerHit}");
            sb.Append($" / {nameof(defenderLevel)}: {defenderLevel}");
            sb.Append($" / {nameof(defenderHit)}: {defenderHit}");
            sb.Append($" / {nameof(lowLimitChance)}: {lowLimitChance}");
            sb.Append($" / {nameof(correction)}: {correction}");
            sb.Append($" / {nameof(isHit)}: {isHit}");
            Debug.LogWarning(sb.ToString());
#endif
            return isHit;
        }

        public static bool IsHitWithoutLevelCorrection(
            int attackerLevel, int attackerHit,
            int defenderLevel, int defenderHit,
            int lowLimitChance)
        {
            var correction = 40;
            correction += GetHitStep2(attackerHit, defenderHit);
            correction = GetHitStep3(correction);
            var isHit = GetHitStep4(lowLimitChance, correction);
#if TEST_LOG
            var sb = new StringBuilder();
            sb.Append($"{nameof(attackerLevel)}: {attackerLevel}");
            sb.Append($" / {nameof(attackerHit)}: {attackerHit}");
            sb.Append($" / {nameof(defenderLevel)}: {defenderLevel}");
            sb.Append($" / {nameof(defenderHit)}: {defenderHit}");
            sb.Append($" / {nameof(lowLimitChance)}: {lowLimitChance}");
            sb.Append($" / {nameof(correction)}: {correction}");
            sb.Append($" / {nameof(isHit)}: {isHit}");
            Debug.LogWarning(sb.ToString());
#endif
            return isHit;
        }

        public static int GetHitStep1(int attackerLevel, int defenderLevel)
        {
            var diff = attackerLevel - defenderLevel;

            if (diff <= GetHitStep1LevelDiffMin)
            {
                return GetHitStep1CorrectionMin;
            }

            if (diff >= GetHitStep1LevelDiffMax)
            {
                return GetHitStep1CorrectionMax;
            }

            return diff switch
            {
                -13 => -4,
                -12 => -3,
                -11 => -2,
                -10 => -1,
                -9 => 0,
                -8 => 1,
                -7 => 2,
                -6 => 4,
                -5 => 6,
                -4 => 8,
                -3 => 13,
                -2 => 20,
                -1 => 28,
                0 => 40,
                1 => 41,
                2 => 42,
                3 => 43,
                4 => 44,
                5 => 45,
                6 => 46,
                7 => 47,
                8 => 48,
                9 => 49,
                _ => 0
            };
        }

        public static int GetHitStep2(int attackerHit, int defenderHit)
        {
            attackerHit = Math.Max(1, attackerHit);
            defenderHit = Math.Max(1, defenderHit);
            var additionalCorrection = (attackerHit * 10000 - defenderHit * 10000 / 3) / defenderHit / 100;
            return Math.Min(Math.Max(additionalCorrection, GetHitStep2AdditionalCorrectionMin),
                GetHitStep2AdditionalCorrectionMax);
        }

        public static int GetHitStep3(int correction)
        {
            return Math.Min(Math.Max(correction, GetHitStep3CorrectionMin), GetHitStep3CorrectionMax);
        }

        public static bool GetHitStep4(int lowLimitChance, int correction)
        {
            return correction >= lowLimitChance;
        }
    }
}
