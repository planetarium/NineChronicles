using System;
using Nekoyume.Game.Controller;

namespace Nekoyume.TableData
{
    [Serializable]
    public class WorldChapter : Sheet<int, WorldChapter.Row>
    {
        [Serializable]
        public struct Row : ISheetRow<int>
        {
            public int Id { get; private set; }
            public string Name { get; private set; }
            public int StageBegin { get; private set; }
            public int StageEnd { get; private set; }
            public string Prefab { get; private set; }

            public int Key => Id;
            
            public void Set(string[] fields)
            {
                Id = int.TryParse(fields[0], out var stageId) ? stageId : 0;
                Name = fields[1];
                StageBegin = int.TryParse(fields[2], out var chapterBegin) ? chapterBegin : 0;
                StageEnd = int.TryParse(fields[3], out var chapterEnd) ? chapterEnd : 0;
                Prefab = fields[4];
            }
        }

        public bool TryGetByStage(int stage, out Row row)
        {
            foreach (var chapterRow in this)
            {
                if (stage < chapterRow.StageBegin ||
                    stage > chapterRow.StageEnd)
                {
                    continue;
                }
                
                row = chapterRow;

                return true;
            }

            row = new Row();
            return false;
        }
    }
}
