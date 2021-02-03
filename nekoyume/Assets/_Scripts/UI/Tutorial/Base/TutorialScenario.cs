using System;
using System.Text.Json.Serialization;

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
        public ScenarioData data;
    }

    [Serializable]
    public class ScenarioData
    {
        public int presetId;
        public TutorialTargetType targetType;
        public TutorialActionType actionType;
        public GuideType guideType;
        public DialogEmojiType emojiType;
        public string scriptKey;
    }
}
