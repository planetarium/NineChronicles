using System;
using JetBrains.Annotations;
using UnityEngine;

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
    public static class NcDebugger
    {
        // used for build with 'DEBUG_USE' symbol.
        [UsedImplicitly]
        public static string InsertTimestamp(string message)
        {
            return $"[{DateTime.UtcNow:yyyy-M-d HH:mm:ss}] {message}";
        }

        public static void Log(object message)
        {
#if UNITY_EDITOR
            Debug.Log(message);
#elif DEBUG_USE
            Debug.Log(InsertTimestamp(message.ToString()));
#endif
        }

        public static void Log(object message, UnityEngine.Object context)
        {
#if UNITY_EDITOR
            Debug.Log(message, context);
#elif DEBUG_USE
            Debug.Log(InsertTimestamp(message.ToString()), context);
#endif
        }

        public static void LogFormat(string format, params object[] args)
        {
#if UNITY_EDITOR
            Debug.LogFormat(format, args);
#elif DEBUG_USE
            // LogFormat() in itself expands an array when it takes only one array.
            Debug.Log(InsertTimestamp(string.Format(format, args)));
#endif
        }

        public static void LogWarning(object message)
        {
#if UNITY_EDITOR
            Debug.LogWarning(message);
#elif DEBUG_USE
            Debug.LogWarning(InsertTimestamp(message.ToString()));
#endif
        }

        public static void LogWarning(object message, UnityEngine.Object context)
        {
#if UNITY_EDITOR
            Debug.LogWarning(message, context);
#elif DEBUG_USE
            Debug.LogWarning(InsertTimestamp(message.ToString()), context);
#endif
        }

        public static void LogWarningFormat(string format, params object[] args)
        {
#if UNITY_EDITOR
            Debug.LogWarningFormat(format, args);
#elif DEBUG_USE
            // LogWarningFormat() in itself expands an array when it takes only one array.
            Debug.LogWarningFormat(InsertTimestamp(string.Format(format, args)));
#endif
        }

        public static void LogError(object message)
        {
#if UNITY_EDITOR
            Debug.LogError(message);
#elif DEBUG_USE
            Debug.LogError(InsertTimestamp(message.ToString()));
#endif
        }

        public static void LogError(object message, UnityEngine.Object context)
        {
#if UNITY_EDITOR
            Debug.LogError(message, context);
#elif DEBUG_USE
            Debug.LogError(InsertTimestamp(message.ToString()), context);
#endif
        }

        public static void LogErrorFormat(string format, params object[] args)
        {
#if UNITY_EDITOR
            Debug.LogErrorFormat(format, args);
#elif DEBUG_USE
            // LogErrorFormat() in itself expands an array when it takes only one array.
            Debug.LogErrorFormat(InsertTimestamp(string.Format(format, args)));
#endif
        }

        public static void LogException(Exception exception)
        {
#if UNITY_EDITOR
            Debug.LogError(exception.Message);
#elif DEBUG_USE
            Debug.LogError(InsertTimestamp(exception.Message));
#endif
        }

        public static void LogException(Exception exception, UnityEngine.Object context)
        {
#if UNITY_EDITOR
            Debug.LogError(exception.Message, context);
#elif DEBUG_USE
            Debug.LogError(InsertTimestamp(exception.Message), context);
#endif
        }
    }
}
