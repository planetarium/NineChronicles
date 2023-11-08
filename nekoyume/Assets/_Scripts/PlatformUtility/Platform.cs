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
#if !UNITY_EDITOR && UNITY_ANDROID
                return "storage/emulated/0/Documents/NineChronicles";
#else
                return Application.persistentDataPath;
#endif
            }
        }

        public static string GetPersistentDataPath(string fileName)
        {
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
