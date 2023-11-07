using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class MarkAssetBundle
    {
        [MenuItem("Assets/AssetBundles/Mark Automatically", true)]
        public static bool TestValidation()
        {
            var obj = Selection.activeObject;
            var path = obj ? AssetDatabase.GetAssetPath(obj.GetInstanceID()) : "";

            return path.Length > 0 && Directory.Exists(path);
        }

        [MenuItem("Assets/AssetBundles/Mark Automatically")]
        public static void Test()
        {
            var path = AssetDatabase.GetAssetPath(Selection.activeObject.GetInstanceID());
            var bundleName = path.Split('/')[^1].ToLower();
            EnumerateSubdirectories(path, bundleName, bn =>
            {
                var assetPath = $"{string.Join('/', path.Split('/')[..^1])}/{bn}";
                var assetImporter = AssetImporter.GetAtPath(assetPath);
                assetImporter.SetAssetBundleNameAndVariant(bn, null);
                assetImporter.SaveAndReimport();
            });
        }

        private static void EnumerateSubdirectories(string root, string parentBundleName, Action<string> then)
        {
            then(parentBundleName);
            foreach (var dir in Directory.EnumerateDirectories(root))
            {
                var bundleName = $"{parentBundleName}/{dir.Split('/')[^1]}".ToLower();
                EnumerateSubdirectories(dir, bundleName, then);
            }
        }
    }
}
