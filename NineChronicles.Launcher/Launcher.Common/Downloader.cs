using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace Launcher.Common
{
    public static class Downloader
    {
        public const string SnapshotUrl =
            "https://download.nine-chronicles.com/latest/7bc7f433c8fca903f461de848bfd57d30202f1b87e4e1fd97512030a9b7a7bf0-snapshot.zip";

        public static async Task<string> DownloadFileAsync(
            string downloadUrl,
            IProgress<(long Downloaded, long Total)> progress = null,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            var tempFilePath = Path.GetTempFileName();
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }

            Log.Information(
                "Start download from {DownloadUri} to {TempFilePath}.",
                downloadUrl,
                tempFilePath
            );

            using var httpClient = new HttpClient();
            httpClient.Timeout = Timeout.InfiniteTimeSpan;
            using var dest = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write);
            using HttpResponseMessage response = await httpClient.GetAsync(
                downloadUrl,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken
            );
            using var src = await response.Content.ReadAsStreamAsync();

            var buffer = new byte[8192];
            long contentLength = response.Content.Headers.ContentLength ?? 0;
            long totalRead = 0;
            int bytesRead;

            while ((bytesRead = await src.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) != 0)
            {
                await dest.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
                totalRead += bytesRead;
                progress?.Report((totalRead, contentLength));
            }

            Log.Information("Finished download from {DownloadUri}!", downloadUrl);
            return tempFilePath;
        }

        public static async Task DownloadBlockchainSnapshot(
            IProgress<(long Downloaded, long Total)> progress = null,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            Log.Information("Download the recent blockchain snapshot...");
            string tempPath = await DownloadFileAsync(SnapshotUrl, progress, cancellationToken);
            string storePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "planetarium",
                "9c"
            );
            if (Directory.Exists(storePath))
            {
                Directory.Delete(storePath, recursive: true);
            }

            ZipFile.ExtractToDirectory(tempPath, storePath);
        }
    }
}
