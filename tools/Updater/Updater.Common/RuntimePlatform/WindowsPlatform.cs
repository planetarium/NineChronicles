using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Windows.UI.Notifications;
using Serilog;

namespace Updater.Common.RuntimePlatform
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
            Version v = Environment.OSVersion.Version;
            try
            {
                if (v.Major > 6 || v.Major == 6 && v.Minor >= 3)
                {
                    // 다음 호출되는 메서드의 코드를 본 if문 안쪽에 바로 넣으면, 실행 시간에 이 if문
                    // 조건이 평가되기 전에 DisplayNotification() 메서드 자체가 호출될 시점에 바로
                    // TypeLoadException이 발생하는 듯. 따라서 플랫폼 의존적인 코드는 별도 메서드로
                    // 격리하고 조건이 맞을 때만 호출해야 함.
                    DisplayNotificationAboveWindows10(title, message);
                }
                else
                {
                    // Windows 8 이전에는 운영체제 수준에서 제공하는 표준적인 알림 기능이 없습니다.
                    // 그래서 비슷한 것을 제공하는 Notifu라는 CLI 앱을 번들하여 사용합니다:
                    //   http://www.paralint.com/projects/notifu/
                    // 론처 resources 하위에 넣어 빌드할 때 포함하게 하고 있습니다.
                    string notifuExePath = Path.Combine(
                        Path.GetDirectoryName(ExecutableLauncherBinaryPath),
                        "notifu64.exe"
                    );

                    // cmd.exe의 인자 이스케이프 문법은 아주 기괴해서 임의의 문자열을 일반적으로
                    // 이스케이프해주는 함수를 제대로 구현하기는 어려운 것 같습니다.
                    // https://stackoverflow.com/a/31413730/383405
                    string arguments =
                        $"/i parent /p \"Nine Chronicles\" /m \"{title}\n{message}\"";
                    Process.Start(notifuExePath, arguments);
                }
            }
            catch (Exception e)
            {
                Log.Error(e, e.Message);
            }
        }

        private void DisplayNotificationAboveWindows10(string title, string message)
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
    }
}
