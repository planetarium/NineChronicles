using Nekoyume.UI;
using UnityEngine;

namespace Nekoyume
{
    [CreateAssetMenu(fileName = "TutorialPreset", menuName = "Scriptable Object/Tutorial/TutorialPreset", order = int.MaxValue)]
    public class TutorialPresetScriptableObject : ScriptableObject
    {
        public TutorialPreset tutorialPreset;

        public TextAsset json;
    }
}
