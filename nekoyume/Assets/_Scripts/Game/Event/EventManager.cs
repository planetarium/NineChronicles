using System;
using System.Linq;
using UnityEngine;
using EventType = Nekoyume.EnumType.EventType;

namespace Nekoyume
{
    public static class EventManager
    {
        private static EventScriptableObject _so;

        private static EventScriptableObject SO =>
            _so ? _so : (_so = Resources.Load<EventScriptableObject>("ScriptableObject/EventData"));

        private static EventInfo GetEventInfo()
        {
            return SO.Events.FirstOrDefault(x => IsInTime(x.BeginDateTime, x.EndDateTime)) ??
                   SO.DefaultEvent;
        }

        public static bool TryGetArenaSeasonInfo(long blockIndex, out ArenaSeasonInfo info)
        {
            info = SO.ArenaSeasons.FirstOrDefault(x =>
                x.StartBlockIndex <= blockIndex && blockIndex <= x.EndBlockIndex);
            return info != null;
        }

        private static bool IsInTime(string begin, string end)
        {
            var n = DateTime.UtcNow;
            var b = $"{n.Year}/{begin}";
            var e = $"{n.Year}/{end}";
            var bDt = DateTime.ParseExact(b, "yyyy/MM/dd HH:mm:ss", null);
            var eDt = DateTime.ParseExact(e, "yyyy/MM/dd HH:mm:ss", null);
            var bDiff = (n - bDt).TotalSeconds;
            var eDiff = (eDt - n).TotalSeconds;
            return bDiff > 0 && eDiff > 0;
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
            return GetEventInfo().Intro;
        }

        public static Sprite GetStageIcon()
        {
            return GetEventInfo().StageIcon;
        }

        public static Vector2 GetStageIconOffset()
        {
            return GetEventInfo().StageIconOffset;
        }

        public static string GetMainBgmName()
        {
            return GetEventInfo().mainBGM.name;
        }

        public static EventType GetEventType()
        {
            return GetEventInfo().EventType;
        }
    }
}
