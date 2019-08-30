using System;
using System.Collections.Generic;
using System.Linq;

namespace Nekoyume.TableData
{
    [Serializable]
    public class WorldSheet : Sheet<int, WorldSheet.Row>
    {
        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => Id;
            public int Id { get; private set; }
            public string Name { get; private set; }
            public int StageBegin { get; private set; }
            public int StageEnd { get; private set; }
            
            public int StagesCount { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = int.TryParse(fields[0], out var id) ? id : 0;
                Name = fields[1];
                StageBegin = int.TryParse(fields[2], out var stageBegin) ? stageBegin : 0;
                StageEnd = int.TryParse(fields[3], out var stageEnd) ? stageEnd : 0;
                StagesCount = StageEnd - StageBegin + 1;
            }
        }
    }

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

        public static bool TryGetStageId(this WorldSheet.Row worldRow, int stageId, out int stageNumber)
        {
            if (stageId < worldRow.StageBegin || stageId > worldRow.StageEnd)
            {
                stageNumber = 0;
                return false;
            }

            stageNumber = stageId - worldRow.StageBegin + 1;
            return true;
        }
    }
}
