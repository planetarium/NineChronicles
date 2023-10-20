using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Nekoyume.AssetBundleHelper
{
    public static class AssetBundleLoader
    {
        private static readonly bool useCache = true;
        private static readonly Dictionary<string, AssetBundle> loadedAssetBundleCache = new();

        #region ASYNC_LOADING

        public static IEnumerator LoadAllAssetBundleAsync<T>(string bundleName,
            Action<float> onProgress, Action<T[]> onFinished) where T : UnityEngine.Object
        {
            return LoadFromFileAsync(bundleName, onProgress, assetBundle =>
            {
                var assets = assetBundle.LoadAllAssets<T>();
                onFinished(assets);
            });
        }

        public static IEnumerator LoadAssetBundleAsync<T>(string bundleName, string objectName,
            Action<float> onProgress, Action<T> onFinished) where T : UnityEngine.Object
        {
            return LoadFromFileAsync(bundleName, onProgress, assetBundle =>
            {
                var asset = assetBundle.LoadAsset<T>(objectName);
                onFinished(asset);
            });
        }

        private static IEnumerator LoadFromFileAsync(string bundleName, Action<float> onProgress,
            Action<AssetBundle> onFinished)
        {
            if (useCache && loadedAssetBundleCache.ContainsKey(bundleName))
            {
                onFinished(loadedAssetBundleCache[bundleName]);
                yield break;
            }

            var path = Path.Combine(Application.streamingAssetsPath, bundleName);
            var request = AssetBundle.LoadFromFileAsync(path);
            while
                (request.progress <
                 0.95) // condition checks like [progress < 1] does not work properly due to the Unity engine bug
            {
                onProgress(request.progress);
                yield return null;
            }

            yield return request;
            var loadedBundle = request.assetBundle;
            if (loadedBundle == null)
            {
                Debug.LogError($"Failed to load Assetbundle at ${path} - ${bundleName}");
                yield break;
            }

            if (useCache)
            {
                loadedAssetBundleCache[bundleName] = loadedBundle;
            }

            onFinished.Invoke(loadedBundle);
        }

        #endregion

        #region STATIC_LOADING

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

        #endregion
    }
}
