using System;

namespace Nekoyume
{
    public static class DateTimeExtensions
    {
        public static bool IsInTime(this DateTime value, DateTime begin, DateTime end)
        {
            var bDiff = (value - begin).TotalSeconds;
            var eDiff = (end - value).TotalSeconds;
            return bDiff > 0 && eDiff > 0;
        }

        /// <param name="value"></param>
        /// <param name="begin">"yyyy-MM-ddTHH:mm:ss"</param>
        /// <param name="end">"yyyy-MM-ddTHH:mm:ss"</param>
        /// <returns></returns>
        public static bool IsInTime(this DateTime value, string begin, string end) =>
            value.IsInTime(
                DateTime.ParseExact(begin, "yyyy-MM-ddTHH:mm:ss", null),
                DateTime.ParseExact(end, "yyyy-MM-ddTHH:mm:ss", null));
    }
}
