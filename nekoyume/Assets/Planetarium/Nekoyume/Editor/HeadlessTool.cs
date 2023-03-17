using System.Diagnostics;
using System.IO;
using Nekoyume.BlockChain;
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

        private static string SetDirectory(string title)
        {
            return EditorUtility.OpenFolderPanel(title, _docsRoot, "");
        }

        [MenuItem("Tools/Headless/Setup NineChronicles.Headless repository")]
        private static void SetupHeadlessRepository()
        {
            Debug.LogFormat($"Current project directory is: {_currentDir}");
            Debug.LogFormat($"Docs root directory is: {_docsRoot}");
            _docsRoot = SetDirectory("Select directory to put headless repository code");
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

        [MenuItem("Tools/Headless/Delete and re-create genesis block")]
        private static void ResetGenesisBlock()
        {
            // TODO: Select option to use local genesis or download genesis from URL (like use mainnet genesis)
            Debug.Log($"Delete and create genesis-block to {BlockManager.GenesisBlockPath()}");
            LibplanetEditor.DeleteAllEditorAndMakeGenesisBlock();
        }

        [MenuItem("Tools/Headless/Setup headless config")]
        private static void SetupAppsettingsJson()
        {
            var appsettings = System.IO.File.ReadAllText(Path.Combine("Assets", "Planetarium",
                "Nekoyume", "Editor", "appsettings.example.json"));
            Debug.Log($"{appsettings.Length} text read from appsettings.");
            if (string.IsNullOrEmpty(_headlessPath))
            {
                _headlessPath = SetDirectory("Please select NineChronicles.Headless directory");
            }

            Debug.Log(_headlessPath);
            File.WriteAllText(
                Path.Combine(
                    _headlessPath,
                    "NineChronicles.Headless.Executable",
                    "appsettings.local.json"
                ), appsettings);
            Debug.Log("appsettings.local.json set.");
        }
    }
}
