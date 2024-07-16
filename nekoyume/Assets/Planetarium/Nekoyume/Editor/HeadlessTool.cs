// See also: HeadlessHelper.cs

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Libplanet.Common;
using Nekoyume;
using Nekoyume.Blockchain;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Planetarium.Nekoyume.Editor
{
    public class HeadlessTool : EditorWindow
    {
        private static string _currentDir = Directory.GetCurrentDirectory();

        private static string _lib9cDir =
            Path.Combine(_currentDir, "Assets", "_Scripts", "Lib9c", "lib9c");

        private static string _docsRoot =
            PlayerPrefs.HasKey("docsRoot")
                ? PlayerPrefs.GetString("docsRoot")
                : Directory.GetParent(Directory.GetParent(_currentDir).ToString()).ToString();

        private static string _headlessPath = PlayerPrefs.HasKey("headlessPath")
            ? PlayerPrefs.GetString("headlessPath")
            : "";

        private static string _storeName = "unity-runner";
        private static string _genesisPath = FixPath(Application.streamingAssetsPath);


        private static string FixPath(string path)
        {
            return path.Replace("/",
                (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? Path.DirectorySeparatorChar
                    : Path.AltDirectorySeparatorChar).ToString());
        }

        public static string SetDirectory(string title)
        {
            return FixPath(EditorUtility.OpenFolderPanel(title, _docsRoot, ""));
        }

        [MenuItem("Tools/Headless/Setup NineChronicles.Headless repository")]
        private static void SetupHeadlessRepository()
        {
            Debug.LogFormat($"Current project directory is: {_currentDir}");
            Debug.LogFormat($"Docs root directory is: {_docsRoot}");
            _docsRoot = SetDirectory("Select directory to put headless repository code");
            PlayerPrefs.SetString("docsRoot", _docsRoot);
            Debug.LogFormat($"Docs root directory is changed to: {_docsRoot}");
            _headlessPath = Path.Combine(_docsRoot, "NineChronicles.Headless_Unity-runner");
            PlayerPrefs.SetString("headlessPath", _headlessPath);

            Debug.LogFormat("Cloning Repository...");
            var process = Process.Start(
                "git",
                $@"clone https://github.com/planetarium/NineChronicles.Headless {_headlessPath}"
            );
            process.WaitForExit();
            Debug.Log("Headless repository cloned for unity.");

            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "reset HEAD --hard",
                WorkingDirectory = _headlessPath
            };
            process = Process.Start(startInfo);
            process.WaitForExit();

            startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "checkout development",
                WorkingDirectory = _headlessPath
            };
            process = Process.Start(startInfo);
            process.WaitForExit();

            startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "pull origin development",
                WorkingDirectory = _headlessPath
            };
            process = Process.Start(startInfo);
            process.WaitForExit();
            Debug.Log("Pull latest commits from origin.");

            startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = @"submodule update --init --recursive NineChronicles.RPC.Shared",
                WorkingDirectory = _headlessPath
            };
            process = Process.Start(startInfo);
            process.WaitForExit();
            Debug.Log($"NineChronicles.RPC.Shared submodule updated : {process.ExitCode}");

            if (Directory.Exists(Path.Combine(_headlessPath, "Lib9c")))
            {
                new DirectoryInfo(Path.Combine(_headlessPath, "Lib9c")).Delete(true);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments =
                        $@"/c mklink /d {Path.Combine(_headlessPath, "Lib9c")} {_lib9cDir}",
                    Verb = "runas",
                    CreateNoWindow = true
                };
                process = Process.Start(startInfo);
                process.WaitForExit();
                Debug.Log($"Lib9c symlink created. with {process.ExitCode}");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ||
                RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                startInfo = new ProcessStartInfo
                {
                    FileName = "ln",
                    Arguments = @$"-s {_lib9cDir} {Path.Combine(_headlessPath, "Lib9c")}"
                };
                process = Process.Start(startInfo);
                process.WaitForExit();
                Debug.Log($"Lib9c symlink created. with {process.ExitCode}");
            }
            else
            {
                throw new NotSupportedException(
                    "Current OS is not in Windows|OSX|Linux. Please use supported one");
            }
        }

        [MenuItem("Tools/Headless/Delete and re-create genesis block")]
        private static void ResetGenesisBlock()
        {
            // TODO: Select option to use local genesis or download genesis from URL (like use mainnet genesis)
            Debug.Log($"Delete and create genesis-block to {BlockManager.GenesisBlockPath()}");
            if (Directory.Exists(Path.Combine(_docsRoot, "planetarium", _storeName)))
            {
                Directory.Delete(Path.Combine(_docsRoot, "planetarium", _storeName), true);
            }

            if (!Directory.Exists(Path.Combine(_docsRoot, "planetarium")))
            {
                Directory.CreateDirectory(Path.Combine(_docsRoot, "planetarium"));
            }

            LibplanetEditor.DeleteAllEditorAndMakeGenesisBlock();
        }

        [MenuItem("Tools/Headless/Setup headless config")]
        private static void SetupAppsettingsJson()
        {
            var appsettings = File.ReadAllText(Path.Combine("Assets", "Planetarium",
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

        [MenuItem("Tools/Headless/Run local headless node")]
        public static void Initialize()
        {
            if (string.IsNullOrEmpty(_headlessPath) ||
                !File.Exists(Path.Combine(_headlessPath, "NineChronicles.Headless.Executable",
                    "appsettings.local.json")))
            {
                SetupAppsettingsJson();
            }

            Debug.Log(Path.Combine(_genesisPath, "genesis-block"));
            Debug.Log(Path.Combine(_docsRoot, "planetarium", _storeName));
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments =
                    $"run -c DevEx --project NineChronicles.Headless.Executable -C appsettings.local.json --genesis-block-path {Path.Combine(_genesisPath, "genesis-block")} --store-path {Path.Combine(_docsRoot, "planetarium", _storeName)} --store-type memory"
            };

            var pkHex = Agent.ProposerKey.ToHexWithZeroPaddings();
            startInfo.Arguments +=
                $" --miner-private-key {pkHex} --consensus-private-key {pkHex} --consensus-seed {Agent.ProposerKey.PublicKey},localhost,60000";

            Debug.Log(startInfo.Arguments);
            startInfo.WorkingDirectory = _headlessPath;
            Debug.Log($"WorkingDirectory: {startInfo.WorkingDirectory}");
            var process = Process.Start(startInfo);
            // FIXME: Can I wait here?
            process.WaitForExit();
            Debug.Log($"Headless done: {process.ExitCode}");
        }
    }
}
