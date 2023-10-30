using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class BuildAssetBundle
{
    private static readonly string assetBundleDirectory = "Assets/AssetBundles";

    [MenuItem("Assets/Build AssetBundles/All")]
    public static void BuildAllAssetBundles()
    {
        BuildAllAssetBundlesIOS();
        BuildAllAssetBundlesAndroid();
        BuildAllAssetBundlesWindows();
    }

    [MenuItem("Assets/Build AssetBundles/iOS")]
    public static void BuildAllAssetBundlesIOS()
    {
        BuildAllAssetBundles($"{assetBundleDirectory}/{Application.version}/iOS", BuildTarget.iOS);
    }

    [MenuItem("Assets/Build AssetBundles/Android")]
    public static void BuildAllAssetBundlesAndroid()
    {
        BuildAllAssetBundles($"{assetBundleDirectory}/{Application.version}/Android",
            BuildTarget.Android);
    }

    [MenuItem("Assets/Build AssetBundles/Windows")]
    public static void BuildAllAssetBundlesWindows()
    {
        BuildAllAssetBundles($"{assetBundleDirectory}/{Application.version}/Windows",
            BuildTarget.StandaloneWindows);
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
    }
}
