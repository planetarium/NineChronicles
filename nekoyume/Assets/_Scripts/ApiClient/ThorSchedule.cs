using System;
using Nekoyume.Helper;

namespace Nekoyume.ApiClient
{
    [Serializable]
    public class ThorSchedule
    {
        public string StartDateUtcString { get; set; }

        public string EndDateUtcString { get; set; }

        public string InformationUrl { get; set; }

        public DateTimeOffset StartDateUtc => DateTimeOffset.Parse(StartDateUtcString);

        public DateTimeOffset EndDateUtc => DateTimeOffset.Parse(EndDateUtcString);

        public bool IsOpened => StartDateUtc <= DateTimeOffset.UtcNow && DateTimeOffset.UtcNow <= EndDateUtc;

        public TimeSpan DiffFromEndTimeSpan => EndDateUtc - DateTimeOffset.UtcNow;

        public double DiffFromEndSeconds => DiffFromEndTimeSpan.TotalSeconds;

        public long DiffFromEndBlockIndex => (long)(DiffFromEndSeconds / Util.BlockInterval);
    }

    [Serializable]
    public class ThorSchedules
    {
        public ThorSchedule MainNet { get; set; }
        public ThorSchedule Others { get; set; }
    }
}
