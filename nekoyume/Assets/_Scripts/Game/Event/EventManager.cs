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

        private static EventScriptableObject SO =>
            _so ? _so : (_so = Resources.Load<EventScriptableObject>("ScriptableObject/EventData"));

        /// <summary>
        /// There is several event types in `EventScriptableObject`.
        /// This method returns the `EventInfo` by following logic.
        /// 1. Find time based event.
        /// 2. Find block index based event.
        /// 3. Find `EventDungeonSheet` id based event.
        /// 4. Return default settings.
        /// </summary>
        /// <returns></returns>
        private static EventInfo GetEventInfo()
        {
            var currentBlockIndex = Game.Game.instance.Agent.BlockIndex;
            var currentEventDungeonId = RxProps.EventDungeonRow?.Id ?? 0;
            return SO.timeBasedEvents.FirstOrDefault(e =>
                       Util.IsInTime(e.beginDateTime, e.endDateTime)) ??
                   SO.blockIndexBasedEvents.FirstOrDefault(e =>
                       e.beginBlockIndex <= currentBlockIndex &&
                       e.endBlockIndex >= currentBlockIndex) ??
                   SO.eventDungeonIdBasedEvents.FirstOrDefault(e =>
                       e.targetDungeonIds.Contains(currentEventDungeonId)) ??
                   SO.defaultSettings;
        }

        public static EventDungeonIdBasedEventInfo GetEventDungeonInfo()
        {
            return GetEventInfo() as EventDungeonIdBasedEventInfo;
        }
        public static bool TryGetEvent(EventType eventType, out EventInfo timeBasedEventInfo)
        {
            timeBasedEventInfo = SO.timeBasedEvents.FirstOrDefault(e => e.eventType == eventType);
            return timeBasedEventInfo is not null;
        }

        public static void UpdateEventContainer(Transform parent)
        {
            var eventContainers = parent.GetComponentsInChildren<EventContainer>(true);
            var activeType = GetEventType();
            foreach (var ec in eventContainers)
            {
                ec.gameObject.SetActive(ec.Type == activeType);
            }
        }

        public static Sprite GetIntroSprite()
        {
            return GetEventInfo().intro;
        }

        public static Sprite GetStageIcon()
        {
            return GetEventInfo().stageIcon;
        }

        public static Vector2 GetStageIconOffset()
        {
            return GetEventInfo().stageIconOffset;
        }

        public static string GetMainBgmName()
        {
            return GetEventInfo().mainBGM.name;
        }

        public static EventType GetEventType()
        {
            return GetEventInfo().eventType;
        }
    }
}
