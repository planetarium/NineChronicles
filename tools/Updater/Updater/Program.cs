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
using Updater.Common;
using Serilog;
using Serilog.Events;
using ShellProgressBar;
using static Updater.Common.RuntimePlatform.RuntimePlatform;
using static Updater.Common.Configuration.Path;
using static Updater.Common.Utils;

namespace Updater
{
    class Program
    {
        const string MacOSLatestBinaryUrl = "https://release.nine-chronicles.com/latest/macOS.tar.gz";
        const string WindowsLatestBinaryUrl = "https://release.nine-chronicles.com/latest/Windows.zip";

        private const string SnapshotUrl = "https://download.nine-chronicles.com/v100034/9c-main-snapshot.zip";

        const string MacOSUpdaterLatestBinaryUrl = "https://release.nine-chronicles.com/latest/NineChroniclesUpdater";
        const string WindowsUpdaterLatestBinaryUrl = "https://release.nine-chronicles.com/latest/NineChroniclesUpdater.exe";


        static async Task Main(string[] args)
        {
            var configuration = new Configuration();
            AppDomain.CurrentDomain.ProcessExit += Configuration.Log.FlushApplicationInsightLog;
            AppDomain.CurrentDomain.UnhandledException += Configuration.Log.FlushApplicationInsightLog;

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File(
                    CurrentPlatform.LogFilePath,
                    fileSizeLimitBytes: 20 * 1024 * 1024,
                    rollOnFileSizeLimit: true,
                    retainedFileCountLimit: 5)
                .WriteTo.ApplicationInsights(
                    Configuration.Log.TelemetryClient,
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
                }
                catch (OperationCanceledException)
                {
                    Log.Information("task was cancelled.");
                }
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

            var localUpdaterVersion = Assembly.GetExecutingAssembly().GetName().Version;
            using var client = new HttpClient();

            const string versionMetadataKey = "x-amz-meta-version";
            var updaterBinaryUrl = RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                ? MacOSUpdaterLatestBinaryUrl
                : WindowsUpdaterLatestBinaryUrl;
            var resp = await client.GetAsync(updaterBinaryUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            // 메타데이터가 없다면 업데이트를 진행하지 않습니다.
            // 메타데이터의 버전이 더 높은 경우에만 업데이트를 진행합니다.
            // https://docs.microsoft.com/en-us/dotnet/api/system.version?view=netstandard-2.0#comparing-version-objects
            if (resp.Headers.TryGetValues(versionMetadataKey, out IEnumerable<string> latestUpdaterVersions) &&
                Version.Parse(latestUpdaterVersions.First()).CompareTo(localUpdaterVersion) == 1)
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

        private static async Task<string> DownloadBinariesAsync(
            string downloadUrl,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            // FIXME: 표준 출력(stdout)으로 출력하고 있기 때문에, 커맨드라인 인자등을 추가해서 출력을 제어해야합니다.
            using var progress = new DownloadProgress(downloadUrl);
            return await Downloader.DownloadFileAsync(downloadUrl, progress, cancellationToken);
        }

        private static void ExtractBinaries(string path)
        {
            // TODO: implement a function to extract with file extension.
            Log.Information("Extracting downloaded game data...");

            var cwd = new FileInfo(Process.GetCurrentProcess().MainModule.FileName).DirectoryName;

            string settingPath = Path.Combine(cwd, SettingFileName);
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
