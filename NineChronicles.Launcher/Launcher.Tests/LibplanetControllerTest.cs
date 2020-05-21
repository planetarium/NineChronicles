using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Text.Json;
using Launcher.Common;
using static Launcher.Common.Configuration.Path;
using Xunit;
using Xunit.Abstractions;

namespace Launcher.Tests
{
    public class LibplanetControllerTest
    {
        private readonly ITestOutputHelper Logger;

        public LibplanetControllerTest(ITestOutputHelper logger)
        {
            Logger = logger;

            LauncherSettings = new LauncherSettings
            {
                KeyStorePath = Path.Combine(Path.GetTempPath(), "keystore"),
            };
            // `Path.GetTempPath` doesn't return randomly well.
            Directory.Delete(LauncherSettings.KeyStorePath, recursive: true);
        }

        private readonly LauncherSettings LauncherSettings;

        private string LauncherSettingsData => JsonSerializer.Serialize(LauncherSettings, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });

        [Fact]
        public void CreatePrivateKey()
        {
            MockFileSystem mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                [SettingFilePath] = new MockFileData(LauncherSettingsData),
            });

            Configuration configuration = new Configuration(mockFileSystem);
            Logger.WriteLine(configuration.LoadSettings().KeyStorePath);
            Logger.WriteLine(SettingFilePath);
            Logger.WriteLine(LauncherSettingsData);

            var controller = new LibplanetController(configuration, mockFileSystem);

            Assert.True(controller.KeyStoreEmpty);

            var random = new Random();
            var passphrase = string.Join(
                string.Empty,
                Enumerable.Range(32, 127).OrderBy(_ => random.Next()).Select(Convert.ToChar));

            controller.CreatePrivateKey(passphrase);

            Assert.False(controller.KeyStoreEmpty);
            Assert.Single(controller.KeyStore.List());
        }
    }
}
