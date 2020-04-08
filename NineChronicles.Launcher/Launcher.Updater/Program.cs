using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Launcher.Common;
using Launcher.Common.Storage;

using static Launcher.Common.RuntimePlatform.RuntimePlatform;

namespace Launcher.Updater
{
    class Program
    {
        const string MacOSLatestBinaryUrl = "https://download.nine-chronicles.com/latest/macOS.tar.gz";
        const string WindowsLatestBinaryUrl = "https://download.nine-chronicles.com/latest/Windows.zip";

        static async Task Main(string[] args)
        {
            var cts = new CancellationTokenSource();
            string binaryUrl;

            if (args.Length > 0)
            {
                // 업데이트 모드
                binaryUrl = args[0];
            }
            else
            {
                // 인스톨러 모드
                binaryUrl = RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                    ? MacOSLatestBinaryUrl
                    : WindowsLatestBinaryUrl;
            }

            if (binaryUrl is string u)
            {
                try
                {
                    await DownloadBinariesAsync(u, cts.Token);
                }
                catch (OperationCanceledException)
                {
                    Console.Error.WriteLine("task was cancelled.");
                }
            }

            Process.Start(CurrentPlatform.ExecutableLauncherBinaryPath);
        }

        private static async Task DownloadBinariesAsync(
            string gameBinaryDownloadUri,
            CancellationToken cancellationToken
        )
        {
            var tempFilePath = Path.GetTempFileName();

            Console.Error.WriteLine($"Start download game binary from '{gameBinaryDownloadUri}' to {tempFilePath}.");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start(
                    "curl",
                    $"-o {EscapeShellArgument(tempFilePath)} {EscapeShellArgument(gameBinaryDownloadUri)}"
                ).WaitForExit();
            }
            else
            {
                using var httpClient = new HttpClient();
                httpClient.Timeout = Timeout.InfiniteTimeSpan;
                var responseMessage = await httpClient.GetAsync(gameBinaryDownloadUri, cancellationToken);
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
                using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
                {
                    await responseMessage.Content.CopyToAsync(fileStream);
                }
            }
            Console.Error.WriteLine($"Finished download from '{gameBinaryDownloadUri}'!");

            // TODO: implement a function to extract with file extension.
            Console.Error.WriteLine("Start to extract game binary.");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var process = Process.Start(
                    "tar",
                    $"-zxvf {EscapeShellArgument(tempFilePath)} " +
                    $"-C {EscapeShellArgument(CurrentPlatform.CurrentWorkingDirectory)}"
                );
                process.WaitForExit();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                ZipFile.ExtractToDirectory(tempFilePath, CurrentPlatform.CurrentWorkingDirectory);
            }
            else
            {
                throw new Exception("Unsupported platform.");
            }

            Console.Error.WriteLine("Finished to extract game binary.");
        }

        private static string EscapeShellArgument(string value) =>
            "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }
}
