using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;

using static Launcher.Common.RuntimePlatform.RuntimePlatform;

namespace Launcher.Common
{
    public static class Configuration
    {
        private const string InstrumentationKey = "953da29a-95f7-4f04-9efe-d48c42a1b53a";

        public static readonly TelemetryClient TelemetryClient =
            new TelemetryClient(new TelemetryConfiguration(InstrumentationKey));

        public static void FlushApplicationInsightLog(object sender, EventArgs e)
        {
            TelemetryClient?.Flush();
            Thread.Sleep(1000);
        }

        public static LauncherSettings LoadSettings()
        {
            InitializeSettingFile();
            return JsonSerializer.Deserialize<LauncherSettings>(
                File.ReadAllText(SettingFilePath),
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
        }

        public static void InitializeSettingFile()
        {
            if (!File.Exists(SettingFilePath))
            {
                File.Copy(SettingFileName, SettingFilePath);
            }
        }

        public static string LoadKeyStorePath(LauncherSettings settings)
        {
            if (string.IsNullOrEmpty(settings.KeyStorePath))
            {
                return DefaultKeyStorePath;
            }
            else
            {
                return settings.KeyStorePath;
            }
        }

        public const string SettingFileName = "launcher.json";

        public static string DefaultStorePath => Path.Combine(PlanetariumLocalApplicationPath, "9c");

        // It assumes there is game binary file in same directory.
        public static string GameBinaryPath => Path.Combine(CurrentPlatform.CurrentWorkingDirectory, CurrentPlatform.GameBinaryFilename);

        public static string DefaultKeyStorePath => Path.Combine(PlanetariumApplicationPath, "keystore");

        public static string SettingFilePath => Path.Combine(CurrentPlatform.CurrentWorkingDirectory, SettingFileName);

        private static string PlanetariumLocalApplicationPath => Path.Combine(LocalApplicationDataPath, "planetarium");

        private static string PlanetariumApplicationPath => Path.Combine(ApplicationDataPath, "planetarium");

        private static string LocalApplicationDataPath => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        private static string ApplicationDataPath => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    }
}
