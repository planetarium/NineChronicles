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
                return $"data/data/{Application.identifier}";
#else
                return Application.persistentDataPath;
#endif
            }
        }

        public static string GetPersistentDataPath(string fileName)
        {
            NcDebug.Log($"[GetPersistentDataPath] {PersistentDataPath} , {fileName}");
            return Path.Combine(PersistentDataPath, fileName);
        }

        [RuntimeInitializeOnLoadMethod]
        private static void OnRuntimeMethodLoad()
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            Debug.Log("[OnRuntimeMethodLoad] After Scene is loaded and game is running");
            AndroidKeyStorePathChange();
#endif
        }

        /// <summary>
        /// Changes the path of the Android key store by copying the contents from
        /// "storage/emulated/0/Documents/NineChronicles" to the platform's persistent data path.
        /// After copying, it deletes any files in the new location that have the same names as those in the original path.
        /// This method is typically used for data migration or backup purposes in Android applications.
        /// </summary>
        private static void AndroidKeyStorePathChange()
        {
            CopyFolder("storage/emulated/0/Documents/NineChronicles", Platform.PersistentDataPath);
            DeleteSameFileName(Platform.PersistentDataPath, "storage/emulated/0/Documents/NineChronicles");
        }

        /// <summary>
        /// Copies a folder, including its files and subdirectories, from a specified source path to a target path.
        /// If the target directory does not exist, it is created. Each file copy operation is logged for debugging purposes.
        /// This method employs recursion to handle subdirectories, ensuring a complete copy of the folder structure.
        /// </summary>
        /// <param name="path">The source path of the folder to be copied.</param>
        /// <param name="target">The target path where the folder will be copied to.</param>
        private static void CopyFolder(string path, string target)
        {
            NcDebug.Log($"[CopyFolder] path:{path}    target:{target}");

            if (!Directory.Exists(target))
            {
                Directory.CreateDirectory(target);
                NcDebug.Log($"[Directory.CreateDirectory] target: {target}");
            }

            foreach (string file in Directory.GetFiles(path))
            {
                File.Copy(file, Path.Combine(target, Path.GetFileName(file)));
                NcDebug.Log($"[File.Copy] path:{Path.GetFileName(file)}    target:{target}");
            }

            foreach (string directory in Directory.GetDirectories(path))
            {
                CopyFolder(directory, Path.Combine(target, Path.GetFileName(directory)));
            }
        }

        /// <summary>
        /// Deletes files in the target directory that have the same names as those in the specified source directory.
        /// The method iterates over all files in the source path, including its subdirectories.
        /// For each file, it checks if a file with the same relative path exists in the target directory.
        /// If such a file exists, it is deleted. Each deletion operation is logged for debugging purposes.
        /// </summary>
        /// <param name="path">The source directory path to compare files from.</param>
        /// <param name="target">The target directory path where files will be checked and deleted if a match is found.</param>
        private static void DeleteSameFileName(string path, string target)
        {
            NcDebug.Log($"[DeleteCopiedFile] path:{path}    target:{target}");
            foreach (string file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
            {
                string relativePath = file.Substring(path.Length + 1);
                string targetFile = Path.Combine(target, relativePath);

                if (File.Exists(targetFile))
                {
                    File.Delete(targetFile);
                    NcDebug.Log($"[File.Delete] Deleted file: {targetFile}");
                }
            }
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
