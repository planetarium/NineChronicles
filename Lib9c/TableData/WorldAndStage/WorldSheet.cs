using System;
using System.Collections.Generic;
using static Nekoyume.TableData.TableExtensions;

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
                Id = TryParseInt(fields[0], out var id) ? id : 0;
                Name = fields[1];
                StageBegin = TryParseInt(fields[2], out var stageBegin) ? stageBegin : 0;
                StageEnd = TryParseInt(fields[3], out var stageEnd) ? stageEnd : 0;
                StagesCount = StageEnd - StageBegin + 1;
            }
        }

        public WorldSheet() : base(nameof(WorldSheet))
        {
        }
    }
}
