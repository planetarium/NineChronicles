using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace Editor
{
    public class Builder {

        public static string PlayerName = "nekoyume";

        [MenuItem("Build/Windows + Mac OSX + Linux")]
        public static void BuildAll()
        {
            BuildMacOS();
            BuildWindows();
            BuildLinux();
        }

        [MenuItem("Build/MacOS")]
        public static void BuildMacOS()
        {
            Debug.Log("Build MacOS");
            Build(BuildTarget.StandaloneOSX);
        }

        [MenuItem("Build/Windows")]
        public static void BuildWindows()
        {
            Debug.Log("Build Windows");
            Build(BuildTarget.StandaloneWindows64);
        }

        [MenuItem("Build/Linux")]
        public static void BuildLinux()
        {
            Debug.Log("Build Linux");
            Build(BuildTarget.StandaloneLinux64);
        }

        public static void Build(BuildTarget buildTarget)
        {
            string[] scenes = { "Assets/_Scenes/Game.unity" };
            const string basePath = "Build";
            string locationPathName = Path.Combine(
                basePath,
                buildTarget.ToString(),
                PlayerName);

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = locationPathName,
                target = buildTarget,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log("Build succeeded: " + summary.totalSize + " bytes");
            }

            if (summary.result == BuildResult.Failed)
            {
                Debug.Log("Build failed");
            }
        }
    }

}
