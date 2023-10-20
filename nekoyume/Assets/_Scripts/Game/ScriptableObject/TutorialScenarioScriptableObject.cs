using Nekoyume.UI;
using UnityEngine;

namespace Nekoyume
{
    [CreateAssetMenu(fileName = "TutorialScenario", menuName = "Scriptable Object/Tutorial/Tutorial Scenario",
        order = int.MaxValue)]
    public class TutorialScenarioScriptableObject : ScriptableObject
    {
        public TutorialScenario tutorialScenario;

        public TextAsset json;
    }
}
