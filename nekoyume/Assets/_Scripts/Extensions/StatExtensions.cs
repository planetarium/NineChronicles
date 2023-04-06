using Nekoyume.Model.Stat;
using Nekoyume.TableData;
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

        public static string ValueToString(this StatType statType, decimal value, string format = null)
        {
            if (string.IsNullOrEmpty(format))
            {
                var str =
                    statType == StatType.SPD ||
                    statType == StatType.DRR ||
                    statType == StatType.CDMG
                    ? (value / 100m).ToString(CultureInfo.InvariantCulture)
                    : ((int)value).ToString();

                return str;
            }
            else
            {
                var str =
                    statType == StatType.SPD ||
                    statType == StatType.DRR ||
                    statType == StatType.CDMG
                    ? (value / 100m).ToString(format, CultureInfo.InvariantCulture)
                    : ((int)value).ToString();

                return str;
            }
        }
    }
}
