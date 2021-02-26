#if !UNITY_EDITOR

using System;

namespace Nekoyume
{
    ///
    /// It overrides UnityEngine.Debug to mute debug messages completely on a platform-specific basis.
    ///
    /// Putting this inside of 'Plugins' foloder is ok.
    ///
    /// Important:
    ///     Other preprocessor directives than 'UNITY_EDITOR' does not correctly work.
    ///
    /// Note:
    ///     [Conditional] attribute indicates to compilers that a method call or attribute should be
    ///     ignored unless a specified conditional compilation symbol is defined.
    ///
    /// See Also:
    ///     http://msdn.microsoft.com/en-us/library/system.diagnostics.conditionalattribute.aspx
    ///
    /// 2012.11. @kimsama
    ///
    public static class Debug
    {
        public static bool isDebugBuild => UnityEngine.Debug.isDebugBuild;

        public static string InsertTimestamp(string message)
        {
            return $"[{DateTime.UtcNow:yyyy-M-d HH:mm:ss}] {message}";
        }

        [System.Diagnostics.Conditional("DEBUG_USE")]
        public static void Log(object message)
        {
            UnityEngine.Debug.Log(InsertTimestamp(message.ToString()));
        }

        [System.Diagnostics.Conditional("DEBUG_USE")]
        public static void Log(object message, UnityEngine.Object context)
        {
            UnityEngine.Debug.Log(InsertTimestamp(message.ToString()), context);
        }

        [System.Diagnostics.Conditional("DEBUG_USE")]
        public static void LogFormat(string format, params object[] args)
        {
            // LogFormat() in itself expands an array when it takes only one array.
            UnityEngine.Debug.Log(InsertTimestamp(string.Format(format, args)));
        }

        [System.Diagnostics.Conditional("DEBUG_USE")]
        public static void LogWarning(object message)
        {
            UnityEngine.Debug.LogWarning(InsertTimestamp(message.ToString()));
        }

        [System.Diagnostics.Conditional("DEBUG_USE")]
        public static void LogWarning(object message, UnityEngine.Object context)
        {
            UnityEngine.Debug.LogWarning(InsertTimestamp(message.ToString()), context);
        }

        [System.Diagnostics.Conditional("DEBUG_USE")]
        public static void LogWarningFormat(string format, params object[] args)
        {
            // LogWarningFormat() in itself expands an array when it takes only one array.
            UnityEngine.Debug.LogWarningFormat(InsertTimestamp(string.Format(format, args)));
        }

        [System.Diagnostics.Conditional("DEBUG_USE")]
        public static void LogError(object message)
        {
            UnityEngine.Debug.LogError(InsertTimestamp(message.ToString()));
        }

        [System.Diagnostics.Conditional("DEBUG_USE")]
        public static void LogError(object message, UnityEngine.Object context)
        {
            UnityEngine.Debug.LogError(InsertTimestamp(message.ToString()), context);
        }

        [System.Diagnostics.Conditional("DEBUG_USE")]
        public static void LogErrorFormat(string format, params object[] args)
        {
            // LogErrorFormat() in itself expands an array when it takes only one array.
            UnityEngine.Debug.LogErrorFormat(InsertTimestamp(string.Format(format, args)));
        }

        [System.Diagnostics.Conditional("DEBUG_USE")]
        public static void LogException(Exception exception)
        {
            UnityEngine.Debug.LogError(InsertTimestamp(exception.Message));
        }

        [System.Diagnostics.Conditional("DEBUG_USE")]
        public static void LogException(Exception exception, UnityEngine.Object context)
        {
            UnityEngine.Debug.LogError(InsertTimestamp(exception.Message), context);
        }
    }
}
#endif
