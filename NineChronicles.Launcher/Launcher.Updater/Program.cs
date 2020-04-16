using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Launcher.Common;
using Serilog;
using Serilog.Events;
using ShellProgressBar;
using static Launcher.Common.RuntimePlatform.RuntimePlatform;
using static Launcher.Common.Utils;

namespace Launcher.Updater
{
    class Program
    {
        const string MacOSLatestBinaryUrl = "https://download.nine-chronicles.com/latest/macOS.tar.gz";
        const string WindowsLatestBinaryUrl = "https://download.nine-chronicles.com/latest/Windows.zip";

        // NOTE: 9c-beta의 제네시스 해시을 하드코딩 해놓았습니다.
        private const string SnapshotUrl =
            "https://download.nine-chronicles.com/latest/2be5da279272a3cc2ecbe329405a613c40316173773d6d2d516155d2aa67d9bb-snapshot.zip";

        const string MacOSUpdaterLatestBinaryUrl = "https://9c-test.s3.ap-northeast-2.amazonaws.com/latest/Nine+Chronicles+Updater";
        const string WindowsUpdaterLatestBinaryUrl = "https://9c-test.s3.ap-northeast-2.amazonaws.com/latest/Nine+Chronicles+Updater.exe";


        static async Task Main(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += Configuration.FlushApplicationInsightLog;
            AppDomain.CurrentDomain.UnhandledException += Configuration.FlushApplicationInsightLog;

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File(CurrentPlatform.UpdaterLogFilePath, fileSizeLimitBytes: 1024 * 1024)
                .WriteTo.ApplicationInsights(
                    Configuration.TelemetryClient,
                    TelemetryConverter.Traces,
                    LogEventLevel.Information)
                .MinimumLevel.Debug()
                .CreateLogger();

            var cts = new CancellationTokenSource();
            string binaryUrl;

            await CheckUpdaterUpdate(cts.Token);

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
                    string tempPath = await DownloadBinariesAsync(u, cts.Token);
                    ExtractBinaries(tempPath);

                    if (args.Length == 0)
                    {
                        // 인스톨러 모드 - 스냅샷 다운로드
                        Console.Error.WriteLine("Start download snapshot");
                        tempPath = await DownloadBinariesAsync(SnapshotUrl, cts.Token);
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
                catch (OperationCanceledException)
                {
                    Log.Information("task was cancelled.");
                }
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var launcherFileName = CurrentPlatform.LauncherFilename;
                var cwd = new FileInfo(Process.GetCurrentProcess().MainModule.FileName).DirectoryName;
                var launcherBinaryPath = Path.Combine(
                    cwd,
                    launcherFileName,
                    "Contents",
                    "MacOS",
                    Path.GetFileNameWithoutExtension(launcherFileName));
                Process.Start(launcherBinaryPath);
            }
            else
            {
                Process.Start(CurrentPlatform.ExecutableLauncherBinaryPath);
            }
        }

        private static async Task CheckUpdaterUpdate(CancellationToken cancellationToken)
        {
            var localUpdaterPath = Process.GetCurrentProcess().MainModule.FileName;
            var localUpdaterLastWriteTime = new FileInfo(localUpdaterPath).LastWriteTime.ToUniversalTime();
            var client = new HttpClient();

            const string dateTimeFormat = "yyyy-MM-dd-HH-mm-ss";
            const string mtimeMetadataKey = "x-amz-meta-mtime";
            var updaterBinaryUrl = RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                ? MacOSUpdaterLatestBinaryUrl
                : WindowsUpdaterLatestBinaryUrl;
            var resp = await client.GetAsync(updaterBinaryUrl, cancellationToken);
            string mtimeMetadata = resp.Headers.GetValues(mtimeMetadataKey).First();
            var latestUpdaterLastWriteTime = DateTime.ParseExact(
                mtimeMetadata,
                dateTimeFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None);
            if (latestUpdaterLastWriteTime > localUpdaterLastWriteTime)
            {
                Console.Error.WriteLine("It needs to update.");
                // Download latest updater binary.
                string tempFileName = Path.GetTempFileName();
                await using var fileStream = new FileStream(tempFileName, FileMode.OpenOrCreate, FileAccess.Write);
                await resp.Content.CopyToAsync(fileStream);

                // Replace updater and run.
                string downloadedUpdaterPath = tempFileName;
                string command =
                    "sleep 3; " +
                    $"mv {EscapeShellArgument(downloadedUpdaterPath)} {EscapeShellArgument(localUpdaterPath)}; " +
                    $"chmod +x {EscapeShellArgument(localUpdaterPath)}; " +
                    $"{EscapeShellArgument(localUpdaterPath)}";

                ProcessStartInfo processStartInfo = new ProcessStartInfo
                {
                    UseShellExecute = true,
                    FileName = "/bin/bash",
                    Arguments = $"-c {EscapeShellArgument(command)}"
                };

                Process.Start(processStartInfo);
                Environment.Exit(0);
            }
        }

        private static async Task<string> DownloadBinariesAsync(
            string gameBinaryDownloadUri,
            CancellationToken cancellationToken
        )
        {
            var tempFilePath = Path.GetTempFileName();
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }

            Log.Information(
                "Start download from {DownloadUri} to {TempFilePath}.",
                gameBinaryDownloadUri,
                tempFilePath
            );

            using var httpClient = new HttpClient();
            httpClient.Timeout = Timeout.InfiniteTimeSpan;
            using var dest = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write);
            using HttpResponseMessage response = await httpClient.GetAsync(
                gameBinaryDownloadUri,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken
            );
            using var src = await response.Content.ReadAsStreamAsync();

            var buffer = new byte[8192];
            long contentLength = response.Content.Headers.ContentLength ?? 0;
            long totalRead = 0;
            int bytesRead;

            // FIXME: 표준 출력(stdout)으로 출력하고 있기 때문에, 커맨드라인 인자등을 추가해서 출력을 제어해야합니다.
            using var progressBar = new ProgressBar(
                (int)(contentLength / 1024L),
                $"Downloading from {gameBinaryDownloadUri}...",
                new ProgressBarOptions
                {
                    ProgressCharacter = '-',
                    BackgroundCharacter = '-',
                    CollapseWhenFinished = true,
                    ProgressBarOnBottom = true,
                    DisplayTimeInRealTime = false,
                }
            );

            while ((bytesRead = await src.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) != 0)
            {
                await dest.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
                totalRead += bytesRead;
                progressBar.Tick((int)(totalRead / 1024L));
                progressBar.Message = $"Downloading from {gameBinaryDownloadUri}... ({(int)(totalRead / 1024L)}KB/{(int)(contentLength / 1024L)}KB)";
            }

            Log.Information("Finished download from {DownloadUri}!", gameBinaryDownloadUri);
            return tempFilePath;
        }

        private static void ExtractBinaries(string path)
        {
            // TODO: implement a function to extract with file extension.
            Log.Information("Extracting downloaded game data...");

            var cwd = new FileInfo(Process.GetCurrentProcess().MainModule.FileName).DirectoryName;

            string settingPath = Path.Combine(cwd, Configuration.SettingFileName);
            string prevSettingPath = settingPath + ".prev";
            if (File.Exists(settingPath))
            {
                File.Copy(settingPath, prevSettingPath, true);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var process = Process.Start(
                    "tar",
                    $"-zxvf {EscapeShellArgument(path)} " +
                    $"-C {EscapeShellArgument(cwd)}"
                );
                process.WaitForExit();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                ZipFile.ExtractToDirectory(path, cwd, true);
            }
            else
            {
                throw new Exception("Unsupported platform.");
            }

            if (File.Exists(prevSettingPath))
            {
                using JsonDocument prev = JsonDocument.Parse(File.ReadAllText(prevSettingPath));
                using JsonDocument current = JsonDocument.Parse(File.ReadAllText(settingPath));
                using FileStream output = File.OpenWrite(settingPath);
                using var writer = new Utf8JsonWriter(output, new JsonWriterOptions { Indented = true });

                writer.WriteStartObject();
                foreach (JsonProperty prop in prev.RootElement.EnumerateObject())
                {
                    if (current.RootElement.TryGetProperty(prop.Name, out var currentProp))
                    {
                        writer.WritePropertyName(prop.Name);
                        currentProp.WriteTo(writer);
                    }
                    else
                    {
                        prop.WriteTo(writer);

                    }
                }
                writer.WriteEndObject();

                File.Delete(prevSettingPath);
            }

            Log.Information("Finished to extract game binary.");
        }
    }
}
