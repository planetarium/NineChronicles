using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Nekoyume.Helper
{
    public static class HeadlessHelper
    {
        private static string _headlessPath = PlayerPrefs.HasKey("headlessPath")
            ? PlayerPrefs.GetString("headlessPath")
            : "";

        private static string _docsRoot =
            PlayerPrefs.HasKey("docsRoot") ? PlayerPrefs.GetString("docsRoot") : "";

        private static string _genesisPath = FixPath(Application.streamingAssetsPath);
        private static string _storeName = "unity-runner";
        private static Process process;

        public static bool CheckHeadlessSettings()
        {
            if (string.IsNullOrEmpty(_docsRoot))
            {
                EditorUtility.DisplayDialog("Headless path set set",
                    "Please set headless path first on Menubar > Tools > Headless", "OK");
            }

            if (string.IsNullOrEmpty(_headlessPath) ||
                !File.Exists(Path.Combine(_headlessPath, "NineChronicles.Headless.Executable",
                    "appsettings.local.json")))
            {
                // SetupAppsettingsJson();
                EditorUtility.DisplayDialog("Headless config not set",
                    "Please set configs first on Menubar > Tools > Headless", "OK");
                return false;
            }

            return true;
        }

        public static void RunLocalHeadless()
        {
            try
            {
                Debug.Log(Path.Combine(_genesisPath, "genesis-block"));
                Debug.Log(Path.Combine(_docsRoot, "planetarium", _storeName));
                var startInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments =
                        @$"run -c DevEx --project NineChronicles.Headless.Executable -C appsettings.local.json --genesis-block-path ""{Path.Combine(_genesisPath, "genesis-block")}"" --store-path ""{Path.Combine(_docsRoot, "planetarium", _storeName)}"" --store-type memory",
                };
                Debug.Log(startInfo.Arguments);
                startInfo.WorkingDirectory = _headlessPath;
                Debug.Log($"WorkingDirectory: {startInfo.WorkingDirectory}");
                process = Process.Start(startInfo);
                // FIXME: Can I wait here?
                process.WaitForExit();
                Debug.Log($"Headless done: {process.ExitCode}");
            }
            catch (ThreadInterruptedException)
            {
                Debug.Log("Interrupt Detected. Exiting...");
                process.CloseMainWindow();
            }
        }

        private static string FixPath(string path) => path.Replace("/",
            (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? Path.DirectorySeparatorChar
                : Path.AltDirectorySeparatorChar).ToString());
    }
}
