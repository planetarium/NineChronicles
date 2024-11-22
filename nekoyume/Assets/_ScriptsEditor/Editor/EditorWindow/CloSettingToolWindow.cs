using System;
using System.Collections;
using System.IO;
using Nekoyume;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace NekoyumeEditor
{
    public class CloSettingToolWindow : EditorWindow
    {
        private const string SAVE_FILE_PREFIX = "clo-";
        private bool _currentCLOSettingEditState = false;
        private string _lastEditJson;
        private string _saveFileName = SAVE_FILE_PREFIX;
        private Vector2 _scrollPosition;

        [MenuItem("Tools/CLO_Settings")]
        private static void Init()
        {
            GetWindow<CloSettingToolWindow>("CLO_Settings", true).Show();
        }

        public IEnumerator GetJson(string url, Action<string> onSuccess, Action<UnityWebRequest> onFailed = null)
        {
            EditorUtility.DisplayProgressBar("Download CLO Settings", "Download CLO Settings...", 0);
            using var request = UnityWebRequest.Get(url);
            request.timeout = 3;
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                onSuccess(request.downloadHandler.text);
            }
            else
            {
                onFailed?.Invoke(request);
            }

            Repaint();
            EditorUtility.ClearProgressBar();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            if (_currentCLOSettingEditState)
            {
                var prev = GUI.backgroundColor;
                GUI.backgroundColor = Color.green;
                _lastEditJson = EditorGUILayout.TextArea(_lastEditJson, EditorStyles.textArea);
                GUI.backgroundColor = prev;
            }
            else
            {
                EditorGUILayout.LabelField("Current clo.json View", EditorStyles.boldLabel);
                if (File.Exists(Platform.GetStreamingAssetsPath("clo.json")))
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.TextArea(File.ReadAllText(Platform.GetStreamingAssetsPath("clo.json")), EditorStyles.textArea);
                    EditorGUI.EndDisabledGroup();
                }
                else
                {
                    EditorGUILayout.TextField("Can't find clo file");
                }
            }

            if (_currentCLOSettingEditState == false)
            {
                if (GUILayout.Button("Edit"))
                {
                    _currentCLOSettingEditState = true;
                    _lastEditJson = File.ReadAllText(Platform.GetStreamingAssetsPath("clo.json"));
                }
            }
            else
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("FileName");

                if (_saveFileName.Length < SAVE_FILE_PREFIX.Length)
                {
                    _saveFileName = SAVE_FILE_PREFIX;
                }

                if (!_saveFileName.Substring(0, SAVE_FILE_PREFIX.Length).Contains(SAVE_FILE_PREFIX))
                {
                    _saveFileName = SAVE_FILE_PREFIX + _saveFileName;
                }

                _saveFileName = GUILayout.TextField(_saveFileName);

                if (GUILayout.Button("Save"))
                {
                    File.WriteAllText(Platform.GetStreamingAssetsPath(_saveFileName + ".json"), _lastEditJson);
                    File.WriteAllText(Platform.GetStreamingAssetsPath("clo.json"), _lastEditJson);
                    _saveFileName = SAVE_FILE_PREFIX;
                    _currentCLOSettingEditState = false;
                }

                GUILayout.EndHorizontal();

                if (GUILayout.Button("Exit"))
                {
                    _currentCLOSettingEditState = false;
                }
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Explore To"))
            {
                EditorUtility.RevealInFinder(Platform.GetStreamingAssetsPath("clo.json"));
            }

            GUILayout.Space(10);

            EditorGUILayout.LabelField("RemoteSettings", EditorStyles.boldLabel);
            if (GUILayout.Button("Internal Launcher CLO Setting"))
            {
                EditorCoroutineUtility.StartCoroutineOwnerless(GetJson("https://release.nine-chronicles.com/internal/config.json", (cloData) => { File.WriteAllText(Platform.GetStreamingAssetsPath("clo.json"), cloData); }));
            }

            if (GUILayout.Button("Main Launcher CLO Setting"))
            {
                EditorCoroutineUtility.StartCoroutineOwnerless(GetJson("https://download.nine-chronicles.com/9c-launcher-config.json", (cloData) => { File.WriteAllText(Platform.GetStreamingAssetsPath("clo.json"), cloData); }));
            }

            GUILayout.Space(10);

            if (_currentCLOSettingEditState)
            {
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.LabelField("LocalFileSettings", EditorStyles.boldLabel);
            GUILayout.BeginVertical();
            foreach (var filePath in Directory.GetFiles(Platform.StreamingAssetsPath))
            {
                if (filePath.Contains("clo") && filePath.Contains(".json") && !filePath.Contains(".meta") && !filePath.Contains("clo.json"))
                {
                    var labelName = filePath.Replace(Platform.StreamingAssetsPath + "\\", "").Replace(".json", "");
                    if (GUILayout.Button(labelName))
                    {
                        _lastEditJson = File.ReadAllText(filePath);
                        File.WriteAllText(Platform.GetStreamingAssetsPath("clo.json"), _lastEditJson);
                    }
                }
            }

            GUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }
    }
}
