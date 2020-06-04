using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Windows.UI.Notifications;
using Serilog;

namespace Launcher.Common.RuntimePlatform
{
    public class WindowsPlatform : IRuntimePlatform
    {
        public string GameBinaryDownloadFilename => "win.zip";

        public string GameBinaryFilename => "9c.exe";

        public string LauncherFilename => "Nine Chronicles.exe";

        public string OpenCommand => "notepad.exe";

        public string CurrentWorkingDirectory =>
            new FileInfo(Process.GetCurrentProcess().MainModule.FileName).DirectoryName;

        public string QtRuntimeDirectory =>
            Path.Combine(CurrentWorkingDirectory, "qt-runtime");

        public string ExecutableLauncherBinaryPath =>
            Path.Combine(CurrentWorkingDirectory, LauncherFilename);

        public string ExecutableGameBinaryPath =>
            Path.Combine(CurrentWorkingDirectory, GameBinaryFilename);

        public string ExecutableUpdaterBinaryPath =>
            Path.Combine(CurrentWorkingDirectory, "Nine Chronicles Updater.exe");

        public string LogFilePath =>
            Path.Combine(CurrentWorkingDirectory, "Logs", "launcher.log");

        public string UpdaterLogFilePath =>
            Path.Combine(CurrentWorkingDirectory, "Logs", "updater.log");

        public void DisplayNotification(string title, string message)
        {
            try
            {
                var template =
                    ToastNotificationManager.GetTemplateContent(ToastTemplateType
                        .ToastImageAndText02);
                template.GetElementsByTagName("image")[0].Attributes.GetNamedItem("src").InnerText =
                    "file://" +
                    Path.Join(
                        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                        "images",
                        "logo-0.png");
                template.GetElementsByTagName("text").Item(0).InnerText = title;
                template.GetElementsByTagName("text").Item(1).InnerText = message;
                ToastNotificationManager.CreateToastNotifier("NineChronicles Notifier")
                    .Show(new ToastNotification(template));
            }
            catch (Exception e)
            {
                Log.Error(e, e.Message);
            }
        }
    }
}
