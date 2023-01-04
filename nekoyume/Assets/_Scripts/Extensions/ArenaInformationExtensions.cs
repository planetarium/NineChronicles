using Nekoyume.Arena;
using Nekoyume.Model.Arena;

namespace Nekoyume
{
    public static class ArenaInformationExtensions
    {
        public static int GetTicketCount(
            this ArenaInformation arenaInfo,
            long blockIndex,
            long roundStartBlockIndex,
            int gameConfigStateDailyArenaInterval)
        {
            var currentTicketResetCount = ArenaHelper.GetCurrentTicketResetCount(
                blockIndex,
                roundStartBlockIndex,
                gameConfigStateDailyArenaInterval);

            return arenaInfo.TicketResetCount < currentTicketResetCount
                ? ArenaInformation.MaxTicketCount
                : arenaInfo.Ticket;
        }

        public static int GetPurchasedCountInInterval(
            this ArenaInformation arenaInfo,
            long blockIndex,
            long roundStartBlockIndex,
            int gameConfigStateDailyArenaInterval,
            int purchasedCountDuringInterval)
        {
            var currentTicketResetCount = ArenaHelper.GetCurrentTicketResetCount(
                blockIndex,
                roundStartBlockIndex,
                gameConfigStateDailyArenaInterval);

            return arenaInfo.TicketResetCount < currentTicketResetCount
                ? 0
                : purchasedCountDuringInterval;
        }
    }
}
