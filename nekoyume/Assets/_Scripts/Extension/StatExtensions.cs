using Nekoyume.Model.Stat;
using Nekoyume.TableData;
using System.Globalization;

namespace Nekoyume
{
    public static class StatExtensions
    {
        public static string DecimalStatToString(this DecimalStat stat)
        {
            var value = stat.Type == StatType.SPD ?
                (stat.Value / 100m) : stat.Value;

            return $"{stat.Type} +{(float)value}";
        }

        public static string OptionRowToString(this EquipmentItemOptionSheet.Row optionRow)
        {
            var statMin = ValueToString(optionRow.StatType, optionRow.StatMin);
            var statMax = ValueToString(optionRow.StatType, optionRow.StatMax);

            var description = $"{optionRow.StatType} +({statMin}-{statMax})";
            return description;
        }

        public static string ValueToString(StatType statType, int value, string format = null)
        {
            if (string.IsNullOrEmpty(format))
            {
                var str = statType == StatType.SPD
                    ? (value / 100f).ToString(CultureInfo.InvariantCulture)
                    : value.ToString();

                return str;
            }
            else
            {
                var str = statType == StatType.SPD
                    ? (value / 100f).ToString(format, CultureInfo.InvariantCulture)
                    : value.ToString();

                return str;
            }
        }
    }
}
