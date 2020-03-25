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

            if (!Configuration.LocalCurrentVersion.Equals(currentVersionDescriptor))
            {
                Console.Error.WriteLine($"New update released! {version}");

                if (Directory.Exists(CurrentPlatform.BinariesPath))
                {
                    Directory.Delete(CurrentPlatform.BinariesPath, true);
                }

                try
                {
                    await DownloadBinariesAsync(s3Storage, settings.DeployBranch, version,
                        cts.Token);
                    Configuration.LocalCurrentVersion = currentVersionDescriptor;
                }
                catch (OperationCanceledException)
                {
                    Console.Error.WriteLine("task was cancelled.");
                }
            }

            Process.Start(CurrentPlatform.ExecutableLauncherBinaryPath);
        }

        private static async Task DownloadBinariesAsync(S3Storage storage, string deployBranch, string version, CancellationToken cancellationToken)
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
            using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
            {
                await responseMessage.Content.CopyToAsync(fileStream);
                Console.Error.WriteLine($"Finished download from '{gameBinaryDownloadUri}'!");
            }

            // Extract binary.
            // TODO: implement a function to extract with file extension.
            Console.Error.WriteLine("Start to extract game binary.");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                await using var tempFile = File.OpenRead(tempFilePath);
                using var gz = new GZipInputStream(tempFile);
                using var tar = TarArchive.CreateInputTarArchive(gz);
                tar.ExtractContents(CurrentPlatform.BinariesPath);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                ZipFile.ExtractToDirectory(tempFilePath, CurrentPlatform.BinariesPath);
            }
            Console.Error.WriteLine("Finished to extract game binary.");
        }
    }
}
