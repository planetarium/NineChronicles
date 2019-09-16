using System;
using System.Collections.Generic;
using System.Linq;

namespace Nekoyume.TableData
{
    [Serializable]
    public class WorldChapterSheet : Sheet<int, WorldChapterSheet.Row>
    {
        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => Id;
            public int Id { get; private set; }
            public string Name { get; private set; }
            public int StageBegin { get; private set; }
            public int StageEnd { get; private set; }
            public string Prefab { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = int.TryParse(fields[0], out var id) ? id : 0;
                Name = fields[1];
                StageBegin = int.TryParse(fields[2], out var chapterBegin) ? chapterBegin : 0;
                StageEnd = int.TryParse(fields[3], out var chapterEnd) ? chapterEnd : 0;
                Prefab = fields[4];
            }
        }

        public bool TryGetByStage(int stage, out Row outRow)
        {
            var orderedList = ToOrderedList();
            foreach (var row in orderedList)
            {
                if (stage < row.StageBegin
                    || stage > row.StageEnd)
                {
                    continue;
                }

                outRow = row;

                return true;
            }

            outRow = orderedList.Last();

            return true;
        }

        public WorldChapterSheet(string csv) : base(csv)
        {
        }
    }
}
