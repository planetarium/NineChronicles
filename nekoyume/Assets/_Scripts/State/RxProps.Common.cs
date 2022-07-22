using Nekoyume.Helper;

namespace Nekoyume.State
{
    public static partial class RxProps
    {
        public class TicketProgress
        {
            public int currentTicketCount;
            public int maxTicketCount;
            public int progressedBlockRange;
            public int totalBlockRange;
            public string remainTimespanToReset;
            
            public float NormalizedTicketCount => (float)currentTicketCount / maxTicketCount;
            
            public string CurrentAndMaxTicketCountText => $"{currentTicketCount}/{maxTicketCount}";

            public TicketProgress(
                int currentTicketCount,
                int maxTicketCount,
                int progressedBlockRange,
                int totalBlockRange,
                string remainTimespanToReset)
            {
                this.currentTicketCount = currentTicketCount;
                this.maxTicketCount = maxTicketCount;
                this.progressedBlockRange = progressedBlockRange;
                this.totalBlockRange = totalBlockRange;
                this.remainTimespanToReset = remainTimespanToReset;
            }

            public TicketProgress(
                int currentTicketCount,
                int maxTicketCount,
                int progressedBlockRange,
                int totalBlockRange)
                : this(
                    currentTicketCount,
                    maxTicketCount,
                    progressedBlockRange,
                    totalBlockRange,
                    Util.GetBlockToTime(totalBlockRange - progressedBlockRange))
            {
            }
        }
    }
}
