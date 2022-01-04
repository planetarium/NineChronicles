using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume
{
    [CreateAssetMenu(fileName = "EventData", menuName = "Scriptable Object/Event Data",
        order = int.MaxValue)]
    public class EventScriptableObject : ScriptableObjectIncludeEnum<EnumType.EventType>
    {
        public EventInfo DefaultEvent;
        public List<EventInfo> Events;
    }

    [Serializable]
    public class EventInfo
    {
        [Tooltip("The type that describes the event")]
        public EnumType.EventType EventType;
        [Tooltip("DateTimeFormat(UTC):MM/dd/ HH:mm:ss [ex) 05/10 10:20:30]")]
        public string BeginDateTime;
        [Tooltip("DateTimeFormat(UTC):MM/dd/ HH:mm:ss [ex) 05/10 11:22:33]")]
        public string EndDateTime;
        [Tooltip("The sprite used by UI_IntroScreen.prefab")]
        public Sprite Intro;
        [Tooltip("The sprite used by WorldMapStage.prefab")]
        public Sprite StageIcon;
        [Tooltip("Value to modify step icon coordinates")]
        public Vector2 StageIconOffset;
        [Tooltip("Main lobby bgm. Reference only name of audio clip. Audio is managed by AudioController")]
        public AudioClip mainBGM;
    }
}
