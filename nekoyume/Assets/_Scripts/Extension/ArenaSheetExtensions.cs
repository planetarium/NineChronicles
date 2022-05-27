using Nekoyume.Arena;
using Nekoyume.Model.EnumType;
using Nekoyume.TableData;
using Nekoyume.UI.Module.Arena.Join;

namespace Nekoyume
{
    public static class ArenaSheetExtensions
    {
        public static bool TryGetSeasonNumber(this ArenaSheet.Row row, int round, out int seasonNumber)
        {
            seasonNumber = 0;
            foreach (var roundData in row.Round)
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

        public static ArenaJoinSeasonInfo.RewardType GetRewardType(this ArenaSheet.RoundData roundData)
        {
            return ArenaJoinSeasonInfo.RewardType.None;
        }
    }
}
