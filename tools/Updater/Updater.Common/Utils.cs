namespace Updater.Common
{
    public static class Utils
    {
        public static string EscapeShellArgument(string value) =>
            "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }
}
