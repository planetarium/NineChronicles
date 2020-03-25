using System.Diagnostics;
using System.IO;

namespace Launcher.Common.RuntimePlatform
{
    public class WindowsPlatform : IRuntimePlatform
    {
        public string GameBinaryDownloadFilename => "win.zip";

        public string GameBinaryFilename => "Nine Chronicles.exe";

        public string LauncherFilename => "Launcher.exe";

        public string OpenCommand => "notepad.exe";

        public string CurrentWorkingDirectory =>
            new FileInfo(Process.GetCurrentProcess().MainModule.FileName).DirectoryName;

        public string BinariesPath => Path.Combine(CurrentWorkingDirectory, "Binaries");

        public string ExecutableLauncherBinaryPath =>
            Path.Combine(BinariesPath, LauncherFilename);

        public string ExecutableGameBinaryPath =>
            Path.Combine(BinariesPath, GameBinaryFilename);
    }
}
