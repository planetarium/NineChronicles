using System.IO;

namespace Launcher.RuntimePlatform
{
    public sealed class OSXPlatform : IRuntimePlatform
    {
        public string GameBinaryDownloadFilename => "NineChronicles-alpha-2-macOS.tar.gz";

        public string OpenCommand => "open";

        public string ExecutableGameBinaryPath(string gameBinaryPath) =>
            Path.Combine(gameBinaryPath, "Nine Chronicles.app", "Contents", "MacOS", "Nine Chronicles");
    }
}
