using Nekoyume.Model.EnumType;
using Nekoyume.TableData;

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
    }
}
