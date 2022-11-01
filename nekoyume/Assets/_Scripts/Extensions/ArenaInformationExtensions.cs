using Nekoyume.Action;
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
            int gameConfigStateDailyArenaInterval)
        {
            var purchasedCountAddr =
                arenaInfo.Address.Derive(BattleArena.PurchasedCountKey);

            var currentTicketResetCount = ArenaHelper.GetCurrentTicketResetCount(
                blockIndex,
                roundStartBlockIndex,
                gameConfigStateDailyArenaInterval);


            // if (!states.TryGetState(purchasedCountAddr, out Integer purchasedCountDuringInterval))
            // {
            //     purchasedCountDuringInterval = 0;
            // }
            // Todo : 클라에서 GetState하는게 lib9c에서만큼 막 하면 안될 것 같음...!

            return arenaInfo.TicketResetCount < currentTicketResetCount
                ? 0
                : 0;
        }
    }
}
