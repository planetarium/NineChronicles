using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Nekoyume.AssetBundleHelper
{
    public static class AssetBundleLoader
    {
        private static readonly bool useCache = true;
        private static readonly Dictionary<string, AssetBundle> loadedAssetBundleCache = new();

        public static void LoadVFXCache()
        {
            var bundleName = "vfx/materials";
            if (!loadedAssetBundleCache.ContainsKey(bundleName))
            {
                if (!LoadFromFile(bundleName))
                {
                    Debug.LogError($"{bundleName} does not exist.");
                }
            }
        }

        public static T LoadAssetBundle<T>(string bundleName, string objectName)
            where T : UnityEngine.Object
        {
            if (!loadedAssetBundleCache.ContainsKey(bundleName))
            {
                if (!LoadFromFile(bundleName))
                {
                    Debug.LogError($"{bundleName} - {objectName} does not exist.");
                    return null;
                }
            }

            return loadedAssetBundleCache[bundleName].LoadAsset<T>(objectName);
        }

        public static T[] LoadAllAssetBundle<T>(string bundleName) where T : UnityEngine.Object
        {
            if (!loadedAssetBundleCache.ContainsKey(bundleName))
            {
                if (!LoadFromFile(bundleName))
                {
                    Debug.LogError($"{bundleName} does not exist.");
                    return null;
                }
            }

            return loadedAssetBundleCache[bundleName].LoadAllAssets<T>();
        }

        private static bool LoadFromFile(string bundleName)
        {
            // TODO: change path to downloaded assetbundle using UWR
            var path = Path.Combine(Application.streamingAssetsPath, bundleName);
            var loadedBundle = AssetBundle.LoadFromFile(path);
            if (loadedBundle == null)
            {
                Debug.LogError($"Failed to load Assetbundle at ${path} - ${bundleName}");
                return false;
            }

            if (useCache)
            {
                loadedAssetBundleCache[bundleName] = loadedBundle;
            }

            return true;
        }
    }
}
