using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class AssetDuplicationChecker : EditorWindow
{
    [MenuItem("Window/Asset Duplication Checker")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(AssetDuplicationChecker));
    }

    private void OnGUI()
    {
        if (GUILayout.Button("Check Duplicate Assets"))
        {
            CheckDuplicateAssets();
        }
    }

    private void CheckDuplicateAssets()
    {
        var path = AssetDatabase.GetAssetPath(Selection.activeObject.GetInstanceID());
        string[] allAssetPaths = AssetDatabase.GetAllAssetPaths();
        Dictionary<string, List<string>> assetDictionary = new Dictionary<string, List<string>>();

        foreach (string assetPath in allAssetPaths)
        {
            if (!assetPath.Contains(path)) continue;
            Object asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (asset != null)
            {
                string assetName = asset.name;
                string assetType = asset.GetType().ToString();
                string key = assetName + "_" + assetType;

                if (assetDictionary.ContainsKey(key))
                {
                    assetDictionary[key].Add(assetPath);
                }
                else
                {
                    assetDictionary[key] = new List<string> { assetPath };
                }
            }
        }

        foreach (var pair in assetDictionary)
        {
            if (pair.Value.Count > 1)
            {
                Debug.Log("Duplicate Asset Found: " + pair.Key);
                foreach (var assetPath in pair.Value)
                {
                    Debug.Log("Path: " + assetPath);
                }
            }
        }

        Debug.Log("Duplicate Asset Check Complete!");
    }
}
