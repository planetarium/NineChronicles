using System;
using System.Linq;
using Nekoyume.Helper;
using Nekoyume.State;
using UnityEngine;
using EventType = Nekoyume.EnumType.EventType;

namespace Nekoyume
{
    public static class EventManager
    {
        private static EventScriptableObject _so;

        private static EventScriptableObject SO => _so ? _so : _so = Resources.Load<EventScriptableObject>("ScriptableObject/EventData");

        /// <summary>
        /// There is several event types in `EventScriptableObject`.
        /// This method returns the `EventInfo` by following logic.
        /// 1. Find `EventDungeonSheet` id based event.
        /// 2. Find time based event.
        /// 3. Find block index based event.
        /// 4. Return default settings.
        /// </summary>
        /// <returns></returns>
        public static EventInfo GetEventInfo()
        {
            var currentBlockIndex = Game.Game.instance.Agent.BlockIndex;
            var currentEventDungeonId = RxProps.EventDungeonRow?.Id ?? 0;
            return SO.eventDungeonIdBasedEvents.FirstOrDefault(e =>
                    e.TargetDungeonIds.Contains(currentEventDungeonId)) ??
                SO.timeBasedEvents.FirstOrDefault(e =>
                    DateTime.UtcNow.IsInTime($"{DateTime.UtcNow.Year}-{e.BeginDateTime}",
                        $"{DateTime.UtcNow.Year}-{e.EndDateTime}")) ??
                SO.blockIndexBasedEvents.FirstOrDefault(e =>
                    e.BeginBlockIndex <= currentBlockIndex &&
                    e.EndBlockIndex >= currentBlockIndex) ??
                SO.defaultSettings;
        }

        public static void UpdateEventContainer(Transform parent)
        {
            var eventContainers = parent.GetComponentsInChildren<EventContainer>(true);
            var activeType = GetEventInfo().EventType;
            foreach (var ec in eventContainers)
            {
                ec.gameObject.SetActive(ec.Types.Contains(activeType));
            }
        }
    }
}
