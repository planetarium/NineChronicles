using System.Diagnostics;
using System.IO;

namespace Launcher.RuntimePlatform
{
    public class WindowsPlatform : IRuntimePlatform
    {
        public string GameBinaryDownloadFilename => "win.zip";

        public string GameBinaryFilename => "Nine Chronicles.exe";

        public string OpenCommand => "notepad.exe";

        public string CurrentWorkingDirectory =>
            new FileInfo(Process.GetCurrentProcess().MainModule.FileName).DirectoryName;

        public string ExecutableGameBinaryPath(string gameBinaryPath) =>
            Path.Combine(gameBinaryPath, GameBinaryFilename);
    }
}
