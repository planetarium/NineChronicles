using System.IO;
using Nekoyume;
using Nekoyume.UI;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(TutorialScenarioScriptableObject))]
    public class TutorialScenarioScriptableObjectEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            GUILayout.Space(10);

            var tutorialScenario = (TutorialScenarioScriptableObject)target;

            if (GUILayout.Button("Export to JSON"))
            {
                if (tutorialScenario.tutorialScenarioForJson != null)
                {
                    var settings = new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore };
                    var jsonStr = JsonConvert.SerializeObject(tutorialScenario.tutorialScenarioForJson, Formatting.Indented, settings);

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
                    EditorUtility.DisplayDialog("Failed", "Import Failed! : tutorialScenario is null", "ok");
                }
            }

            if (GUILayout.Button("Import from JSON"))
            {
                if (tutorialScenario.json != null)
                {
                    tutorialScenario.tutorialScenarioForJson = JsonConvert.DeserializeObject<TutorialScenario>(tutorialScenario.json.text);

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
