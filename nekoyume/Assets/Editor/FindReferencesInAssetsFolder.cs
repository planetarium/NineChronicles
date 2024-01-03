using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class FindReferencesInAssetsFolder : MonoBehaviour
{
    [MenuItem("Assets/Find References In Assets Folder", false, 25)]
    public static void Find()
    {
        var referenceCache = new Dictionary<string, List<string>>();

        string[] guids = AssetDatabase.FindAssets("", new[] { "Assets" });
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            string[] dependencies = AssetDatabase.GetDependencies(assetPath, false);

            foreach (var dependency in dependencies)
            {
                if (referenceCache.ContainsKey(dependency))
                {
                    if (!referenceCache[dependency].Contains(assetPath))
                    {
                        referenceCache[dependency].Add(assetPath);
                    }
                }
                else
                {
                    referenceCache[dependency] = new List<string>() { assetPath };
                }
            }
        }

        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        Debug.Log("===== Find: " + path, Selection.activeObject);
        if (referenceCache.ContainsKey(path))
        {
            foreach (var reference in referenceCache[path])
            {
                Debug.Log(reference, AssetDatabase.LoadMainAssetAtPath(reference));
            }
        }
        else
        {
            Debug.LogWarning("No References");
        }

        referenceCache.Clear();
    }
}
