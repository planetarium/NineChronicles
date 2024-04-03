#if UNITY_EDITOR

using System;
using System.Diagnostics;
using System.Threading;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Nekoyume.Helper
{
    public class MarketHelper
    {
        private static string _marketPath = PlayerPrefs.GetString("marketPath");
        private static Process _process;

        public static bool CheckPath()
        {
            if (string.IsNullOrEmpty(_marketPath))
            {
                EditorUtility.DisplayDialog("MarketService repository not set",
                    "Please set configs first on Menubar > Tools > Market", "OK");

                return false;
            }

            return true;
        }

        public static void RunLocalMarketService(string marketDbConnectionString)
        {
            if (string.IsNullOrEmpty(_marketPath))
            {
                throw new ArgumentException("SetupMarketServiceRepository first.");
            }
            NcDebug.LogFormat($"MarketService project directory is: {_marketPath}");
            var options = CommandLineOptions.Load(Platform.GetStreamingAssetsPath("clo.local.json"));
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "run --project MarketService --no-launch-profile",
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                WorkingDirectory = _marketPath,
                EnvironmentVariables =
                {
                    ["WorkerConfig__SyncProduct"] = "true",
                    ["WorkerConfig__SyncShop"] = "false",
                    ["RpcConfig__Host"] = options.RpcServerHost,
                    ["RpcConfig__Port"] = options.RpcServerPort.ToString(),
                    ["ConnectionStrings__MARKET"] = marketDbConnectionString,
                },
            };
            NcDebug.Log(startInfo.Arguments);
            NcDebug.Log($"WorkingDirectory: {startInfo.WorkingDirectory}");
            try
            {
                _process = Process.Start(startInfo);
                // FIXME: Can I wait here?
                _process.WaitForExit();
                NcDebug.LogError($"{_process.StandardError.ReadToEnd()}");
                NcDebug.Log($"{_process.StandardOutput.ReadToEnd()}");
                NcDebug.Log($"MarketService done: {_process.ExitCode}");
            }
            catch (ThreadInterruptedException)
            {
                _process.CloseMainWindow();
                _process.Close();
            }
        }
    }
}

#endif
