using System.Collections.Generic;
using Libplanet;
using Libplanet.Assets;
using Nekoyume.Action;
using Nekoyume.Helper;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.Arena
{
    /// <summary>
    /// There are only things that don't change
    /// </summary>
    public static class ArenaHelper
    {
        public static Address DeriveArenaAddress(int championshipId, int round) =>
            Addresses.Arena.Derive($"_{championshipId}_{round}");

        public static int GetMedalItemId(int championshipId, int round) =>
            700_000 + (championshipId * 100) + round;

        public static Material GetMedal(
            int championshipId,
            int round,
            MaterialItemSheet materialItemSheet)
        {
            var itemId = GetMedalItemId(championshipId, round);
            var medal = ItemFactory.CreateMaterial(materialItemSheet, itemId);
            return medal;
        }

        public static int GetMedalTotalCount(ArenaSheet.Row row, AvatarState avatarState)
        {
            var count = 0;
            foreach (var data in row.Round)
            {
                if (!data.ArenaType.Equals(ArenaType.Season))
                {
                    continue;
                }

                var itemId = GetMedalItemId(data.Id, data.Round);
                if (avatarState.inventory.TryGetItem(itemId, out var item))
                {
                    count += item.count;
                }
            }

            return count;
        }

        public static FungibleAssetValue GetEntranceFee(
            ArenaSheet.RoundData roundData,
            long currentBlockIndex)
        {
            var fee = roundData.IsTheRoundOpened(currentBlockIndex)
                ? roundData.EntranceFee
                : roundData.DiscountedEntranceFee;
            return fee * CrystalCalculator.CRYSTAL;
        }

        public static bool ValidateScoreDifference(
            IReadOnlyDictionary<ArenaType, (int, int)> scoreLimits,
            ArenaType arenaType,
            int myScore,
            int enemyScore)
        {
            if (arenaType.Equals(ArenaType.OffSeason))
            {
                return true;
            }

            var (upper, lower) = scoreLimits[arenaType];
            var diff = enemyScore - myScore;
            return lower <= diff && diff <= upper;
        }

        public static int GetCurrentTicketResetCount(
            long currentBlockIndex,
            long roundStartBlockIndex,
            int interval)
        {
            var blockDiff = currentBlockIndex - roundStartBlockIndex;
            return interval > 0 ? (int)(blockDiff / interval) : 0;
        }
    }
}
