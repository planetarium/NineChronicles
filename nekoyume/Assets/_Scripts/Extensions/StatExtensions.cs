using mixpanel;
using Nekoyume.Model.Stat;
using Nekoyume.TableData;
using System;
using System.Globalization;

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

        public static string OptionRowToString(this EquipmentItemOptionSheet.Row optionRow)
        {
            var statMin = ValueToString(optionRow.StatType, optionRow.StatMin);
            var statMax = ValueToString(optionRow.StatType, optionRow.StatMax);

            var description = $"{optionRow.StatType} +({statMin}-{statMax})";
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

        public static string EffectToString(this StatBuffSheet.Row row, int power)
        {
            var valueText = row.Value.ToString();

            if (power > 0)
            {
                var sign = power >= 0 ? "+" : "-";
                if (row.ReferencedStatType != StatType.NONE)
                {
                    var multiplierText = (Math.Abs(power) / 10000m).ToString("0.##");
                    
                    valueText = $"({valueText} {sign} {multiplierText} {row.ReferencedStatType})";
                }
                else
                {
                    valueText = (row.Value + power).ToString();
                }
            }
            
            return (row.Value >= 0 && power >= 0 ? "+" : string.Empty) +
                valueText +
                (row.OperationType == StatModifier.OperationType.Percentage ? "%" : string.Empty);
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
