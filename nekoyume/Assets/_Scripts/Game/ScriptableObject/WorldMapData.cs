using System;
using System.Linq;
using UnityEngine;

namespace Nekoyume
{
    [CreateAssetMenu(
        fileName = "WorldMapData",
        menuName = "Scriptable Object/World Map Data",
        order = int.MaxValue)]
    public class WorldMapData : ScriptableObject
    {
        [Serializable]
        public class Icon
        {
            public Sprite sprite;
            public Vector2 offset;
        }

        [Serializable]
        public class StageIcon
        {
            public Icon icon;
            public Color selectedColor;
        }

        [Serializable]
        public class BossStageIcon : StageIcon
        {
            public EnumType.BossType bossType;
        }

        [Serializable]
        public class EventStageIcon : StageIcon
        {
            public EnumType.EventType eventType;
        }

        [Serializable]
        public class BossMarkIcon
        {
            public EnumType.BossType bossType;
            public Icon icon;
        }

        [Header("Stage Icons")]
        public StageIcon defaultStageIcon;
        public BossStageIcon[] bossStageIcons;
        public EventStageIcon[] eventStageIcons;

        [Header("Mark Icons")]
        public BossMarkIcon[] bossMarkIcons;
    }
}
