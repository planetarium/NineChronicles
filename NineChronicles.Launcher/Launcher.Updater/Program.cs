using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
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

        const string MacOSUpdaterLatestBinaryUrl = "https://download.nine-chronicles.com/latest/NineChroniclesUpdater";
        const string WindowsUpdaterLatestBinaryUrl = "https://download.nine-chronicles.com/latest/NineChroniclesUpdater.exe";


        static async Task Main(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += Configuration.FlushApplicationInsightLog;
            AppDomain.CurrentDomain.UnhandledException += Configuration.FlushApplicationInsightLog;

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File(
                    CurrentPlatform.LogFilePath,
                    fileSizeLimitBytes: 20 * 1024 * 1024,
                    rollOnFileSizeLimit: true,
                    retainedFileCountLimit: 5)
                .WriteTo.ApplicationInsights(
                    Configuration.TelemetryClient,
                    TelemetryConverter.Traces,
                    LogEventLevel.Information)
                .MinimumLevel.Debug()
                .CreateLogger();

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

            await CheckUpdaterUpdate(binaryUrl, cts.Token);

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

        private static async Task CheckUpdaterUpdate(string argument, CancellationToken cancellationToken)
        {
            var localUpdaterPath = Process.GetCurrentProcess().MainModule.FileName;
            if (File.Exists(localUpdaterPath + ".back"))
            {
                while (true)
                {
                    try
                    {
                        File.Delete(localUpdaterPath + ".back");
                        break;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        await Task.Delay(1000);
                    }
                }
            }

            var localUpdaterVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            using var client = new HttpClient();

            const string versionMetadataKey = "x-amz-meta-version";
            var updaterBinaryUrl = RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                ? MacOSUpdaterLatestBinaryUrl
                : WindowsUpdaterLatestBinaryUrl;
            var resp = await client.GetAsync(updaterBinaryUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            // If there is no metadata, it will not do update.
            // FIXME: 버전이 더 높은지 등으로 검사하면 좋을 것 같습니다.
            if (resp.Headers.TryGetValues(versionMetadataKey, out IEnumerable<string> latestUpdaterMD5Checksums) &&
                !string.Equals(latestUpdaterMD5Checksums.First(), localUpdaterVersion, StringComparison.InvariantCultureIgnoreCase))
            {
                Log.Debug("It needs to update.");
                // Download latest updater binary.
                string tempFileName = Path.GetTempFileName();
                using (var fileStream = new FileStream(tempFileName, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    Log.Debug("Download from {url}", updaterBinaryUrl);
                    var retry = 2;
                    while (true)
                    {
                        resp = await client.GetAsync(updaterBinaryUrl, cancellationToken);
                        if (!resp.IsSuccessStatusCode)
                        {
                            Log.Error("Can't download latest updater from {url}", updaterBinaryUrl);
                            if (retry > 0)
                            {
                                Log.Debug("Retrying...");
                                retry--;
                                continue;
                            }
                            // FIXME 예외형을 정의하고 앞에서 잡아서 제대로 표시하는 정리가 필요합니다.
                            throw new Exception($"Can't download latest updater from {updaterBinaryUrl}.");
                        }

                        await resp.Content.CopyToAsync(fileStream);
                        break;
                    }
                }

                // Replace updater and run.
                string downloadedUpdaterPath = tempFileName;
                File.Move(localUpdaterPath, localUpdaterPath + ".back");
                File.Move(downloadedUpdaterPath, localUpdaterPath);

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                    RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("chmod", $"+rx {EscapeShellArgument(localUpdaterPath)}")
                        .WaitForExit();
                }

                ProcessStartInfo processStartInfo = new ProcessStartInfo
                {
                    UseShellExecute = true,
                    FileName = localUpdaterPath,
                    Arguments = argument
                };

                Log.Debug("Restart.");
                Process.Start(processStartInfo);
                Console.Clear();
                Environment.Exit(0);
            }
        }

        private static string CalculateMD5File(string filename)
        {
            using var md5 = MD5.Create();
            using var fileStream = File.OpenRead(filename);
            var hashBytes = md5.ComputeHash(fileStream);
            return BitConverter.ToString(hashBytes).Replace("-", string.Empty);
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
