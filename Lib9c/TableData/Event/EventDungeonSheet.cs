using System;

namespace Nekoyume.TableData.Event
{
    [Serializable]
    public class EventDungeonSheet : WorldSheet
    {
        public EventDungeonSheet() : base(nameof(EventDungeonSheet))
        {
        }
    }
}
