using System;
using System.Linq;
using Nekoyume.L10n;
using Nekoyume.TableData;

namespace Nekoyume
{
    public static class WorldSheetExtensions
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

        public static bool TryGetByStageId(
            this WorldSheet sheet,
            int stageId,
            out WorldSheet.Row worldRow)
        {
            foreach (var row in sheet.OrderedList)
            {
                if (stageId < row.StageBegin || stageId > row.StageEnd)
                {
                    continue;
                }

                worldRow = row;
                return true;
            }

            worldRow = null;
            return false;
        }

        public static string GetLocalizedName(this WorldSheet.Row worldRow)
        {
            return L10nManager.Localize($"WORLD_NAME_{worldRow.Name.ToUpper().Replace(" ", "_")}");
        }

        public static bool ContainsStageId(this WorldSheet.Row worldRow, int stageId)
        {
            return stageId >= worldRow.StageBegin &&
                   stageId <= worldRow.StageEnd;
        }

        [Obsolete("이전에는 스테이지 번호를 스테이지 ID와 다르게 월드 내의 스테이지 순서로 표시했는데, 지금은 스테이지 ID를 그대로 표시하게 수정했습니다.")]
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
