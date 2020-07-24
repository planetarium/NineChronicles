using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;

namespace Editor
{
    public class Builder {

        public static string PlayerName = PlayerSettings.productName;

        public const string BuildBasePath = "Build";

        public static readonly string ProjectBasePath = Path.Combine(Application.dataPath, "..", "..");

        [MenuItem("Build/Standalone/Windows + macOS + Linux")]
        public static void BuildAll()
        {
            BuildMacOS();
            BuildWindows();
            BuildLinux();
        }

        [MenuItem("Build/Standalone/macOS")]
        public static void BuildMacOS()
        {
            Debug.Log("Build macOS");
            Build(BuildTarget.StandaloneOSX, targetDirName: "macOS", scriptName: "run", snapshotName: "NineChroniclesSnapshot");
        }

        [MenuItem("Build/Standalone/Windows")]
        public static void BuildWindows()
        {
            Debug.Log("Build Windows");
            Build(BuildTarget.StandaloneWindows64, targetDirName: "Windows", scriptName: "run.bat", snapshotName: "NineChroniclesSnapshot.exe");
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
            Build(BuildTarget.StandaloneOSX, BuildOptions.Development, targetDirName: "macOS", scriptName: "run", snapshotName: "NineChroniclesSnapshot");
        }

        [MenuItem("Build/Development/Windows")]
        public static void BuildWindowsDevelopment()
        {
            Debug.Log("Build Windows Development");
            Build(BuildTarget.StandaloneWindows64, BuildOptions.Development, targetDirName: "Windows", scriptName: "run.bat", snapshotName: "NineChroniclesSnapshot.exe");
        }

        [MenuItem("Build/Development/Linux")]
        public static void BuildLinuxDevelopment()
        {
            Debug.Log("Build Linux Development");
            Build(BuildTarget.StandaloneLinux64, BuildOptions.Development, targetDirName: "Linux");
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
                BuildOptions.EnableHeadlessMode | BuildOptions.Development,
                "LinuxHeadless");
        }

        [MenuItem("Build/Standalone/Windows Headless Development")]
        public static void BuildWindowsHeadlessDevelopment()
        {
            Debug.Log("Build Windows Headless Development");
            Build(BuildTarget.StandaloneWindows64, BuildOptions.EnableHeadlessMode, "WindowsHeadless");
        }

        public static void Build(
            BuildTarget buildTarget,
            BuildOptions options = BuildOptions.None,
            string targetDirName = null,
            string scriptName = null,
            string snapshotName = null)
        {
            string[] scenes = { "Assets/_Scenes/Game.unity" };

            targetDirName = targetDirName ?? buildTarget.ToString();
            string locationPathName = Path.Combine(
                BuildBasePath,
                targetDirName,
                buildTarget.HasFlag(BuildTarget.StandaloneWindows64) ? $"{PlayerName}.exe" : PlayerName);

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = locationPathName,
                target = buildTarget,
                options = EditorUserBuildSettings.development ? options | BuildOptions.Development : options,
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);

            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log("Build succeeded: " + summary.totalSize + " bytes");
            }

            if (summary.result == BuildResult.Failed)
            {
                Debug.LogError("Build failed");
            }
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

            foreach (FileInfo fileInfo in src.GetFiles())
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
