using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume
{
    [CreateAssetMenu(
        fileName = "EventData",
        menuName = "Scriptable Object/Event Data",
        order = int.MaxValue)]
    public class EventScriptableObject : ScriptableObjectIncludeEnum<EnumType.EventType>
    {
        public EventInfo defaultSettings;
        public List<TimeBasedEventInfo> timeBasedEvents;
        public List<BlockIndexBasedEventInfo> blockIndexBasedEvents;
        public List<EventDungeonIdBasedEventInfo> eventDungeonIdBasedEvents;
    }

    [Serializable]
    public class EventInfo
    {
        [Tooltip("The type that describes the event")]
        public EnumType.EventType eventType;

        [Tooltip("The sprite used by UI_IntroScreen.prefab")]
        public Sprite intro;

        [Tooltip("The sprite used by WorldMapStage.prefab")]
        public Sprite stageIcon;

        [Tooltip("Value to modify step icon coordinates")]
        public Vector2 stageIconOffset;

        [Tooltip(
            "Main lobby bgm. Reference only name of audio clip. Audio is managed by AudioController")]
        public AudioClip mainBGM;
    }

    [Serializable]
    public class TimeBasedEventInfo : EventInfo
    {
        [Tooltip("DateTimeFormat(UTC):MM/dd/ HH:mm:ss (E.g: 05/10 10:20:30)")]
        public string beginDateTime;

        [Tooltip("DateTimeFormat(UTC):MM/dd/ HH:mm:ss (E.g: 05/10 11:22:33)")]
        public string endDateTime;
    }

    [Serializable]
    public class BlockIndexBasedEventInfo : EventInfo
    {
        [Tooltip("Beginning block index")]
        public long beginBlockIndex;

        [Tooltip("End block index")]
        public long endBlockIndex;
    }

    [Serializable]
    public class EventDungeonIdBasedEventInfo : EventInfo
    {
        [Tooltip("ID list of `EventDungeonSheet`")]
        public int[] targetDungeonIds;

        [Tooltip("The Key used by WorldMapPage in WorldMapWorld.prefab")]
        public string eventDungeonKey;

        [Tooltip("The sprite used by GuidedQuestCell.prefab as event dungeon icon")]
        public Sprite eventDungeonGuidedQuestIcon;

        [Tooltip("The sprite used by GuideQuestCell.prefab as event recipe icon")]
        public Sprite eventRecipeGuidedQuestIcon;

        [Tooltip("The sprite used by UI_BattlePreparation.prefab")]
        public Sprite eventDungeonBattlePreparationBg;
    }
}
