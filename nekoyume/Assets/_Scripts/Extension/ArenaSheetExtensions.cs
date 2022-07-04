using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Arena;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using UnityEngine;

namespace Nekoyume
{
    public static class ArenaSheetExtensions
    {
        /// <param name="medalItemId">`ItemSheet.Row.Id`</param>
        /// <param name="arenaSheet"></param>
        /// <returns>
        /// `championshipNumber`: 1 ~ 4
        /// `seasonNumber`: 1 ~ 12
        /// </returns>
        public static (int championshipNumber, int seasonNumber) ToArenaNumbers(
            this int medalItemId,
            ArenaSheet arenaSheet)
        {
            // NOTE: `700_102` is new arena medal item id.
            if (medalItemId < 700_000 ||
                medalItemId >= 800_000)
            {
                return (0, 0);
            }

            // `championshipId`: 1 ~ 999
            var championshipId = medalItemId % 100_000 / 100;
            if (!arenaSheet.TryGetValue(championshipId, out var row))
            {
                return (0, 0);
            }

            // `round`: 1 ~ 99
            var round = medalItemId % 100;
            var seasonNumber = 0;
            var isSeason = false;
            foreach (var roundData in row.Round)
            {
                if (roundData.ArenaType == ArenaType.Season)
                {
                    seasonNumber++;
                }

                if (roundData.Round == round)
                {
                    isSeason = roundData.ArenaType == ArenaType.Season;
                    break;
                }
            }

            if (championshipId <= 2)
            {
                seasonNumber = isSeason
                    ? seasonNumber + (championshipId - 1) * 3 + 3
                    : 0;
                return (championshipId, seasonNumber);
            }

            var championshipNumber = (championshipId - 2) % 4;
            seasonNumber = isSeason
                ? seasonNumber + (championshipNumber - 1) * 3
                : 0;
            return (championshipNumber, seasonNumber);
        }

        public static int ToItemIconResourceId(
            this (int championshipNumber, int seasonNumber) tuple)
        {
            var (championshipNumber, seasonNumber) = tuple;
            var id = 700_000;
            if (seasonNumber == 0)
            {
                id += championshipNumber * 100;
                id += 8;
                return id;
            }
            
            // seasonNumber 1, 2, 3 -> championshipNumber 1
            //              4, 5, 6 ->                    2
            //              7, 8, 9 ->                    3
            id += (seasonNumber + 2) / 3 * 100;
            // seasonNumber 1, 2, 3 -> round 2, 4, 6
            //              4, 5, 6 -> round 2, 4, 6
            //              7, 8, 9 -> round 2, 4, 6
            id += (seasonNumber - (seasonNumber - 1) / 3 * 3) * 2;
            return id;
        }

        public static bool TryGetCurrentRound(
            this ArenaSheet sheet,
            long blockIndex,
            out ArenaSheet.RoundData current)
        {
            try
            {
                current = sheet.GetRoundByBlockIndex(blockIndex);
                return true;
            }
            catch (InvalidOperationException e)
            {
                Debug.LogException(e);
                current = null;
                return false;
            }
        }

        public static bool TryGetNextRound(
            this ArenaSheet sheet,
            long blockIndex,
            out ArenaSheet.RoundData next)
        {
            try
            {
                var current = sheet.GetRoundByBlockIndex(blockIndex);
                var row = sheet.GetRowByBlockIndex(blockIndex);
                return row.TryGetRound(current.Round + 1, out next);
            }
            catch (InvalidOperationException e)
            {
                Debug.LogException(e);
                next = null;
                return false;
            }
        }

        public static int GetSeasonNumber(
            this ArenaSheet sheet,
            long blockIndex,
            int round,
            int defaultValue = 0) =>
            sheet.TryGetSeasonNumber(blockIndex, round, out var seasonNumber)
                ? seasonNumber
                : defaultValue;

        public static bool TryGetSeasonNumber(
            this ArenaSheet sheet,
            long blockIndex,
            int round,
            out int seasonNumber) =>
            sheet
                .GetRowByBlockIndex(blockIndex)
                .TryGetSeasonNumber(round, out seasonNumber);

        public static int GetSeasonNumber(
            this ArenaSheet.RoundData roundData,
            ArenaSheet arenaSheet,
            int defaultValue = 0) =>
            arenaSheet.TryGetValue(roundData.ChampionshipId, out var row)
                ? row.GetSeasonNumber(roundData.Round)
                : defaultValue;

        public static int GetSeasonNumber(
            this ArenaSheet.Row row,
            int round,
            int defaultValue = 0) =>
            row.TryGetSeasonNumber(round, out var seasonNumber)
                ? seasonNumber
                : defaultValue;

        public static bool TryGetSeasonNumber(
            this ArenaSheet.Row row,
            int round,
            out int seasonNumber) =>
            row.Round.TryGetSeasonNumber(round, out seasonNumber);

        public static int GetSeasonNumber(
            this IEnumerable<ArenaSheet.RoundData> roundDataEnumerable,
            int round,
            int defaultValue = 0) =>
            roundDataEnumerable.TryGetSeasonNumber(round, out var seasonNumber)
                ? seasonNumber
                : defaultValue;

        public static bool TryGetSeasonNumber(
            this IEnumerable<ArenaSheet.RoundData> roundDataEnumerable,
            int round,
            out int seasonNumber)
        {
            var roundDataArray = roundDataEnumerable as ArenaSheet.RoundData[]
                                 ?? roundDataEnumerable.ToArray();
            var firstRound = roundDataArray.FirstOrDefault();
            if (firstRound is null)
            {
                seasonNumber = 0;
                return false;
            }

            var championshipId = firstRound.ChampionshipId;
            if (roundDataArray.Any(e => e.ChampionshipId != championshipId))
            {
                seasonNumber = 0;
                return false;
            }

            // NOTE: The season number is beginning from 4 when the championship id is 1 or 2.
            //       So it initialized as 3 when the championship id is 1 or 2 because the first
            //       season number will be set like `seasonNumber++` in the following code.
            seasonNumber = championshipId == 1 || championshipId == 2
                ? 3
                : 0;

            // NOTE: The championship cycles once over four times.
            // And each championship includes three seasons.
            seasonNumber += (championshipId % 4 - 1) * 3;
            foreach (var roundData in roundDataArray)
            {
                if (roundData.ArenaType == ArenaType.Season)
                {
                    seasonNumber++;
                }

                if (roundData.Round == round)
                {
                    return roundData.ArenaType == ArenaType.Season;
                }
            }

            return false;
        }

        public static bool TryGetMedalItemResourceId(
            this ArenaSheet.RoundData roundData,
            out int medalItemResourceId)
        {
            if (roundData.ArenaType == ArenaType.OffSeason)
            {
                medalItemResourceId = 0;
                return false;
            }

            if (roundData.ArenaType == ArenaType.Championship)
            {
                medalItemResourceId = ArenaHelper.GetMedalItemId(
                    roundData.ChampionshipId % 4,
                    roundData.Round);
                return true;    
            }

            // NOTE: id = 700{championship id % 4:N1}{round:N2}
            //       e.g., 700102, 700406
            // NOTE: The season number is beginning from 4 when the championship id is 1 or 2.
            //       So `championshipNumber` should +1 like below
            //       because every championship has 3 seasons.
            var championshipNumber = roundData.ChampionshipId == 1 || roundData.ChampionshipId == 2
                ? roundData.ChampionshipId % 4 + 1
                : roundData.ChampionshipId % 4;
            medalItemResourceId = ArenaHelper.GetMedalItemId(championshipNumber, roundData.Round);
            return true;
        }

        public static (long beginning, long end, long current) GetSeasonProgress(
            this ArenaSheet.RoundData roundData,
            long blockIndex) => (
            roundData?.StartBlockIndex ?? 0,
            roundData?.EndBlockIndex ?? 0,
            blockIndex);

        public static int GetChampionshipYear(this ArenaSheet.Row row)
        {
            // row.ChampionshipId
            // 1, 2: 2022
            // 3, 4, 5, 6: 2023
            // 7, 8, 9, 10: 2024
            // ...
            if (row.ChampionshipId <= 2)
            {
                return 2022;
            }

            var offset = 0;
            var i = row.ChampionshipId - 2;
            while (i > 0)
            {
                offset++;
                i -= 4;
            }

            return 2022 + offset;
        }

        public static bool IsChampionshipConditionComplete(
            this ArenaSheet sheet,
            int championshipId,
            AvatarState avatarState) =>
            sheet[championshipId].IsChampionshipConditionComplete(avatarState);

        public static bool IsChampionshipConditionComplete(this ArenaSheet.Row row, AvatarState avatarState)
        {
            var medalTotalCount = ArenaHelper.GetMedalTotalCount(row, avatarState);
            var championshipRound = row.Round[7];
            return medalTotalCount >= championshipRound.RequiredMedalCount;
        }

        public static List<int> GetSeasonNumbersOfChampionship(
            this ArenaSheet.Row row) =>
            TryGetSeasonNumbersOfChampionship(
                row.Round,
                out var seasonNumbers)
                ? seasonNumbers
                : new List<int>();

        public static List<int> GetSeasonNumbersOfChampionship(
            this IEnumerable<ArenaSheet.RoundData> roundDataEnumerable) =>
            TryGetSeasonNumbersOfChampionship(
                roundDataEnumerable,
                out var seasonNumbers)
                ? seasonNumbers
                : new List<int>();

        public static bool TryGetSeasonNumbersOfChampionship(
            this IEnumerable<ArenaSheet.RoundData> roundDataEnumerable,
            out List<int> seasonNumbers)
        {
            seasonNumbers = new List<int>();
            var roundDataArray = roundDataEnumerable as ArenaSheet.RoundData[]
                                 ?? roundDataEnumerable.ToArray();
            var firstRound = roundDataArray.FirstOrDefault();
            if (firstRound is null)
            {
                return false;
            }

            var championshipId = firstRound.ChampionshipId;
            if (roundDataArray.Any(e => e.ChampionshipId != championshipId))
            {
                return false;
            }

            // NOTE: The season number is beginning from 4.
            // NOTE: The championship cycles once over four times.
            // And each championship includes three seasons.
            var seasonStartNumber = (championshipId % 4 - 1) * 3 + 4;
            foreach (var roundData in roundDataArray)
            {
                if (roundData.ArenaType != ArenaType.Season)
                {
                    continue;
                }

                seasonNumbers.Add(seasonStartNumber++);
            }

            return true;
        }
    }
}
