using Nekoyume.Game;
using Nekoyume.UI;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class TutorialTestingEditor : EditorWindow
    {
        private static string _tutorialIdInput;

        [MenuItem("Tools/Show Tutorial tester")]
        private static void Init()
        {
            _tutorialIdInput = "tutorial id(int)";
            var window = GetWindow(typeof(TutorialTestingEditor));
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("Show some tutorial.(Only runtime)");
            _tutorialIdInput = EditorGUILayout.TextField("Id of tutorial: ", _tutorialIdInput);
            if (GUILayout.Button("Show Tutorial"))
            {
                Debug.Log("Trying to show tutorial...");
                if (int.TryParse(_tutorialIdInput, out var id))
                {
                    Game.instance.Stage.TutorialController.Play(id);
                    Debug.Log($"Show tutorial: {id}");
                }
                else
                {
                    Debug.Log($"{_tutorialIdInput} is not number. plz input id of tutorial by number.");
                }
            }

            if (GUILayout.Button("Show Small Guide"))
            {
                Debug.Log("Trying to show small guide...");
                if (int.TryParse(_tutorialIdInput, out var id))
                {
                    Widget.Find<Tutorial>().PlaySmallGuide(id);
                    Debug.Log($"Show Small Guide: {id}");
                }
                else
                {
                    Debug.Log($"{_tutorialIdInput} is not number. plz input id of tutorial by number.");
                }
            }
        }
    }
}
