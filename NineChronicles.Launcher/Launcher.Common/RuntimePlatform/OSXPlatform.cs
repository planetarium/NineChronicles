using System;
using System.Diagnostics;
using System.IO;
using static Launcher.Common.Utils;

namespace Launcher.Common.RuntimePlatform
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
            // 2. open AppleScript Application, then it will read the file and display notification.
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
