using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Nekoyume;
using Nekoyume.Helper;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace NekoyumeEditor
{
    public class MarketTool : EditorWindow
    {
        private static readonly string CurrentDir = Directory.GetCurrentDirectory();

        private static string _marketPath = PlayerPrefs.HasKey("marketPath")
            ? PlayerPrefs.GetString("marketPath")
            : "";

        private static Thread _thread;
        private static Process _process;

        [MenuItem("Tools/Market/Setup MarketService repository")]
        private static void SetupMarketServiceRepository()
        {
            Debug.LogFormat($"Current project directory is: {CurrentDir}");
            _marketPath = HeadlessTool.SetDirectory("Select directory from local MarketService repository code");
            PlayerPrefs.SetString("marketPath", _marketPath);
            Debug.LogFormat($"MarketService project directory is: {_marketPath}");
        }
    }
}
