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
            var correction = 0;
            var diff = attackerLevel - defenderLevel;

            if (diff <= GetHitStep1LevelDiffMin)
            {
                correction = GetHitStep1CorrectionMin;
            }
            else if (diff >= GetHitStep1LevelDiffMax)
            {
                correction = GetHitStep1CorrectionMax;
            }
            else
            {
                switch (diff)
                {
                    case -13:
                        correction = -4;
                        break;
                    case -12:
                        correction = -3;
                        break;
                    case -11:
                        correction = -2;
                        break;
                    case -10:
                        correction = -1;
                        break;
                    case -9:
                        correction = 0;
                        break;
                    case -8:
                        correction = 1;
                        break;
                    case -7:
                        correction = 2;
                        break;
                    case -6:
                        correction = 4;
                        break;
                    case -5:
                        correction = 6;
                        break;
                    case -4:
                        correction = 8;
                        break;
                    case -3:
                        correction = 13;
                        break;
                    case -2:
                        correction = 20;
                        break;
                    case -1:
                        correction = 28;
                        break;
                    case 0:
                        correction = 40;
                        break;
                    case 1:
                        correction = 41;
                        break;
                    case 2:
                        correction = 42;
                        break;
                    case 3:
                        correction = 43;
                        break;
                    case 4:
                        correction = 44;
                        break;
                    case 5:
                        correction = 45;
                        break;
                    case 6:
                        correction = 46;
                        break;
                    case 7:
                        correction = 47;
                        break;
                    case 8:
                        correction = 48;
                        break;
                    case 9:
                        correction = 49;
                        break;
                }
            }

            return correction;
        }

        public static int GetHitStep2(int attackerHit, int defenderHit)
        {
            attackerHit = Math.Max(1, attackerHit);
            defenderHit = Math.Max(1, defenderHit);
            var additionalCorrection = (int) ((attackerHit - defenderHit / 3m) / defenderHit * 100);
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
