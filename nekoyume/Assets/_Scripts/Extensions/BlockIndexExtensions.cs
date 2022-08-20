using System;
using System.Globalization;
using System.Text;

namespace Nekoyume
{
    public static class BlockIndexExtensions
    {
        private const int SecondsPerBlock = 12;

        public static string BlockIndexToDateTimeString(
            this long targetBlockIndex,
            long currentBlockIndex,
            DateTime now,
            string format)
        {
            var differ = targetBlockIndex - currentBlockIndex;
            var timeSpan = TimeSpan.FromSeconds(differ * SecondsPerBlock);
            var targetDateTime = now + timeSpan;
            return targetDateTime.ToString(format, CultureInfo.InvariantCulture);
        }

        public static string BlockIndexToDateTimeString(
            this long targetBlockIndex,
            long currentBlockIndex,
            DateTime now) =>
            BlockIndexToDateTimeString(targetBlockIndex, currentBlockIndex, now, "yyyy/MM/dd");

        public static string BlockRangeToTimeSpanString(this long blockRange)
        {
            var timeSpan = TimeSpan.FromSeconds(blockRange * SecondsPerBlock);
            var sb = new StringBuilder();
            if (timeSpan.Days > 0)
            {
                sb.Append(@"d\d");
            }

            if (timeSpan.Hours > 0)
            {
                if (sb.Length > 0)
                {
                    sb.Append(@"\ ");
                }

                sb.Append(@"h\h");
            }

            if (timeSpan.Minutes > 0)
            {
                if (sb.Length > 0)
                {
                    sb.Append(@"\ ");
                }

                sb.Append(@"m\m");
            }

            return sb.Length > 0
                ? timeSpan.ToString(sb.ToString(), CultureInfo.InvariantCulture)
                : "1m";
        }
    }
}
