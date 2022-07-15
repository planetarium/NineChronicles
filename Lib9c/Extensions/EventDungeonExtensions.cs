using System;

namespace Nekoyume.Extensions
{
    public static class EventDungeonExtensions
    {
        public static int ToEventScheduleId(this int eventDungeonId)
        {
            if (eventDungeonId < 10_000_000 ||
                eventDungeonId > 99_999_999)
            {
                throw new ArgumentException(
                    "DungeonId must be between 10000000 and 99999999.");
            }

            return eventDungeonId / 10_000;
        }
    }
}
