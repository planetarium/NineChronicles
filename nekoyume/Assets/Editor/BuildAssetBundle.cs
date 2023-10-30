using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class BuildAssetBundle
{
    private static readonly string assetBundleDirectory = "Assets/AssetBundles";

    [MenuItem("Assets/Build AssetBundles/iOS")]
    public static void BuildAllAssetBundlesIOS()
    {
        BuildAllAssetBundles(assetBundleDirectory + "/iOS", BuildTarget.iOS);
    }

    [MenuItem("Assets/Build AssetBundles/Android")]
    public static void BuildAllAssetBundlesAndroid()
    {
        BuildAllAssetBundles(assetBundleDirectory + "/Android", BuildTarget.Android);
    }

    [MenuItem("Assets/Build AssetBundles/Windows")]
    public static void BuildAllAssetBundlesWindows()
    {
        BuildAllAssetBundles(assetBundleDirectory + "/Windows", BuildTarget.StandaloneWindows);
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
