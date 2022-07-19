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
                    $"{nameof(eventDungeonId)}({eventDungeonId}) must be" +
                    " between 10,000,000 and 99,999,999.");
            }

            return eventDungeonId / 10_000;
        }
    }
}
