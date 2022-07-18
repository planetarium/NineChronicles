using System;
using System.Collections.Generic;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData.Event
{
    public class EventScheduleSheet : Sheet<int, EventScheduleSheet.Row>
    {
        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => Id;
            public int Id { get; private set; }
            public string Name { get; private set; }
            public long StartBlockIndex { get; private set; }
            public long DungeonEndBlockIndex { get; private set; }
            public long RecipeEndBlockIndex { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = ParseInt(fields[0], 0);
                Name = fields[1];
                StartBlockIndex = ParseLong(fields[2], 0L);
                DungeonEndBlockIndex = ParseLong(fields[3], 0L);
                RecipeEndBlockIndex = ParseLong(fields[4], 0L);
            }
        }

        public EventScheduleSheet() : base(nameof(EventScheduleSheet))
        {
        }
    }
}
