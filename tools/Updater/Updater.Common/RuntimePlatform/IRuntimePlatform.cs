using System.IO;

namespace Updater.Common.RuntimePlatform
{
    public interface IRuntimePlatform
    {
        string GameBinaryDownloadFilename { get; }
        string GameBinaryFilename { get; }
        string LauncherFilename { get; }
        string OpenCommand { get; }
        string CurrentWorkingDirectory { get; }
        string RunCommandFilePath => Path.Combine(CurrentWorkingDirectory, ".rungame");
        string QtRuntimeDirectory { get; }
        string ExecutableLauncherBinaryPath { get; }
        string ExecutableGameBinaryPath { get; }
        string ExecutableUpdaterBinaryPath { get; }
        string LogFilePath { get; }
        string UpdaterLogFilePath { get; }
        void DisplayNotification(string title, string message);
    }
}
