using Codice.Client.BaseCommands;
using Microsoft.Extensions.Primitives;
using mixpanel;
using Nekoyume.Model.Stat;
using Nekoyume.TableData;
using System;
using System.Globalization;
using System.Text;

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

        public static string EffectToString(this StatBuffSheet.Row row)
        {
            var sb = new StringBuilder();

            if (row.BaseValue > 0)
            {
                sb.Append('+');
            }

            var hasBaseValue = row.BaseValue != 0;
            var hasStatRatio = row.StatRatio != 0;

            if (hasBaseValue)
            {
                sb.Append(row.BaseValue);
            }

            if (hasStatRatio)
            {
                if (hasBaseValue)
                {
                    sb.Append('/');
                }
                sb.Append($"{row.StatRatio} {row.ReferencedStatType}");
            }

            return row.OperationType == StatModifier.OperationType.Percentage ?
                $"({sb})%" : sb.ToString();
        }
    }
}
