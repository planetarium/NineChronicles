using System.IO;
using UnityEngine;
using UnityEditor;

namespace Editor
{
    public class AssetBundleNameWindow : EditorWindow
    {
        private static readonly string assetBundleDirectory = "Assets/AssetBundles";

        private string _name = "";

        [MenuItem("Window/AssetBundle Name")]
        public static void ShowAssetBundleNameWindow()
        {
            GetWindow(typeof(AssetBundleNameWindow));
        }

        private void OnGUI()
        {
            GUILayout.Label("AssetBundle Name");
            _name = GUILayout.TextField(_name);
            if (GUILayout.Button("Set AssetBundle Name"))
            {
                SetAssetBundleName(_name);
            }

            GUILayout.Label("Build");

            if (GUILayout.Button("Build AssetBundle for iOS"))
            {
                BuildAssetBundles($"{assetBundleDirectory}/{Application.version}/iOS", BuildTarget.iOS);
            }

            if (GUILayout.Button("Build AssetBundle for Android"))
            {
                BuildAssetBundles($"{assetBundleDirectory}/{Application.version}/Android", BuildTarget.Android);
            }

            if (GUILayout.Button("Build AssetBundle for Windows"))
            {
                BuildAssetBundles($"{assetBundleDirectory}/{Application.version}/Windows", BuildTarget.StandaloneWindows);
            }
        }

        static void SetAssetBundleName(string assetName)
        {
            Debug.Log(assetName);
            if (Selection.assetGUIDs.Length > 0)
            {
                int count = 0;
                foreach (Object asset in Selection.objects)
                {
                    string path = AssetDatabase.GetAssetPath(asset);
                    string[] files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
                    foreach (string file in files)
                    {
                        if (Path.GetExtension(file).Equals(".meta"))
                        {
                            continue;
                        }
                        AssetImporter assetImporter = AssetImporter.GetAtPath(file);
                        assetImporter.assetBundleName = assetName;
                        count++;
                    }
                }
                Debug.Log(count + " Asset Bundles Assigned");
            }
            else
            {
                Debug.Log("No Assets Selected");
            }
        }

        private void BuildAssetBundles(string path, BuildTarget target)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            AssetBundleBuild[] buildBundles = new AssetBundleBuild[1];
            var bundleName = _name;
            buildBundles[0].assetBundleName = bundleName;
            buildBundles[0].assetNames = AssetDatabase.GetAssetPathsFromAssetBundle(bundleName);
            BuildPipeline.BuildAssetBundles(path, buildBundles, BuildAssetBundleOptions.None, target);
        }
    }
}
