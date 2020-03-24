namespace Launcher
{
    public interface IRuntimePlatform
    {
        string GameBinaryDownloadFilename { get; }
        string GameBinaryFilename { get; }
        string LauncherFilename { get; }
        string OpenCommand { get; }
        string CurrentWorkingDirectory { get; }
        string ExecutableLauncherBinaryPath { get; }
        string ExecutableGameBinaryPath(string gameBinaryPath);
    }
}
