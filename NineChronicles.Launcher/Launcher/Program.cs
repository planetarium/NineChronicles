using System;
using System.Diagnostics;
using System.IO;
using Qml.Net;
using Qml.Net.Runtimes;
using Serilog;
using Serilog.Events;
using Launcher.Common;
using static Launcher.Common.RuntimePlatform.RuntimePlatform;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using static Launcher.Common.Utils;

namespace Launcher
{
    public class Program
    {
        public static int Main(string[] args)
        {
            // FIXME launcher.json 설정이 제대로 병합되지 않는 문제가 있어서 설정 파일을 강제로 받아옵니다.
            // https://github.com/planetarium/nekoyume-unity/issues/2032
            // 업데이터에서 병합 문제가 해결되면 이 코드는 제거해야 합니다.
            if (!CheckConfig())
            {
                using var wc = new WebClient();
                wc.DownloadFile(
                    // 9c-beta 클러스터 설정
                    "https://download.nine-chronicles.com/2be5da279272a3cc2ecbe329405a613c40316173773d6d2d516155d2aa67d9bb-launcher.json",
                    Path.Combine(CurrentPlatform.CurrentWorkingDirectory, "launcher.json")
                );
            }

            AppDomain.CurrentDomain.ProcessExit += Configuration.FlushApplicationInsightLog;
            AppDomain.CurrentDomain.UnhandledException += Configuration.FlushApplicationInsightLog;

            string procName = Process.GetCurrentProcess().ProcessName;
            Process[] ps = Process.GetProcessesByName(procName);
            if (ps.Length > 1)
            {
                File.WriteAllText(CurrentPlatform.RunCommandFilePath, string.Empty);
                return 0;
            }
            else if (File.Exists(CurrentPlatform.RunCommandFilePath))
            {
                File.Delete(CurrentPlatform.RunCommandFilePath);
            }

            Configuration.TelemetryClient.Context.Session.Id = Guid.NewGuid().ToString();

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

            // Set current directory to executable path.
            Log.Logger.Debug("Current working directory: {0}", CurrentPlatform.CurrentWorkingDirectory);
            Directory.SetCurrentDirectory(Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]));

            // Configure Qt Runtime directory to bundled.
            Log.Logger.Debug("Find Qt runtime from: {0}", CurrentPlatform.QtRuntimeDirectory);
            RuntimeManager.ConfigureRuntimeDirectory(CurrentPlatform.QtRuntimeDirectory);

            // FIXME: 셀프 업데이트 가능한 업데이터로 재설치 없이 교체하기 위해 들어간 코드입니다. 차후 없애는 작업이 필요할 것 같습니다.
            const string updateCheckDummyFilename = ".updater-updated";
            string updateCheckDummyPath = Path.Combine(CurrentPlatform.CurrentWorkingDirectory, updateCheckDummyFilename);
            if (!File.Exists(updateCheckDummyPath))
            {
                ReplaceUpdaterNewerAsync()
                    .ContinueWith(_ => File.Create(updateCheckDummyPath))
                    .ConfigureAwait(false);
            }

            QmlNetConfig.ShouldEnsureUIThread = false;

            using var application = new QGuiApplication(args);
            using var qmlEngine = new QQmlApplicationEngine();
            Qml.Net.Qml.RegisterType<LibplanetController>("LibplanetLauncher");
            qmlEngine.Load("qml/Main.qml");
            return application.Exec();
        }

        private static bool CheckConfig()
        {
            string configPath = Path.Combine(CurrentPlatform.CurrentWorkingDirectory, "launcher.json");

            if (File.Exists(configPath))
            {
                try
                {
                    _ = JsonDocument.Parse(File.ReadAllText(configPath));
                    return true;
                }
                catch (JsonException)
                {
                    return false;
                }
            }

            return false;
        }

        private static async Task ReplaceUpdaterNewerAsync()
        {
            // Copied from Launcher.Updater/Program.cs L:31 @ git-cf29661f72e648b96720af3c0d5910ab2bc3832b
            const string MacOSUpdaterLatestBinaryUrl = "https://download.nine-chronicles.com/latest/NineChroniclesUpdater";
            const string WindowsUpdaterLatestBinaryUrl = "https://download.nine-chronicles.com/latest/NineChroniclesUpdater.exe";
            string newUpdaterBinaryUrl = RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                ? MacOSUpdaterLatestBinaryUrl
                : RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? WindowsUpdaterLatestBinaryUrl
                    : throw new NotSupportedException();

            // 1. Download
            var tempPath = Path.GetTempFileName();
            Log.Debug(
                "Start to download updater from {NewUpdaterDownloadURL} to {TempPath}",
                newUpdaterBinaryUrl,
                tempPath);
            using var httpClient = new HttpClient();
            var stream = await httpClient.GetStreamAsync(newUpdaterBinaryUrl);
            using (var tmpFile = File.Open(tempPath, FileMode.OpenOrCreate))
            {
                await stream.CopyToAsync(tmpFile);
            }
            Log.Debug("Finished to download.");


            // 2. Replace
            File.Delete(CurrentPlatform.ExecutableUpdaterBinaryPath);
            File.Move(tempPath, CurrentPlatform.ExecutableUpdaterBinaryPath);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("chmod", $"+rx {EscapeShellArgument(CurrentPlatform.ExecutableUpdaterBinaryPath)}")
                    .WaitForExit();
            }

            Log.Debug(
                "Replaced updater to {TempPath} (from {NewUpdaterDownloadURL})",
                tempPath,
                newUpdaterBinaryUrl);
        }
    }
}
