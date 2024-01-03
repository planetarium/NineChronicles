using UnityEngine;
using System.IO;

namespace Nekoyume
{
    public class Platform
    {
        public static string PersistentDataPath
        {
            get
            {
#if UNITY_EDITOR
                return Application.persistentDataPath;
#elif UNITY_ANDROID
                return "data/data/com.planetariumlabs.ninechroniclesmobile";
#else
                return Application.persistentDataPath;
#endif
            }
        }

        public static string GetPersistentDataPath(string fileName)
        {
            Debug.Log($"[GetPersistentDataPath] {PersistentDataPath} , {fileName}");
            return Path.Combine(PersistentDataPath, fileName);
        }

        public static string StreamingAssetsPath => Application.streamingAssetsPath;

        public static string GetStreamingAssetsPath(string fileName)
        {
            return Path.Combine(StreamingAssetsPath, fileName);
        }

        public static string DataPath => Application.dataPath;

        public static string GetDataPath(string fileName)
        {
            return Path.Combine(DataPath, fileName);
        }

        public static RuntimePlatform CurrentPlatform => Application.platform;

        public static bool IsTargetPlatform(RuntimePlatform platform)
        {
            return CurrentPlatform == platform;
        }

        public static bool IsMobilePlatform()
        {
            return IsTargetPlatform(RuntimePlatform.Android) ||
                   IsTargetPlatform(RuntimePlatform.IPhonePlayer);
        }
    }
}
