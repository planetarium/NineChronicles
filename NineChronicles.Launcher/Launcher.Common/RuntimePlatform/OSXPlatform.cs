using System;
using System.Diagnostics;
using System.IO;

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
                var executablePath = Process.GetCurrentProcess().MainModule.FileName;
                var parentDirectory = new FileInfo(executablePath).Directory;
                if (executablePath.EndsWith(bundlePath))
                {
                    parentDirectory = parentDirectory.Parent.Parent.Parent;
                }
                return parentDirectory.FullName;
            }
        }

        public string BinariesPath => Path.Combine(CurrentWorkingDirectory, "Binaries");

        public string ExecutableLauncherBinaryPath =>
            Path.Combine(
                CurrentWorkingDirectory,
                LauncherFilename,
                "Contents",
                "MacOS",
                Path.GetFileNameWithoutExtension(LauncherFilename)
            );

        public string ExecutableGameBinaryPath =>
            Path.Combine(
                CurrentWorkingDirectory,
                GameBinaryFilename,
                "Contents",
                "MacOS",
                Path.GetFileNameWithoutExtension(GameBinaryFilename)
            );

        public string LogFilePath
            => Path.Combine(
                Environment.GetEnvironmentVariable("HOME"),
                "Library",
                "Logs",
                "Planetarium",
                "launcher.log");
    }
}
