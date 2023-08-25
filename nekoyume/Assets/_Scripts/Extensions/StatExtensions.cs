using mixpanel;
using Nekoyume.Game;
using Nekoyume.Model.Skill;
using Nekoyume.Model.Stat;
using Nekoyume.TableData;
using Org.BouncyCastle.Utilities;
using System;
using System.Globalization;
using System.Linq;

namespace Nekoyume
{
    public static class StatExtensions
    {
        public static string DecimalStatToString(this DecimalStat stat)
        {
            var value =
                stat.StatType == StatType.SPD ||
                stat.StatType == StatType.DRR ||
                stat.StatType == StatType.CDMG ?
                (stat.BaseValue / 100m) : stat.BaseValue;

            return $"{stat.StatType} +{(float)value}";
        }

        public static string OptionRowToString(
            this EquipmentItemOptionSheet.Row optionRow, decimal ratio, bool showRatio = true)
        {
            var statMin = ValueToString(optionRow.StatType, optionRow.StatMin);
            var statMax = ValueToString(optionRow.StatType, optionRow.StatMax);

            var description = $"{optionRow.StatType} {statMin}~{statMax}";
            if (showRatio)
            {
                description += $" ({ratio:0%})";
            }

            return description;
        }

        public static string ValueToString(this StatType statType, int value, bool isSigned = false)
        {
            switch (statType)
            {
                case StatType.HP:
                case StatType.ATK:
                case StatType.DEF:
                case StatType.HIT:
                case StatType.DRV:
                case StatType.ArmorPenetration:
                case StatType.Thorn:
                    return isSigned
                        ? value.ToString("+0.##;-0.##")
                        : (value).ToString();
                case StatType.CRI:
                    return isSigned
                        ? value.ToString("+0.##\\%;-0.##\\%")
                        : $"{value:0.#\\%}";
                case StatType.SPD:
                case StatType.DRR:
                case StatType.CDMG:
                    return isSigned
                        ? (value / 100m).ToString("+0.##;-0.##", CultureInfo.InvariantCulture)
                        : (value / 100m).ToString("0.##", CultureInfo.InvariantCulture);
                default:
                    throw new ArgumentOutOfRangeException(nameof(statType), statType, null);
            }
        }

        public static string ValueToShortString(this StatType statType, int value)
        {
            switch (statType)
            {
                case StatType.HP:
                case StatType.ATK:
                case StatType.DEF:
                case StatType.HIT:
                case StatType.DRV:
                case StatType.ArmorPenetration:
                case StatType.Thorn:
                    return value.ToCurrencyNotation();
                case StatType.CRI:
                    return $"{value:0.#\\%}";
                case StatType.SPD:
                case StatType.DRR:
                case StatType.CDMG:
                    return ((int)(value / 100m)).ToCurrencyNotation();
                default:
                    throw new ArgumentOutOfRangeException(nameof(statType), statType, null);
            }
        }

        public static string GetAcronym(this StatType type)
        {
            switch (type)
            {
                case StatType.NONE:
                case StatType.HP:
                case StatType.ATK:
                case StatType.DEF:
                case StatType.CRI:
                case StatType.HIT:
                case StatType.SPD:
                case StatType.DRV:
                case StatType.DRR:
                case StatType.CDMG:
                    return type.ToString();
                case StatType.ArmorPenetration:
                    return "A.PEN";
                case StatType.Thorn:
                    return "Thorn";
                default:
                    return "NONE";
            }
        }
    }
}
