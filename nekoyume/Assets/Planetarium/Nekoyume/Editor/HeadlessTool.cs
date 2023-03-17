using System.Diagnostics;
using System.IO;
using System.Net.Mime;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Directory = System.IO.Directory;
using Path = System.IO.Path;

namespace Planetarium.Nekoyume.Editor
{
    public class HeadlessTool : EditorWindow
    {
        private static string _currentDir = Directory.GetCurrentDirectory();

        private static string _docsRoot =
            Directory.GetParent(Directory.GetParent(_currentDir).ToString()).ToString();

        private static string _headlessPath = "";

        private static string _genesisPath = Application.streamingAssetsPath;

        [MenuItem("Tools/Headless/Setup NineChronicles.Headless repository")]
        private static void SetupHeadlessRepository()
        {
            Debug.LogFormat($"Current project directory is: {_currentDir}");
            Debug.LogFormat($"Docs root directory is: {_docsRoot}");
            _docsRoot =
                EditorUtility.OpenFolderPanel(
                    "Select directory to put headless repository code",
                    _docsRoot, ""
                );
            Debug.LogFormat($"Docs root directory is changed to: {_docsRoot}");
            _headlessPath = Path.Join(_docsRoot, "NineChronicles.Headless");

            Debug.LogFormat("Cloning Repository...");
            var process = Process.Start(
                "git",
                $@"clone https://github.com/planetarium/NineChronicles.Headless {_headlessPath}"
            );
            process.WaitForExit();
            Debug.Log("Headless repository cloned.");

            var startInfo = new ProcessStartInfo("git", "checkout development");
            startInfo.WorkingDirectory = _headlessPath;
            process = Process.Start(startInfo);
            process.WaitForExit();

            startInfo.Arguments = "pull origin development";
            startInfo.WorkingDirectory = _headlessPath;
            process = Process.Start(startInfo);

            process.WaitForExit();
            startInfo.Arguments = "submodule update --init --recursive";
            process = Process.Start(startInfo);
            process.WaitForExit();
            Debug.Log("Submodules updated.");
        }
    }
}
