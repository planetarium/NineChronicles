using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Launcher.Common;
using ShellProgressBar;
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
                    string tempPath = await DownloadBinariesAsync(u, cts.Token);
                    ExtractBinaries(tempPath);
                }
                catch (OperationCanceledException)
                {
                    Console.Error.WriteLine("task was cancelled.");
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
            Console.Error.WriteLine($"Start download game from '{gameBinaryDownloadUri}' to {tempFilePath}.");

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

            Console.Error.WriteLine($"Finished download from '{gameBinaryDownloadUri}'!");
            return tempFilePath;
        }

        private static void ExtractBinaries(string path)
        {
            // TODO: implement a function to extract with file extension.
            Console.Error.WriteLine("Extracting downloaded game data...");

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

            Console.Error.WriteLine("Finished to extract game binary.");
        }

        private static string EscapeShellArgument(string value) =>
            "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }
}
