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
        public static bool TryGetArenaType(
            this ArenaSheet arenaSheet,
            int medalItemId,
            out ArenaType arenaType)
        {
            arenaType = ArenaType.OffSeason;
            // NOTE: `700_102` is new arena medal item id.
            if (medalItemId is < 700_100 or >= 800_000)
            {
                return false;
            }

            // `championshipId`: 1 ~ 999
            var championshipId = medalItemId % 100_000 / 100;
            if (!arenaSheet.TryGetValue(championshipId, out var row))
            {
                return false;
            }

            // `round`: 1 ~ 99
            var round = medalItemId % 100;
            if (!row.TryGetRound(round, out var roundData))
            {
                return false;
            }

            arenaType = roundData.ArenaType;
            return true;
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
                NcDebug.LogException(e);
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
                NcDebug.LogException(e);
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

        /// <summary>
        /// This is origin of TryGetSeasonNumber().
        /// </summary>
        /// <param name="roundDataEnumerable"></param>
        /// <param name="round"></param>
        /// <param name="seasonNumber"></param>
        /// <returns></returns>
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

            // NOTE: The season number is beginning from 4. so it initialized as 3.
            // because the first season number will be set like `seasonNumber++` in the following code.
            // ********
            // The season number is beginning from 1 on heimdall, idun chain.(NOT ODIN, a.k.a. Nine Chronicles main-net)
            // ********
            seasonNumber = championshipId > 0 ? 3 : 0;

            // Add count of last season by id.
            // championship 1 includes 1 seasons.
            if (championshipId > 1) seasonNumber += 3;
            // championship 2 or more includes 2 seasons.
            if (championshipId > 2) seasonNumber += (championshipId - 2) * 2;

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

            medalItemResourceId = ArenaHelper.GetMedalItemId(
                roundData.ChampionshipId,
                roundData.Round);
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
            // 1,  2: 2022
            // 3,  4,  5,  6,  7,  8: 2023
            // 9, 10, 11, 12, 13, 14: 2024
            // ...
            var championshipId = row.ChampionshipId;
            // exception handling for Championship 0 of Heimdall or Idun chain
            if (championshipId == 0)
            {
                championshipId += 3;
            }

            if (championshipId <= 2)
            {
                return 2022;
            }

            var offset = (row.ChampionshipId - 3) / 6 + 1;

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
            var requiredMedalCount = row.Round
                .FirstOrDefault(r => r.ArenaType == ArenaType.Championship)
                ?.RequiredMedalCount ?? 0;
            return medalTotalCount >= requiredMedalCount;
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

        /// <summary>
        /// This is origin of TryGetSeasonNumbersOfChampionship().
        /// </summary>
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

            // NOTE: The season number is beginning from 4. so it initialized as 3.
            // because the first season number will be set like `++seasonStartNumber` in the following code.
            // ********
            // The season number is beginning from 1 on heimdall, idun chain.(NOT ODIN, a.k.a. Nine Chronicles main-net)
            // ********
            var seasonStartNumber = championshipId > 0 ? 3 : 0;

            // Add count of last season by id.
            // championship 1 includes 1 seasons.
            if (championshipId > 1) seasonStartNumber += 3;
            // championship 2 or more includes 2 seasons.
            if (championshipId > 2) seasonStartNumber += (championshipId - 2) * 2;

            foreach (var roundData in roundDataArray)
            {
                if (roundData.ArenaType != ArenaType.Season)
                {
                    continue;
                }

                seasonNumbers.Add(++seasonStartNumber);
            }

            return true;
        }
    }
}
