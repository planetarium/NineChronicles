using System.Diagnostics;
using System.IO;
using Microsoft.Toolkit.Uwp.Notifications;

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
            var content = new ToastContent
            {
                Visual = new ToastVisual
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                        {
                            new AdaptiveText()
                            {
                                Text = title,
                                HintMaxLines = 1,
                            },
                            new AdaptiveText()
                            {
                                Text = message,
                            },
                        }
                    }
                }
            };
        }
    }
}
