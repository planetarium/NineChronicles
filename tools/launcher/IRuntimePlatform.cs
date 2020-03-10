using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace Launcher
{
    public interface IRuntimePlatform
    {
        string GameBinaryDownloadFilename { get; }
        string OpenCommand { get; }
        string ExecutableGameBinaryPath(string gameBinaryPath);
    }
}
