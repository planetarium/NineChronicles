using System.IO;
using Nekoyume;
using Nekoyume.UI;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(TutorialPresetScriptableObject))]
    public class TutorialPresetScriptableObjectEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            GUILayout.Space(10);

            var tutorialScenario = (TutorialPresetScriptableObject)target;

            if (GUILayout.Button("Export to JSON"))
            {
                if (tutorialScenario.tutorialPreset != null)
                {
                    var settings = new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore };
                    var jsonStr = JsonConvert.SerializeObject(tutorialScenario.tutorialPreset, Formatting.Indented, settings);

                    if (tutorialScenario.json)
                    {
                        var path = AssetDatabase.GetAssetPath(tutorialScenario.json);
                        File.WriteAllText(path, jsonStr);
                        AssetDatabase.Refresh();
                        EditorUtility.DisplayDialog("success", "Apply Complete!", "ok");
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Failed", "Import Failed! : Please assign a TextAsset for the 'json' field first.", "ok");
                    }
                }
                else
                {
                    EditorUtility.DisplayDialog("Failed", "Import Failed! : tutorialPreset is null", "ok");
                }
            }

            if (GUILayout.Button("Import from JSON"))
            {
                if (tutorialScenario.json != null)
                {
                    tutorialScenario.tutorialPreset = JsonConvert.DeserializeObject<TutorialPreset>(tutorialScenario.json.text);

                    EditorUtility.SetDirty(tutorialScenario);
                    EditorUtility.DisplayDialog("Success", "Import Complete!", "ok");
                }
                else
                {
                    EditorUtility.DisplayDialog("Failed", "Import Failed! : json is null", "ok");
                }
            }
        }
    }
}
