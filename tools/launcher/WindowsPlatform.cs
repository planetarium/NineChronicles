using System.IO;

namespace Launcher
{
    public class WindowsPlatform : IRuntimePlatform
    {
        public string GameBinaryDownloadFilename => "NineChronicles-alpha-2-win.zip";

        public string OpenCommand => "notepad.exe";

        public string ExecutableGameBinaryPath(string gameBinaryPath) =>
            Path.Combine(gameBinaryPath, "Nine Chronicles.exe");
    }
}
