using System.IO;
using System.Linq;
using Lib9c.DevExtensions;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
#if UNITY_STANDALONE_OSX
using UnityEditor.OSXStandalone;
#endif

namespace Editor
{
    [ExecuteInEditMode]
    public class Builder {
        private static readonly string PlayerName = PlayerSettings.productName;
        private const string BuildBasePath = "Build";

        [MenuItem("Build/Standalone/Windows + macOS + Linux")]
        public static void BuildAll()
        {
#if UNITY_STANDALONE_OSX
            BuildMacOS();
            // BuildMacOSArm64();
#endif
            BuildWindows();
            BuildLinux();
        }

#if UNITY_STANDALONE_OSX
        [MenuItem("Build/Standalone/macOS (Intel)")]
        public static void BuildMacOS()
        {
            Debug.Log("Build macOS (Intel)");
            var originalArchitecture = UserBuildSettings.architecture;
            try
            {
                UserBuildSettings.architecture = MacOSArchitecture.x64;
                Build(BuildTarget.StandaloneOSX, targetDirName: "macOS");
            }
            finally
            {
                UserBuildSettings.architecture = originalArchitecture;
            }
        }

        /*
        // TODO: macOS Apple Silicon
        [MenuItem("Build/Standalone/macOS (Apple Silicon)")]
        public static void BuildMacOSArm64()
        {
            Debug.Log("Build macOS (Apple Silicon)");
            var originalArchitecture = UserBuildSettings.architecture;
            try
            {
                UserBuildSettings.architecture = MacOSArchitecture.ARM64;
                Build(BuildTarget.StandaloneOSX, targetDirName: "macOS (Apple Silicon)");
            }
            finally
            {
                UserBuildSettings.architecture = originalArchitecture;
            }
        }
        */
#endif

        [MenuItem("Build/Standalone/Windows")]
        public static void BuildWindows()
        {
            Debug.Log("Build Windows");
            Build(BuildTarget.StandaloneWindows64, targetDirName: "Windows");
        }

        [MenuItem("Build/Standalone/Linux")]
        public static void BuildLinux()
        {
            Debug.Log("Build Linux");
            Build(BuildTarget.StandaloneLinux64, targetDirName: "Linux");
        }

        [MenuItem("Build/Standalone/macOS Headless")]
        public static void BuildMacOSHeadless()
        {
            Debug.Log("Build macOS Headless");
            Build(BuildTarget.StandaloneOSX, BuildOptions.EnableHeadlessMode, "macOSHeadless");
        }

        [MenuItem("Build/Standalone/Linux Headless")]
        public static void BuildLinuxHeadless()
        {
            Debug.Log("Build Linux Headless");
            Build(BuildTarget.StandaloneLinux64, BuildOptions.EnableHeadlessMode, "LinuxHeadless");
        }

        [MenuItem("Build/Standalone/Windows Headless")]
        public static void BuildWindowsHeadless()
        {
            Debug.Log("Build Windows Headless");
            Build(BuildTarget.StandaloneWindows64, BuildOptions.EnableHeadlessMode, "WindowsHeadless");
        }

        [MenuItem("Build/Development/Windows + macOS + Linux")]
        public static void BuildAllDevelopment()
        {
            BuildMacOSDevelopment();
            BuildWindowsDevelopment();
            BuildLinuxDevelopment();
        }

        [MenuItem("Build/Development/macOS")]
        public static void BuildMacOSDevelopment()
        {
            Debug.Log("Build MacOS Development");
            Build(BuildTarget.StandaloneOSX, BuildOptions.Development | BuildOptions.AllowDebugging, "macOS");
        }

        [MenuItem("Build/Development/Windows")]
        public static void BuildWindowsDevelopment()
        {
            Debug.Log("Build Windows Development");
            Build(BuildTarget.StandaloneWindows64, BuildOptions.Development | BuildOptions.AllowDebugging, "Windows");
        }

        [MenuItem("Build/Development/Linux")]
        public static void BuildLinuxDevelopment()
        {
            Debug.Log("Build Linux Development");
            Build(BuildTarget.StandaloneLinux64, BuildOptions.Development | BuildOptions.AllowDebugging, "Linux");
        }

        [MenuItem("Build/Development/macOS Headless")]
        public static void BuildMacOSHeadlessDevelopment()
        {
            Debug.Log("Build macOS Headless Development");
            Build(BuildTarget.StandaloneOSX, BuildOptions.EnableHeadlessMode, "macOSHeadless");
        }

        [MenuItem("Build/Development/Linux Headless")]
        public static void BuildLinuxHeadlessDevelopment()
        {
            Debug.Log("Build Linux Headless Development");
            Build(
                BuildTarget.StandaloneLinux64,
                BuildOptions.EnableHeadlessMode | BuildOptions.Development | BuildOptions.AllowDebugging,
                "LinuxHeadless");
        }

        [MenuItem("Build/Standalone/Windows Headless Development")]
        public static void BuildWindowsHeadlessDevelopment()
        {
            Debug.Log("Build Windows Headless Development");
            Build(BuildTarget.StandaloneWindows64, BuildOptions.EnableHeadlessMode, "WindowsHeadless");
        }

        [MenuItem("Build/QA")]
        public static void BuildWindowsForQA()
        {
            Debug.Log("Build Windows For QA");
            CopyJsonDataFile("TestbedSell");
            CopyJsonDataFile("TestbedWeeklyArena");
            CopyJsonDataFile("TestbedCreateAvatar");
            Build(BuildTarget.StandaloneWindows64, BuildOptions.Development | BuildOptions.AllowDebugging, "Windows", true);
        }

        private static void Build(
            BuildTarget buildTarget,
            BuildOptions options = BuildOptions.None,
            string targetDirName = null,
            bool isDevelopment = false)
        {
            string[] scenes = { "Assets/_Scenes/Game.unity" };

            targetDirName ??= buildTarget.ToString();
            var locationPathName = Path.Combine(
                BuildBasePath,
                targetDirName,
                buildTarget.HasFlag(BuildTarget.StandaloneWindows64) ? $"{PlayerName}.exe" : PlayerName);

            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = locationPathName,
                target = buildTarget,
                options = EditorUserBuildSettings.development
                    ? options | BuildOptions.Development | BuildOptions.AllowDebugging
                    : options,
            };

            UpdateDefines(isDevelopment);

            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            var summary = report.summary;

            switch (summary.result)
            {
                case BuildResult.Succeeded:
                    Debug.Log("Build succeeded: " + summary.totalSize + " bytes");
                    UpdateDefines(false);
                    break;
                case BuildResult.Failed:
                    Debug.LogError("Build failed");
                    UpdateDefines(false);
                    break;
            }
        }

        private static void UpdateDefines(bool isDevelopment)
        {
            Debug.Log($"UpdateDefines : {isDevelopment}");
            var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            var preDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
            var newDefines = preDefines.Split( ';' ).ToList();
            const string qaDefine = "LIB9C_DEV_EXTENSIONS";
            if (isDevelopment)
            {
                if (!newDefines.Exists(x => x.Equals(qaDefine)))
                {
                    newDefines.Add(qaDefine);
                }
            }
            else
            {
                if (newDefines.Exists(x => x.Equals(qaDefine)))
                {
                    newDefines.Remove(qaDefine);
                }
            }
            PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup,
                string.Join(";", newDefines.ToArray()));
            EditorApplication.ExecuteMenuItem("File/Save Project");
        }

        private static void CopyJsonDataFile(string fileName)
        {
            var sourcePath = TestbedHelper.GetDataPath(fileName);
            var destPath = Path.Combine(Application.streamingAssetsPath, $"{fileName}.json");
            File.Copy(sourcePath, destPath, true);
            Debug.Log($"Copy json data file : {fileName}");
        }

        [PostProcessBuild(0)]
        public static void CopyNativeLibraries(BuildTarget target, string pathToBuiltProject)
        {
            var binaryName = Path.GetFileNameWithoutExtension(pathToBuiltProject);
            var destLibPath = pathToBuiltProject;
            var libDir = "runtimes";
            switch (target)
            {
                case BuildTarget.StandaloneOSX:
                    libDir = Path.Combine(libDir, "osx-x64", "native");
                    if (!destLibPath.EndsWith(".app"))
                    {
                        destLibPath += ".app";
                    }
                    destLibPath = Path.Combine(
                        destLibPath, "Contents/Resources/Data/Managed/", libDir);
                    break;
                case BuildTarget.StandaloneWindows64:
                    libDir = Path.Combine(libDir, "win-x64", "native");
                    destLibPath = Path.Combine(
                        Path.GetDirectoryName(destLibPath), $"{binaryName}_Data/Managed", libDir);
                    break;
                default:
                    libDir = Path.Combine(libDir, "linux-x64", "native");
                    destLibPath = Path.Combine(
                        Path.GetDirectoryName(destLibPath), $"{binaryName}_Data/Managed", libDir);
                    break;
            }

            var srcLibPath = Path.Combine(Application.dataPath, "Packages", libDir);
            var src = new DirectoryInfo(srcLibPath);
            var dest = new DirectoryInfo(destLibPath);

            Directory.CreateDirectory(dest.FullName);

            foreach (var fileInfo in src.GetFiles())
            {
                if (fileInfo.Extension == ".meta")
                {
                    continue;
                }

                fileInfo.CopyTo(Path.Combine(dest.FullName, fileInfo.Name), true);
            }
        }

        private static void CopyToBuildDirectory(string basePath, string targetDirName, string filename)
        {
            if (filename == null) return;
            var source = Path.Combine(basePath, filename);
            var destination = Path.Combine(BuildBasePath, targetDirName, filename);
            File.Copy(source, destination, true);
        }
    }
}
