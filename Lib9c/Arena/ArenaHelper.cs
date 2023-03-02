using System;
using System.Collections.Generic;
using Libplanet;
using Libplanet.Assets;
using Nekoyume.Action;
using Nekoyume.Battle;
using Nekoyume.Helper;
using Nekoyume.Model.Arena;
using Nekoyume.Model.BattleStatus;
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

        [Obsolete("Use `ScoreLimits` instead.")]
        public static readonly IReadOnlyDictionary<ArenaType, (int, int)> ScoreLimitsV1 =
            new Dictionary<ArenaType, (int, int)>
            {
                { ArenaType.Season, (100, -100) },
                { ArenaType.Championship, (100, -100) }
            };

        [Obsolete("Use `ScoreLimits` instead.")]
        public static readonly IReadOnlyDictionary<ArenaType, (int upper, int lower)> ScoreLimitsV2 =
            new Dictionary<ArenaType, (int, int)>
            {
                { ArenaType.OffSeason, (100, -100) },
                { ArenaType.Season, (100, -100) },
                { ArenaType.Championship, (100, -100) }
            };

        [Obsolete("Use `ScoreLimits` instead.")]
        public static readonly IReadOnlyDictionary<ArenaType, (int upper, int lower)> ScoreLimitsV3 =
            new Dictionary<ArenaType, (int, int)>
            {
                { ArenaType.Season, (100, -100) },
                { ArenaType.Championship, (100, -100) }
            };

        public static readonly IReadOnlyDictionary<ArenaType, (int upper, int lower)> ScoreLimits =
            new Dictionary<ArenaType, (int, int)>
            {
                { ArenaType.Season, (200, -100) },
                { ArenaType.Championship, (200, -100) }
            };

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

                var itemId = GetMedalItemId(data.ChampionshipId, data.Round);
                if (avatarState.inventory.TryGetItem(itemId, out var item))
                {
                    count += item.count;
                }
            }

            return count;
        }

        public static FungibleAssetValue GetEntranceFee(
            ArenaSheet.RoundData roundData,
            long currentBlockIndex,
            int avatarLevel)
        {
            var fee = CrystalCalculator.CalculateEntranceFee(avatarLevel, roundData.EntranceFee);
            return roundData.IsTheRoundOpened(currentBlockIndex)
                ? fee
                : fee.DivRem(100, out _) * 50;
        }

        [Obsolete("Use `ValidateScoreDifference()` instead.")]
        public static bool ValidateScoreDifferenceV1(
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

        [Obsolete("Use `ValidateScoreDifference()` instead.")]
        public static bool ValidateScoreDifferenceV2(
            IReadOnlyDictionary<ArenaType, (int, int)> scoreLimits,
            ArenaType arenaType,
            int myScore,
            int enemyScore)
        {
            if (!scoreLimits.ContainsKey(arenaType))
            {
                return false;
            }

            var (upper, lower) = scoreLimits[arenaType];
            var diff = enemyScore - myScore;
            return lower <= diff && diff <= upper;
        }

        public static bool ValidateScoreDifference(
            IReadOnlyDictionary<ArenaType, (int, int)> scoreLimits,
            ArenaType arenaType,
            int myScore,
            int enemyScore)
        {
            if (!scoreLimits.ContainsKey(arenaType))
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

        public static (int, int, int) GetScores(int myScore, int enemyScore)
        {
            var (myWinScore, enemyWinScore) = ArenaScoreHelper.GetScore(
                myScore, enemyScore, BattleLog.Result.Win);

            var (myDefeatScore, _) = ArenaScoreHelper.GetScore(
                myScore, enemyScore, BattleLog.Result.Lose);

            return (myWinScore, myDefeatScore, enemyWinScore);
        }

        public static (int, int, int) GetScoresV1(int myScore, int enemyScore)
        {
            var (myWinScore, enemyWinScore) = ArenaScoreHelper.GetScoreV4(
                myScore, enemyScore, BattleLog.Result.Win);

            var (myDefeatScore, _) = ArenaScoreHelper.GetScoreV4(
                myScore, enemyScore, BattleLog.Result.Lose);

            return (myWinScore, myDefeatScore, enemyWinScore);
        }

        public static int GetRewardCount(int score)
        {
            if (score >= 1800)
            {
                return 6;
            }

            if (score >= 1400)
            {
                return 5;
            }

            if (score >= 1200)
            {
                return 4;
            }

            if (score >= 1100)
            {
                return 3;
            }

            if (score >= 1001)
            {
                return 2;
            }

            return 1;
        }

        public static FungibleAssetValue GetTicketPrice(
            ArenaSheet.RoundData roundData,
            ArenaInformation arenaInformation,
            Currency currency)
        {
            var ticketPrice = currency * roundData.TicketPrice;
            var addTicketPrice = currency * roundData.AdditionalTicketPrice;
            var price = ticketPrice.DivRem(100, out _) +
                       (addTicketPrice.DivRem(100, out _) * arenaInformation.PurchasedTicketCount);
            return price;
        }

        [Obsolete("not use since v100320, battle_arena6. Use `ArenaSheet.RoundData.MaxPurchaseCount`")]
        public static long GetMaxPurchasedTicketCount(ArenaSheet.RoundData roundData)
        {
            var result = (roundData.EndBlockIndex - roundData.StartBlockIndex + 1) / 1260;
            return result;
        }
    }
}
