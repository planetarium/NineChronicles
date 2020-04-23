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

namespace Launcher
{
    public class Program
    {
        public static int Main(string[] args)
        {
            // FIXME launcher.json 설정이 제대로 병합되지 않는 문제가 있어서 설정 파일을 강제로 받아옵니다.
            // https://github.com/planetarium/nekoyume-unity/issues/2032
            // 병합 문제가 해결되면 이 코드는 제거해야 합니다.
            if (!File.Exists(Path.Combine(CurrentPlatform.CurrentWorkingDirectory, ".preserve-settings")))
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

            QmlNetConfig.ShouldEnsureUIThread = false;
            QCoreApplication.SetAttribute(ApplicationAttribute.EnableHighDpiScaling, true);

            using var application = new QGuiApplication(args);
            using var qmlEngine = new QQmlApplicationEngine();
            Qml.Net.Qml.RegisterType<LibplanetController>("LibplanetLauncher");
            qmlEngine.Load("qml/Main.qml");
            return application.Exec();
        }
    }
}
