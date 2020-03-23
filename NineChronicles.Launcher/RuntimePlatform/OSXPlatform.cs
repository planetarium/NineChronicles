using System.Diagnostics;
using System.IO;

namespace Launcher.RuntimePlatform
{
    public sealed class OSXPlatform : IRuntimePlatform
    {
        public string GameBinaryDownloadFilename => "macOS.tar.gz";

        public string GameBinaryFilename => "Nine Chronicles.app";

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

        public string ExecutableGameBinaryPath(string gameBinaryPath) =>
            Path.Combine(gameBinaryPath, GameBinaryFilename, "Contents", "MacOS", "Nine Chronicles");
    }
}
