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
            DateTime now)
        {
            return BlockIndexToDateTimeString(targetBlockIndex, currentBlockIndex, secondsPerBlock, now, "yyyy/MM/dd");
        }

        public static string BlockIndexToDateTimeStringHour(
            this long targetBlockIndex,
            long currentBlockIndex)
        {
            return BlockIndexToDateTimeString(targetBlockIndex, currentBlockIndex, LiveAssetManager.instance.GameConfig.SecondsPerBlock, DateTime.Now, "yyyy/MM/dd HH:mm");
        }

        public static string BlockRangeToTimeSpanString(this long blockRange, bool limitUnit = false)
        {
            var timeSpan = BlockToTimeSpan(blockRange);
            return timeSpan.TimespanToString(limitUnit);
        }

        public static TimeSpan BlockToTimeSpan(this long block)
        {
            if (block < 0)
            {
                return TimeSpan.Zero;
            }

            var secondsPerBlock = LiveAssetManager.instance.GameConfig.SecondsPerBlock;
            return TimeSpan.FromSeconds(block * secondsPerBlock);
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
