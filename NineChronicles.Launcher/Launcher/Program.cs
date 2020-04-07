using System;
using System.IO;
using Qml.Net;
using Qml.Net.Runtimes;
using Serilog;
using static Launcher.Common.RuntimePlatform.RuntimePlatform;

namespace Launcher
{
    public class Program
    {
        public static int Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File(CurrentPlatform.LogFilePath, rollingInterval: RollingInterval.Day)
                .MinimumLevel.Debug().CreateLogger();

            // Set current directory to executable path.
            Directory.SetCurrentDirectory(Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]));

            // Configure Qt Runtime directory to bundled.
            RuntimeManager.ConfigureRuntimeDirectory(Path.Combine(CurrentPlatform.CurrentWorkingDirectory, "qt-runtime"));

            QmlNetConfig.ShouldEnsureUIThread = false;

            using var application = new QGuiApplication(args);
            using var qmlEngine = new QQmlApplicationEngine();
            Qml.Net.Qml.RegisterType<LibplanetController>("LibplanetLauncher");
            qmlEngine.Load("qml/Main.qml");
            return application.Exec();
        }
    }
}
