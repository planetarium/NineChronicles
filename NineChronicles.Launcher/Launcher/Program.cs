using System;
using System.Diagnostics;
using System.IO;
using Qml.Net;
using Qml.Net.Runtimes;
using Serilog;
using Serilog.Events;
using Launcher.Common;
using static Launcher.Common.RuntimePlatform.RuntimePlatform;

namespace Launcher
{
    public class Program
    {
        public static int Main(string[] args)
        {
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
                .WriteTo.File(CurrentPlatform.LogFilePath, fileSizeLimitBytes: 20 * 1024 * 1024)
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
