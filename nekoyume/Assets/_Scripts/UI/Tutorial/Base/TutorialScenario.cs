using System;
using Newtonsoft.Json.Converters;
using UnityEngine;

namespace Nekoyume.UI
{
    [Serializable]
    public class TutorialScenario
    {
        public Scenario[] scenario;
    }

    [Serializable]
    public class Scenario
    {
        public int id;
        public int nextId;
        public int checkPointId;
        public ScenarioData data;
    }

    [Serializable]
    public class ScenarioData
    {
        public int presetId;
        public Vector2 arrowPositionOffset;
        public Vector2 targetPositionOffset;
        public Vector2 targetSizeOffset;
        public Vector4 buttonRaycastPadding;

        [Newtonsoft.Json.JsonConverter(typeof(StringEnumConverter))]
        public TutorialTargetType targetType;
        [Newtonsoft.Json.JsonConverter(typeof(StringEnumConverter))]
        public TutorialActionType actionType;
        [Newtonsoft.Json.JsonConverter(typeof(StringEnumConverter))]
        public GuideType guideType;
        [Newtonsoft.Json.JsonConverter(typeof(StringEnumConverter))]
        public DialogEmojiType emojiType;
        public string scriptKey;
        public float arrowAdditionalDelay;
        public bool fullScreenButton;
        public bool noArrow;
    }
}
