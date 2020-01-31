using System.Linq;
using Assets.SimpleLocalization;
using Nekoyume.TableData;

namespace Nekoyume.UI
{
    public static class WorldSheetExtension
    {
        public static bool TryGetByName(this WorldSheet sheet, string name, out WorldSheet.Row worldRow)
        {
            foreach (var row in sheet.OrderedList.Where(row => row.Name.Equals(name)))
            {
                worldRow = row;
                return true;
            }

            worldRow = null;
            return false;
        }

        public static bool TryGetByStageId(this WorldSheet sheet, int stageId,
            out WorldSheet.Row worldRow)
        {
            foreach (var row in sheet.OrderedList)
            {
                if (stageId < row.StageBegin || stageId > row.StageEnd)
                    continue;

                worldRow = row;
                return true;
            }

            worldRow = sheet.Last;
            return true;
        }

        public static string GetLocalizedName(this WorldSheet.Row worldRow)
        {
            return LocalizationManager.Localize($"WORLD_NAME_{worldRow.Name.ToUpper().Replace(" ", "_")}");
        }

        public static bool ContainsStageId(this WorldSheet.Row worldRow, int stageId)
        {
            return stageId >= worldRow.StageBegin &&
                   stageId <= worldRow.StageEnd;
        }

        public static bool TryGetStageNumber(this WorldSheet.Row worldRow, int stageId, out int stageNumber)
        {
            if (stageId < worldRow.StageBegin ||
                stageId > worldRow.StageEnd)
            {
                stageNumber = 0;
                return false;
            }

            stageNumber = stageId - worldRow.StageBegin + 1;
            return true;
        }
    }
}
