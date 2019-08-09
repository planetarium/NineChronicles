using System;
using Nekoyume.Game.Controller;

namespace Nekoyume.TableData
{
    [Serializable]
    public class World : Sheet<int, World.Row>
    {
        [Serializable]
        public struct Row : ISheetRow<int>
        {
            public int Id { get; private set; }
            public string Name { get; private set; }
            public int ChapterBegin { get; private set; }
            public int ChapterEnd { get; private set; }

            public int Key => Id;
            
            public void Set(string[] fields)
            {
                Id = int.TryParse(fields[0], out var stageId) ? stageId : 0;
                Name = fields[1];
                ChapterBegin = int.TryParse(fields[2], out var chapterBegin) ? chapterBegin : 0;
                ChapterEnd = int.TryParse(fields[3], out var chapterEnd) ? chapterEnd : 0;
            }
        }
    }
}
