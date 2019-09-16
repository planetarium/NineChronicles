using System;
using System.Collections.Generic;

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
            public int ChapterBegin { get; private set; }
            public int ChapterEnd { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = int.TryParse(fields[0], out var id) ? id : 0;
                Name = fields[1];
                ChapterBegin = int.TryParse(fields[2], out var chapterBegin) ? chapterBegin : 0;
                ChapterEnd = int.TryParse(fields[3], out var chapterEnd) ? chapterEnd : 0;
            }
        }

        public WorldSheet(string csv) : base(csv)
        {
        }
    }
}
