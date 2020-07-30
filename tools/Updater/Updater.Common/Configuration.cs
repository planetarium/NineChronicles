using System;
using System.IO.Abstractions;
using System.Text.Json;
using System.Threading;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;

using static Updater.Common.RuntimePlatform.RuntimePlatform;

namespace Updater.Common
{
    public class Configuration
    {
        private readonly IFileSystem FileSystem;

        private IFile File => FileSystem.File;

        public Configuration() : this(
            fileSystem: new FileSystem()
        )
        {
        }

        public Configuration(IFileSystem fileSystem)
        {
            FileSystem = fileSystem;
        }

        public LauncherSettings LoadSettings()
        {
            InitializeSettingFile();
            return JsonSerializer.Deserialize<LauncherSettings>(
                File.ReadAllText(Path.SettingFilePath),
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
        }

        public void InitializeSettingFile()
        {
            if (!File.Exists(Path.SettingFilePath))
            {
                File.Copy(Path.SettingFileName, Path.SettingFilePath);
            }
        }

        public string LoadKeyStorePath(LauncherSettings settings)
        {
            if (string.IsNullOrEmpty(settings.KeyStorePath))
            {
                return Path.DefaultKeyStorePath;
            }
            else
            {
                return settings.KeyStorePath;
            }
        }

        public static class Log
        {
            private const string InstrumentationKey = "953da29a-95f7-4f04-9efe-d48c42a1b53a";

            public static readonly TelemetryClient TelemetryClient =
                new TelemetryClient(new TelemetryConfiguration(InstrumentationKey));

            public static void FlushApplicationInsightLog(object sender, EventArgs e)
            {
                TelemetryClient?.Flush();
                Thread.Sleep(1000);
            }
        }

        public static class Path
        {
            public const string SettingFileName = "launcher.json";

            public static string DefaultStorePath => System.IO.Path.Combine(PlanetariumLocalApplicationPath, "9c");

            // It assumes there is game binary file in same directory.
            public static string GameBinaryPath => System.IO.Path.Combine(CurrentPlatform.CurrentWorkingDirectory, CurrentPlatform.GameBinaryFilename);

            public static string DefaultKeyStorePath => System.IO.Path.Combine(PlanetariumApplicationPath, "keystore");

            public static string SettingFilePath => System.IO.Path.Combine(CurrentPlatform.CurrentWorkingDirectory, SettingFileName);

            private static string PlanetariumLocalApplicationPath => System.IO.Path.Combine(LocalApplicationDataPath, "planetarium");

            private static string PlanetariumApplicationPath => System.IO.Path.Combine(ApplicationDataPath, "planetarium");

            private static string LocalApplicationDataPath => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            private static string ApplicationDataPath => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        }
    }
}
