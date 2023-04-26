using System;
using System.Globalization;
using System.Text;

namespace Nekoyume
{
    public static class BlockIndexExtensions
    {
        public static string BlockIndexToDateTimeString(
            this long targetBlockIndex,
            long currentBlockIndex,
            int secondsPerBlock,
            DateTime now,
            string format)
        {
            var differ = targetBlockIndex - currentBlockIndex;
            var timeSpan = TimeSpan.FromSeconds(differ * secondsPerBlock);
            var targetDateTime = now + timeSpan;
            return targetDateTime.ToString(format, CultureInfo.InvariantCulture);
        }

        public static string BlockIndexToDateTimeString(
            this long targetBlockIndex,
            long currentBlockIndex,
            int secondsPerBlock,
            DateTime now) =>
            BlockIndexToDateTimeString(targetBlockIndex, currentBlockIndex, secondsPerBlock, now, "yyyy/MM/dd");

        public static string BlockRangeToTimeSpanString(this long blockRange, int secondsPerBlock)
        {
            var timeSpan = TimeSpan.FromSeconds(blockRange * secondsPerBlock);
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
