using Nekoyume.Helper;

namespace Nekoyume.State
{
    public static partial class RxProps
    {
        public class TicketProgress
        {
            public int currentTickets;
            public int maxTickets;
            public int progressedBlockRange;
            public int totalBlockRange;
            public string remainTimespanToReset;

            public float NormalizedTicketCount => (float)currentTickets / maxTickets;

            public string CurrentAndMaxTicketCountText => $"{currentTickets}/{maxTickets}";

            public TicketProgress(
                int currentTickets,
                int maxTickets,
                int progressedBlockRange,
                int totalBlockRange,
                string remainTimespanToReset)
            {
                this.currentTickets = currentTickets;
                this.maxTickets = maxTickets;
                this.progressedBlockRange = progressedBlockRange;
                this.totalBlockRange = totalBlockRange;
                this.remainTimespanToReset = remainTimespanToReset;
            }

            public TicketProgress(
                int currentTickets,
                int maxTickets,
                int progressedBlockRange,
                int totalBlockRange)
                : this(
                    currentTickets,
                    maxTickets,
                    progressedBlockRange,
                    totalBlockRange,
                    ((long)totalBlockRange - progressedBlockRange).BlockRangeToTimeSpanString())
            {
            }

            public TicketProgress()
                : this(0, 0, 0, 0)
            {
            }

            public void Reset(
                int currentTicketCount = 0,
                int maxTicketCount = 0,
                int progressedBlockRange = 0,
                int totalBlockRange = 0)
            {
                currentTickets = currentTicketCount;
                maxTickets = maxTicketCount;
                this.progressedBlockRange = progressedBlockRange;
                this.totalBlockRange = totalBlockRange;
                var remainBlock = (long)totalBlockRange - progressedBlockRange;
                remainTimespanToReset =
                    $"{remainBlock:#,0}({remainBlock.BlockRangeToTimeSpanString()})";
            }
        }

        public class ArenaTicketProgress : TicketProgress
        {
            public int purchasedCountDuringInterval;

            public ArenaTicketProgress(
                int currentTickets,
                int maxTickets,
                int progressedBlockRange,
                int totalBlockRange,
                string remainTimespanToReset,
                int purchasedCountDuringInterval)
                : base(currentTickets,
                    maxTickets,
                    progressedBlockRange,
                    totalBlockRange,
                    remainTimespanToReset)
            {
                this.purchasedCountDuringInterval = purchasedCountDuringInterval;
            }

            public ArenaTicketProgress(
                int currentTickets,
                int maxTickets,
                int progressedBlockRange,
                int totalBlockRange,
                int purchasedCountDuringInterval)
                : this(
                    currentTickets,
                    maxTickets,
                    progressedBlockRange,
                    totalBlockRange,
                    ((long)totalBlockRange - progressedBlockRange).BlockRangeToTimeSpanString(),
                    purchasedCountDuringInterval)
            {
            }

            public ArenaTicketProgress() : this(0, 0, 0, 0, 0)
            {
            }

            public void Reset(
                int currentTicketCount = 0,
                int maxTicketCount = 0,
                int progressedBlockRange = 0,
                int totalBlockRange = 0,
                int purchasedCountDuringInterval = 0)
            {
                base.Reset(
                    currentTicketCount,
                    maxTicketCount,
                    progressedBlockRange,
                    totalBlockRange);
                this.purchasedCountDuringInterval = purchasedCountDuringInterval;
            }
        }
    }
}
