using System;
using System.Globalization;
using System.Text;
using Nekoyume.Game.LiveAsset;
using Nekoyume.State;

namespace Nekoyume
{
    public static class BlockIndexExtensions
    {
        public static string BlockIndexToDateTimeString(
            this long targetBlockIndex,
            long currentBlockIndex,
            double secondsPerBlock,
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
            double secondsPerBlock,
            DateTime now)
        {
            return BlockIndexToDateTimeString(targetBlockIndex, currentBlockIndex, secondsPerBlock, now, "yyyy/MM/dd");
        }

        public static string BlockIndexToDateTimeStringHour(
            this long targetBlockIndex,
            long currentBlockIndex)
        {
            return BlockIndexToDateTimeString(targetBlockIndex, currentBlockIndex, Nekoyume.Helper.Util.BlockInterval, DateTime.Now, "yyyy/MM/dd HH:mm");
        }

        public static string BlockRangeToTimeSpanString(this long blockRange, bool limitUnit = false)
        {
            var timeSpan = BlockToTimeSpan(blockRange);
            return timeSpan.TimespanToString(limitUnit);
        }

        public static TimeSpan BlockToTimeSpan(this long block)
        {
            return block < 0 ?
                TimeSpan.Zero :
                TimeSpan.FromSeconds(block * Nekoyume.Helper.Util.BlockInterval);
        }

        public static string TimespanToString(this TimeSpan timeSpan, bool limitUnit = false)
        {
            var sb = new StringBuilder();
            if (timeSpan.Days > 0)
            {
                sb.Append($"{timeSpan.Days}d");
            }

            if (timeSpan.Hours > 0)
            {
                if (timeSpan.Days > 0)
                {
                    sb.Append(" ");
                }

                sb.Append($"{timeSpan.Hours}h");
            }

            if (timeSpan.Minutes > 0 && !(limitUnit && timeSpan.Days > 0))
            {
                if (timeSpan.Hours > 0)
                {
                    sb.Append(" ");
                }

                sb.Append($"{timeSpan.Minutes}m");
            }

            if (sb.Length == 0)
            {
                sb.Append("0m");
            }

            return sb.ToString();
        }
    }
}
