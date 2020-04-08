namespace Launcher.Common.RuntimePlatform
{
    public interface IRuntimePlatform
    {
        string GameBinaryDownloadFilename { get; }
        string GameBinaryFilename { get; }
        string LauncherFilename { get; }
        string OpenCommand { get; }
        string CurrentWorkingDirectory { get; }
        string QtRuntimeDirectory { get; }
        string ExecutableLauncherBinaryPath { get; }
        string ExecutableGameBinaryPath { get; }
        string LogFilePath { get; }
    }
}
