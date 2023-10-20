using Nekoyume.Game.VFX.Skill;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace Nekoyume.AssetBundleHelper
{
    public static class AssetBundleLoader
    {
        private static readonly bool useCache = true;
        private static readonly Dictionary<string, AssetBundle> loadedAssetBundleCache = new();

        #region ASYNC_DOWNLOADING

        public static IEnumerator DownloadAssetBundles(string assetBundleURL, string bundleName,
            Action<float> onProgress)
        {
            if (loadedAssetBundleCache.ContainsKey(bundleName)) yield break;

            using var www =
                UnityWebRequestAssetBundle.GetAssetBundle($"{assetBundleURL}/{bundleName}");
            var operation = www.SendWebRequest();

            while (!operation.isDone)
            {
                onProgress(www.downloadProgress);
                yield return null;
            }

            if (www.result == UnityWebRequest.Result.Success)
            {
                var bundle = DownloadHandlerAssetBundle.GetContent(www);
                loadedAssetBundleCache[bundleName] = bundle;
            }
            else
            {
                Debug.LogError($"AssetBundle Download failed: {www.error}");
            }
        }

        #endregion

        #region ASYNC_LOADING

        public static IEnumerator LoadAllAssetBundleAsync<T>(string bundleName,
            Action<T[]> onFinished) where T : UnityEngine.Object
        {
            return LoadFromFileAsync(bundleName, assetBundle =>
            {
                var assets = assetBundle.LoadAllAssets<T>();
                onFinished(assets);
            });
        }

        public static IEnumerator LoadAssetBundleAsync<T>(string bundleName, string objectName,
            Action<T> onFinished) where T : UnityEngine.Object
        {
            return LoadFromFileAsync(bundleName, assetBundle =>
            {
                var asset = assetBundle.LoadAsset<T>(objectName);
                onFinished(asset);
            });
        }

        private static IEnumerator LoadFromFileAsync(string bundleName,
            Action<AssetBundle> onFinished)
        {
            if (useCache && loadedAssetBundleCache.ContainsKey(bundleName))
            {
                onFinished(loadedAssetBundleCache[bundleName]);
                yield break;
            }

            var path = Path.Combine(Application.streamingAssetsPath, bundleName);
            var request = AssetBundle.LoadFromFileAsync(path);
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
