using System;
using System.Threading;

namespace Nekoyume.Telemetry
{
    public class TelemetryClock
    {
        private long _offsetMs;

        public long Now()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + Interlocked.Read(ref _offsetMs);
        }

        public void Observe(DateTimeOffset serverDate)
        {
            var offset = serverDate.ToUnixTimeMilliseconds()
                         - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            Interlocked.Exchange(ref _offsetMs, offset);
        }
    }
}
