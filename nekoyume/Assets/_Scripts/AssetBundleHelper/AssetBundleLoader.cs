using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Primitives;
using UnityEngine;
using UnityEngine.Networking;

namespace Nekoyume.AssetBundleHelper
{
    public static class AssetBundleData
    {
        public static string AssetBundleURL
        {
            get
            {
                if (assetBundleURL == null)
                {
                    Initialize();
                }

                return assetBundleURL;
            }
        }

        public static IReadOnlyCollection<string> AssetBundleNames
        {
            get
            {
                if (assetBundleNames == null)
                {
                    Initialize();
                }

                return assetBundleNames;
            }
        }

        public static IReadOnlyCollection<string> PreloadNames
        {
            get
            {
                if (preloadNames == null)
                {
                    Initialize();
                }

                return preloadNames;
            }
        }

        private static string assetBundleURL;
        private static IReadOnlyCollection<string> assetBundleNames;
        private static IReadOnlyCollection<string> preloadNames;

        private static void Initialize()
        {
            var settings = Resources.Load<AssetBundleSettings>("AssetBundleSettings");
            assetBundleURL = settings.AssetBundleURL;
            assetBundleNames = settings.AssetBundleNames;
            preloadNames = settings.PreloadNames;
        }
    }

    public static class AssetBundleLoader
    {
        private static Dictionary<string, AssetBundle> preloaded = new();

        public static IEnumerator DownloadAssetBundles(string bundleName, Action<float> onProgress)
        {
            bundleName = bundleName.ToLower();
            using var www =
                UnityWebRequestAssetBundle.GetAssetBundle(
                    $"{AssetBundleData.AssetBundleURL}/{Application.version}/{GetPlatform()}/{bundleName}", GetVersion(), 0);
            var operation = www.SendWebRequest();

            while (!operation.isDone)
            {
                onProgress(www.downloadProgress);
                yield return null;
            }

            if (www.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var bundle = DownloadHandlerAssetBundle.GetContent(www);
                    if (AssetBundleData.PreloadNames.Contains(bundleName))
                    {
                        preloaded[bundleName] = bundle;
                    }
                    else
                    {
                        Unload(bundle);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning(e.Message);
                }
            }
            else
            {
                Debug.LogError($"AssetBundle Download failed: {www.error}");
            }
        }

        public static T LoadAssetBundle<T>(string bundleName, string objectName)
            where T : UnityEngine.Object
        {
            bundleName = bundleName.ToLower();
            var bundles = GetAssetBundle(bundleName);
            var obj = bundles[0].LoadAsset<T>(objectName);
            foreach (var bundle in bundles)
            {
                Unload(bundle);
            }

            return obj;
        }

        public static T[] LoadAllAssetBundle<T>(string bundleName) where T : UnityEngine.Object
        {
            bundleName = bundleName.ToLower();
            var bundles = GetAssetBundle(bundleName);
            var objs = bundles[0].LoadAllAssets<T>();
            foreach (var bundle in bundles)
            {
                Unload(bundle);
            }

            return objs;
        }

        private static IReadOnlyList<AssetBundle> GetAssetBundle(string bundleName, bool ignoreDeps=false)
        {
            var bundles = new List<AssetBundle>();
            if (preloaded.TryGetValue(bundleName, out var value))
            {
                bundles.Add(value);
                return bundles;
            }
            foreach (var loadedBundle in AssetBundle.GetAllLoadedAssetBundles())
            {
                if (loadedBundle.name == bundleName) return bundles;
            }

            using var www =
                UnityWebRequestAssetBundle.GetAssetBundle(
                    $"{AssetBundleData.AssetBundleURL}/{Application.version}/{GetPlatform()}/{bundleName}", GetVersion(), 0);
            var operation = www.SendWebRequest();

            while (!operation.isDone)
            {
            } // Technically, this MUST be done immediately.

            bundles.Add(DownloadHandlerAssetBundle.GetContent(www));
            if (!ignoreDeps)
            {
                bundles.AddRange(LoadDependencies(bundleName));
            }

            return bundles;
        }

        private static AssetBundleManifest manifest;
        private static IReadOnlyList<AssetBundle> LoadDependencies(string bundleName)
        {
            if (!manifest)
            {
                var bundle = GetAssetBundle(GetPlatform(), true)[0];
                manifest = bundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
                Unload(bundle);
            }

            var dependentBundleNames = new List<string>();
            dependentBundleNames.AddRange(manifest.GetAllDependencies(bundleName));

            while (true)
            {
                var currSize = dependentBundleNames.Count;
                for (var i = 0; i < currSize; i++)
                {
                    var dependency = dependentBundleNames[i];
                    foreach (var deepDep in manifest.GetAllDependencies(dependency))
                    {
                        if (!dependentBundleNames.Contains(deepDep)) dependentBundleNames.Add(deepDep);
                    }
                }

                if (currSize == dependentBundleNames.Count) break;
            }

            var dependencies = new List<AssetBundle>();
            foreach (var dependency in dependentBundleNames)
            {
                dependencies.AddRange(GetAssetBundle(dependency, true));
            }
            return dependencies;
        }

        private static void Unload(AssetBundle bundle)
        {
            if (AssetBundleData.PreloadNames.Contains(bundle.name)) return;
            bundle.Unload(false);
        }

        private static string GetPlatform()
        {
#if UNITY_EDITOR_WIN
            return "Windows";
#elif UNITY_EDITOR_OSX
            return "iOS";
#else
            return Application.platform switch
            {
                RuntimePlatform.Android => "Android",
                RuntimePlatform.IPhonePlayer => "iOS",
                _ => "Windows",
            };
#endif
        }

        private static uint GetVersion()
        {
            var version = Application.version;
            return BitConverter.ToUInt32(Encoding.ASCII.GetBytes(version));
        }
    }
}
