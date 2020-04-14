using System.Diagnostics;
using System.IO;

namespace Launcher.Common.RuntimePlatform
{
    public class WindowsPlatform : IRuntimePlatform
    {
        public string GameBinaryDownloadFilename => "win.zip";

        public string GameBinaryFilename => "9c.exe";

        public string LauncherFilename => "Nine Chronicles.exe";

        public string OpenCommand => "notepad.exe";

        public string CurrentWorkingDirectory =>
            new FileInfo(Process.GetCurrentProcess().MainModule.FileName).DirectoryName;

        public string QtRuntimeDirectory =>
            Path.Combine(CurrentWorkingDirectory, "qt-runtime");

        public string ExecutableLauncherBinaryPath =>
            Path.Combine(CurrentWorkingDirectory, LauncherFilename);

        public string ExecutableGameBinaryPath =>
            Path.Combine(CurrentWorkingDirectory, GameBinaryFilename);

        public string LogFilePath =>
            Path.Combine(CurrentWorkingDirectory, "Logs", "launcher.log");
    }
}
