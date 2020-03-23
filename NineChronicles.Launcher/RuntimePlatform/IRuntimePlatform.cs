namespace Launcher
{
    public interface IRuntimePlatform
    {
        string GameBinaryDownloadFilename { get; }
        string OpenCommand { get; }
        string CurrentWorkingDirectory { get; }
        string ExecutableGameBinaryPath(string gameBinaryPath);
    }
}
