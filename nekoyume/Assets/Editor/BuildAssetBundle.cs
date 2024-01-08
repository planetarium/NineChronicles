using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public static class BuildAssetBundle
    {
        private static readonly string assetBundleDirectory = "Assets/AssetBundles";

        [MenuItem("Build/AssetBundles/All")]
        public static void BuildAllAssetBundles()
        {
            BuildAllAssetBundlesIOS();
            BuildAllAssetBundlesAndroid();
            BuildAllAssetBundlesWindows();
        }

        [MenuItem("Build/AssetBundles/iOS")]
        public static void BuildAllAssetBundlesIOS()
        {
            BuildAllAssetBundles($"{assetBundleDirectory}/{Application.version}/iOS", BuildTarget.iOS);
        }

        [MenuItem("Build/AssetBundles/Android")]
        public static void BuildAllAssetBundlesAndroid()
        {
            BuildAllAssetBundles($"{assetBundleDirectory}/{Application.version}/Android",
                BuildTarget.Android);
        }

        [MenuItem("Build/AssetBundles/Windows")]
        public static void BuildAllAssetBundlesWindows()
        {
            BuildAllAssetBundles($"{assetBundleDirectory}/{Application.version}/Windows",
                BuildTarget.StandaloneWindows);
        }

        [MenuItem("Build/AssetBundles/Delete Cache")]
        public static void DeleteCache()
        {
            UnityEngine.Caching.ClearCache();
        }

        private static void BuildAllAssetBundles(string path, BuildTarget target)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            BuildPipeline.BuildAssetBundles(path,
                BuildAssetBundleOptions.None,
                target);

            AssetBundleBuild[] buildBundles = new AssetBundleBuild[1];
            var bundleName = "video";
            buildBundles[0].assetBundleName = bundleName;
            buildBundles[0].assetNames = AssetDatabase.GetAssetPathsFromAssetBundle(bundleName);
            BuildPipeline.BuildAssetBundles(path, buildBundles, BuildAssetBundleOptions.UncompressedAssetBundle, target);
        }
    }
}
