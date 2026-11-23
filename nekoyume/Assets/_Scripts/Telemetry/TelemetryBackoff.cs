using System;

namespace Nekoyume.Telemetry
{
    public static class TelemetryBackoff
    {
        public static TimeSpan NextDelay(int failures, Random rng)
        {
            var exp = Math.Min(Math.Max(failures - 1, 0), 10);
            var seconds = Math.Min(60.0 * Math.Pow(2, exp), 1800.0);
            return TimeSpan.FromSeconds(seconds + rng.NextDouble() * 30.0);
        }
    }
}
