using System.Collections.Generic;
using Amazon.CloudWatchLogs.Model;
using Nekoyume.Arena;
using Nekoyume.Model.EnumType;
using Nekoyume.TableData;
using Nekoyume.UI.Module.Arena.Join;
using UnityEngine;

namespace Nekoyume
{
    public static class ArenaSheetExtensions
    {
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

        public static bool TryGetSeasonNumber(
            this ArenaSheet sheet,
            long blockIndex,
            int round,
            out int seasonNumber) =>
            sheet
                .GetRowByBlockIndex(blockIndex)
                .TryGetSeasonNumber(round, out seasonNumber);

        public static bool TryGetSeasonNumber(
            this ArenaSheet.Row row,
            int round,
            out int seasonNumber) =>
            row.Round.TryGetSeasonNumber(round, out seasonNumber);

        public static bool TryGetSeasonNumber(
            this IEnumerable<ArenaSheet.RoundData> roundDataEnumerable,
            int round,
            out int seasonNumber)
        {
            seasonNumber = 0;
            foreach (var roundData in roundDataEnumerable)
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

        public static bool TryGetMedalItemId(
            this ArenaSheet.RoundData roundData,
            out int medalItemId)
        {
            if (roundData.ArenaType == ArenaType.OffSeason)
            {
                medalItemId = 0;
                return false;
            }

            medalItemId = ArenaHelper.GetMedalItemId(roundData.ChampionshipId, roundData.Round);
            return true;
        }

        public static (long beginning, long end, long current) GetSeasonProgress(
            this ArenaSheet.RoundData roundData,
            long blockIndex) => (
            roundData?.StartBlockIndex ?? 0,
            roundData?.EndBlockIndex ?? 0,
            blockIndex);
    }
}
