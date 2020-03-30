using System;
using System.IO;
using System.Text.Json;
using Serilog;

using static Launcher.Common.RuntimePlatform.RuntimePlatform;

namespace Launcher.Common
{
    public static class Configuration
    {
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

        public static VersionDescriptor? LocalCurrentVersion
        {
            get
            {
                try
                {
                    var raw = File.ReadAllText(LocalCurrentVersionPath);
                    return JsonSerializer.Deserialize<VersionDescriptor>(
                        raw,
                        new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        });
                }
                catch (Exception e)
                {
                    Log.Error(e, $"Unexpected exception occurred: {e.Message}");
                    return null;
                }
            }
            set => File.WriteAllText(LocalCurrentVersionPath, JsonSerializer.Serialize((VersionDescriptor) value,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                }));
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

        private const string SettingFileName = "launcher.json";

        public static string DefaultStorePath => Path.Combine(PlanetariumLocalApplicationPath, "9c");

        // It assumes there is game binary file in same directory.
        public static string GameBinaryPath => Path.Combine(CurrentPlatform.CurrentWorkingDirectory, CurrentPlatform.GameBinaryFilename);

        public static string DefaultKeyStorePath => Path.Combine(PlanetariumApplicationPath, "keystore");

        public static string SettingFilePath => Path.Combine(CurrentPlatform.CurrentWorkingDirectory, SettingFileName);

        private static string PlanetariumLocalApplicationPath => Path.Combine(LocalApplicationDataPath, "planetarium");

        private static string PlanetariumApplicationPath => Path.Combine(ApplicationDataPath, "planetarium");

        private static string LocalApplicationDataPath => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        private static string ApplicationDataPath => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        private static string LocalCurrentVersionPath => Path.Combine(CurrentPlatform.CurrentWorkingDirectory, "9c-current-version.json");
    }
}
