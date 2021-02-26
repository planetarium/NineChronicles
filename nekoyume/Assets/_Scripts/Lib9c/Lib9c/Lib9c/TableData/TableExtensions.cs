using System;
using System.Globalization;

namespace Nekoyume.TableData
{
    public static class TableExtensions
    {
        public static bool TryParseDecimal(string value, out decimal result) =>
            decimal.TryParse(value, NumberStyles.Number, NumberFormatInfo.InvariantInfo, out result);

        public static bool TryParseFloat(string value, out float result) =>
            float.TryParse(value, NumberStyles.Float, NumberFormatInfo.InvariantInfo, out result);

        public static bool TryParseLong(string value, out long result) =>
            long.TryParse(value, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out result);

        public static bool TryParseInt(string value, out int result) =>
            int.TryParse(value, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out result);

        public static int ParseInt(string value)
        {
            if (TryParseInt(value, out var result))
            {
                return result;
            }
            throw new ArgumentException(value);
        }

        public static decimal ParseDecimal(string value)
        {
            if (TryParseDecimal(value, out var result))
            {
                return result;
            }
            throw new ArgumentException(value);
        }

        public static long ParseLong(string value)
        {
            if (TryParseLong(value, out var result))
            {
                return result;
            }
            throw new ArgumentException(value);
        }
    }
}
