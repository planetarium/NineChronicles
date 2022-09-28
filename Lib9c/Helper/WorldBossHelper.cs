using System;
using Libplanet.Assets;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.Helper
{
    public static class WorldBossHelper
    {
        public const long RefillInterval = 7200L;
        public const int MaxChallengeCount = 3;

        public static int CalculateRank(WorldBossCharacterSheet.Row row, int score)
        {
            var rank = 0;
            // Wave stats are already sorted by wave number.
            foreach (var waveData in row.WaveStats)
            {
                score -= (int)waveData.HP;
                if (score < 0)
                {
                    break;
                }
                ++rank;
            }

            return Math.Min(row.WaveStats.Count, rank);
        }

        public static FungibleAssetValue CalculateTicketPrice(WorldBossListSheet.Row row, RaiderState raiderState, Currency currency)
        {
            return (row.TicketPrice + row.AdditionalTicketPrice * raiderState.PurchaseCount) * currency;
        }

        public static bool CanRefillTicket(long blockIndex, long refilledIndex, long startedIndex)
        {
            return (blockIndex - startedIndex) / RefillInterval > (refilledIndex - startedIndex) / RefillInterval;
        }
    }
}
