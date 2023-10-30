using System;
using Nekoyume.UI;
using UnityEngine;

namespace Nekoyume
{
    [CreateAssetMenu(fileName = "TutorialScenario", menuName = "Scriptable Object/Tutorial/Tutorial Scenario",
        order = int.MaxValue)]
    public class TutorialScenarioScriptableObject : ScriptableObject
    {
        public TutorialScenario tutorialScenario;

        public TutorialScenarioWithComment[] tutorialScenarioWithComments;

        public ScenarioTemplate[] scenarioTemplates;

        public TextAsset json;

        [Serializable]
        public struct ScenarioTemplate
        {
            public string description;
            public ScenarioData scenarioData;
        }

        [Serializable]
        public struct TutorialScenarioWithComment
        {
            public string title;
            public string description;
            public TutorialScenario tutorialScenario;
        }
    }
}
