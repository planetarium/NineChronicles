namespace Launcher
{
    public interface IRuntimePlatform
    {
        string GameBinaryDownloadFilename { get; }
        string OpenCommand { get; }
        string ExecutableGameBinaryPath(string gameBinaryPath);
    }
}
