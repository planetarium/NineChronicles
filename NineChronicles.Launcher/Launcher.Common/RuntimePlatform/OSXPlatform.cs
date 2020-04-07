using System;
using System.Diagnostics;
using System.IO;

namespace Launcher.Common.RuntimePlatform
{
    public sealed class OSXPlatform : IRuntimePlatform
    {
        public string GameBinaryDownloadFilename => "macOS.tar.gz";

        public string GameBinaryFilename => "Nine Chronicles.app";

        public string LauncherFilename => "Launcher.app";

        public string OpenCommand => "open";

        public string CurrentWorkingDirectory
        {
            get
            {
                const string BundlePath = "Launcher.app/Contents/MacOS/Launcher";
                var executablePath = Process.GetCurrentProcess().MainModule.FileName;
                var parentDirectory = new FileInfo(executablePath).Directory;
                if (executablePath.EndsWith(BundlePath))
                {
                    parentDirectory = parentDirectory.Parent.Parent.Parent;
                }
                return parentDirectory.FullName;
            }
        }

        public string BinariesPath => Path.Combine(CurrentWorkingDirectory, "Binaries");

        public string ExecutableLauncherBinaryPath =>
            Path.Combine(CurrentWorkingDirectory, LauncherFilename, "Contents", "MacOS", "Launcher");

        public string ExecutableGameBinaryPath
            => Path.Combine(CurrentWorkingDirectory, GameBinaryFilename, "Contents", "MacOS", "Nine Chronicles");

        public string LogFilePath
            => Path.Combine(
                Environment.GetEnvironmentVariable("HOME"),
                "Library",
                "Logs",
                "Planetarium",
                "launcher.log");
    }
}
