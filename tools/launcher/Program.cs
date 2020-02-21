using System;
using System.Diagnostics;
using System.IO;
using Qml.Net;
using Qml.Net.Runtimes;
using Serilog;

namespace Launcher
{
    public class Program
    {
        public static int Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .MinimumLevel.Debug().CreateLogger();

            // Set current directory to executable path.
            Directory.SetCurrentDirectory(Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]));

            // Configure Qt Runtime directory to bundled. 
            RuntimeManager.ConfigureRuntimeDirectory("qt-runtime");

            QmlNetConfig.ShouldEnsureUIThread = false;

            using (var application = new QGuiApplication(args))
            {
                using (var qmlEngine = new QQmlApplicationEngine())
                {
                    Qml.Net.Qml.RegisterType<LibplanetController>("LibplanetLauncher");
                    qmlEngine.Load("qml/Main.qml");
                    return application.Exec();
                }
            }
        }
    }
}
