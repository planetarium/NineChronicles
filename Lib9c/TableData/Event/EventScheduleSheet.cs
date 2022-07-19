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
            public int DungeonTicketsMax { get; private set; }
            public int DungeonTicketsResetIntervalBlockRange { get; private set; }
            public int DungeonExpSeedValue { get; private set; }
            public long RecipeEndBlockIndex { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = ParseInt(fields[0], 0);
                Name = fields[1];
                StartBlockIndex = ParseLong(fields[2], 0L);
                DungeonEndBlockIndex = ParseLong(fields[3], 0L);
                DungeonTicketsMax = ParseInt(fields[4], 0);
                DungeonTicketsResetIntervalBlockRange = ParseInt(fields[5], 0);
                DungeonExpSeedValue = ParseInt(fields[6], 0);
                RecipeEndBlockIndex = ParseLong(fields[7], 0L);
            }
        }

        public EventScheduleSheet() : base(nameof(EventScheduleSheet))
        {
        }
    }
}
