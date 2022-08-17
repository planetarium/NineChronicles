using System;

namespace Nekoyume.TableData.Event
{
    [Serializable]
    public class EventDungeonSheet : Sheet<int, EventDungeonSheet.Row>
    {
        [Serializable]
        public class Row : WorldSheet.Row
        {
        }

        public EventDungeonSheet() : base(nameof(EventDungeonSheet))
        {
        }
    }
}
