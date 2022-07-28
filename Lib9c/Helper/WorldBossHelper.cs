using System;
using Libplanet.Assets;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.Helper
{
    public static class WorldBossHelper
    {
        public const long RefillInterval = 100L;

        public static int CalculateRank(int highScore)
        {
            return Math.Min(5, highScore / 10_000);
        }

        public static FungibleAssetValue CalculateTicketPrice(WorldBossListSheet.Row row, RaiderState raiderState, Currency currency)
        {
            return (row.TicketPrice + row.AdditionalTicketPrice * raiderState.PurchaseCount) * currency;
        }

        public static bool CanRefillTicket(long blockIndex, long refilledIndex, long startedIndex)
        {
            return (blockIndex - startedIndex) / RefillInterval > refilledIndex / RefillInterval;
        }
    }
}
