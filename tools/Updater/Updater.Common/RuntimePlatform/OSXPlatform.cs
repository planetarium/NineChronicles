using System;
using System.Diagnostics;
using System.IO;
using static Updater.Common.Utils;

namespace Updater.Common.RuntimePlatform
{
    public sealed class OSXPlatform : IRuntimePlatform
    {
        public string GameBinaryDownloadFilename => "macOS.tar.gz";

        public string GameBinaryFilename => "9c.app";

        public string LauncherFilename => "Nine Chronicles.app";

        public string OpenCommand => "open";

        public string CurrentWorkingDirectory
        {
            get
            {
                string bundlePath =
                    $"{LauncherFilename}/Contents/MacOS/{Path.GetFileNameWithoutExtension(LauncherFilename)}";
                string executablePath = ExecutableLauncherBinaryPath;
                var parentDirectory = new FileInfo(executablePath).Directory;
                if (executablePath.EndsWith(bundlePath))
                {
                    parentDirectory = parentDirectory.Parent.Parent.Parent;
                }
                return parentDirectory.FullName;
            }
        }

        public string QtRuntimeDirectory =>
            Path.Combine(Path.GetDirectoryName(ExecutableLauncherBinaryPath), "qt-runtime");

        public string ExecutableLauncherBinaryPath =>
            Process.GetCurrentProcess().MainModule.FileName;

        public string ExecutableGameBinaryPath =>
            Path.Combine(
                CurrentWorkingDirectory,
                GameBinaryFilename,
                "Contents",
                "MacOS",
                Path.GetFileNameWithoutExtension(GameBinaryFilename)
            );

        public string ExecutableUpdaterBinaryPath =>
            Path.Combine(CurrentWorkingDirectory, "Nine Chronicles Updater");

        public string LogFilePath
            => Path.Combine(
                Environment.GetEnvironmentVariable("HOME"),
                "Library",
                "Logs",
                "Planetarium",
                "launcher.log");

        public string UpdaterLogFilePath
            => Path.Combine(
                Environment.GetEnvironmentVariable("HOME"),
                "Library",
                "Logs",
                "Planetarium",
                "updater.log");


        public void DisplayNotification(string title, string message)
        {
            // NineChronicles.Launcher/Notifier/NineChronicles Notifier 에 위치한 xcode swift 프로젝트에서
            // 빌드한 번들 앱을 사용합니다.  msbuild 빌드 태스크에 포함되어 있지 않아서 임시로 직접 빌드하여 론처 resources
            // 하위에 넣어 빌드할 때 포함하게 하고 있습니다.
            string executableNotifierBinaryPath = Path.Combine(
                Path.GetDirectoryName(ExecutableLauncherBinaryPath),
                "NineChronicles Notifier.app",
                "Contents",
                "MacOS",
                "NineChronicles Notifier"
            );

            string arguments =
                $"{EscapeShellArgument(title)} {EscapeShellArgument(message)} {EscapeShellArgument(ExecutableLauncherBinaryPath)}";
            Process.Start(
                executableNotifierBinaryPath,
                arguments
            );
        }
    }
}
