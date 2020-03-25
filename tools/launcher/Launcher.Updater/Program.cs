using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using Launcher.Common;
using Launcher.Common.Storage;

using static Launcher.Common.RuntimePlatform.RuntimePlatform;

namespace Launcher.Updater
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var settings = Configuration.LoadSettings();
            var s3Storage = new S3Storage();
            var cts = new CancellationTokenSource();
            var currentVersionDescriptor = await VersionHelper.CurrentVersionAsync(s3Storage, settings.DeployBranch, cts.Token);

            var version = currentVersionDescriptor.Version;
            var tempPath = Path.Combine(Path.GetTempPath(), "temp-9c-download" + currentVersionDescriptor);

            Console.Error.WriteLine($"New update released! {version}");
            Console.Error.WriteLine($"It will be downloaded at temporary path: {tempPath}");

            var gameBinaryPath = Configuration.LoadGameBinaryPath(settings);
            try
            {
                await DownloadGameBinaryAsync(s3Storage, tempPath, settings.DeployBranch, version,
                    cts.Token);

                // FIXME: it kills game process in force, if it was running. it should be
                //        killed with some message.
                SwapDirectory(
                    gameBinaryPath,
                    Path.Combine(tempPath));
                Configuration.LocalCurrentVersion = currentVersionDescriptor;
            }
            catch (OperationCanceledException)
            {
                Console.Error.WriteLine("task was cancelled.");
            }

            Process.Start(CurrentPlatform.ExecutableLauncherBinaryPath);
        }

        private static async Task DownloadGameBinaryAsync(S3Storage storage, string gameBinaryPath, string deployBranch, string version, CancellationToken cancellationToken)
        {
            var tempFilePath = Path.GetTempFileName();
            using var httpClient = new HttpClient();
            httpClient.Timeout = Timeout.InfiniteTimeSpan;

            var gameBinaryDownloadUri = storage.GameBinaryDownloadUri(deployBranch, version);
            Console.Error.WriteLine($"Start download game binary from '{gameBinaryDownloadUri}' to {tempFilePath}.");
            var responseMessage = await httpClient.GetAsync(gameBinaryDownloadUri, cancellationToken);
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }

            using var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write);
            await responseMessage.Content.CopyToAsync(fileStream);
            Console.Error.WriteLine($"Finished download from '{gameBinaryDownloadUri}'!");

            // Extract binary.
            // TODO: implement a function to extract with file extension.
            Console.Error.WriteLine("Start to extract game binary.");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                await using var tempFile = File.OpenRead(tempFilePath);
                using var gz = new GZipInputStream(tempFile);
                using var tar = TarArchive.CreateInputTarArchive(gz);
                tar.ExtractContents(gameBinaryPath);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                ZipFile.ExtractToDirectory(tempFilePath, gameBinaryPath);
            }
            Console.Error.WriteLine("Finished to extract game binary.");
        }

        private static void SwapDirectory(string gameBinaryPath, string newGameBinaryPath)
        {
            if (Directory.Exists(gameBinaryPath))
            {
                Directory.Delete(gameBinaryPath, recursive: true);
            }

            Directory.Move(newGameBinaryPath, gameBinaryPath);
        }
    }
}
