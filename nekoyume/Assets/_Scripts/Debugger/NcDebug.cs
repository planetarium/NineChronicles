using System;
using JetBrains.Annotations;

namespace Nekoyume
{
    public static class NcDebug
    {
        // used for build with 'DEBUG_USE' symbol.
        [UsedImplicitly]
        public static string InsertTimestamp(string message)
        {
            return $"[{DateTime.UtcNow:yyyy-M-d HH:mm:ss}] {message}";
        }

        public static void Log(object message, string channel = "")
        {
#if UNITY_EDITOR
            UberDebug.LogChannel(channel, message.ToString());
#else
            UnityEngine.Debug.Log(InsertTimestamp(message.ToString()));
#endif
        }

        public static void Log(object message, UnityEngine.Object context, string channel = "")
        {
#if UNITY_EDITOR
            UberDebug.LogChannel(channel, message.ToString(), context);
#else
            UnityEngine.Debug.Log(InsertTimestamp(message.ToString()), context);
#endif
        }

        public static void LogFormat(string format, params object[] args)
        {
            var message = string.Format(format, args);
#if UNITY_EDITOR
            UberDebug.Log(message);
#else
            // LogFormat() in itself expands an array when it takes only one array.
            UnityEngine.Debug.Log(InsertTimestamp(message));
#endif
        }

        public static void LogWarning(object message, string channel = "")
        {
#if UNITY_EDITOR
            UberDebug.LogWarningChannel(channel, message.ToString());
#else
            UnityEngine.Debug.LogWarning(InsertTimestamp(message.ToString()));
#endif
        }

        public static void LogWarning(object message, UnityEngine.Object context, string channel = "")
        {
#if UNITY_EDITOR
            UberDebug.LogWarningChannel(channel, message.ToString(), context);
#else
            UnityEngine.Debug.LogWarning(InsertTimestamp(message.ToString()), context);
#endif
        }

        public static void LogWarningFormat(string format, params object[] args)
        {
            var message = string.Format(format, args);
#if UNITY_EDITOR
            UberDebug.LogWarning(message);
#else
            // LogWarningFormat() in itself expands an array when it takes only one array.
            UnityEngine.Debug.LogWarningFormat(InsertTimestamp(message));
#endif
        }

        public static void LogError(object message, string channel = "")
        {
#if UNITY_EDITOR
            UberDebug.LogErrorChannel(channel, message.ToString());
#else
            UnityEngine.Debug.LogError(InsertTimestamp(message.ToString()));
#endif
        }

        public static void LogError(object message, UnityEngine.Object context, string channel = "")
        {
#if UNITY_EDITOR
            UberDebug.LogErrorChannel(channel, message.ToString(), context);
#else
            UnityEngine.Debug.LogError(InsertTimestamp(message.ToString()), context);
#endif
        }

        public static void LogErrorFormat(string format, params object[] args)
        {
            var message = string.Format(format, args);
#if UNITY_EDITOR
            UberDebug.LogError(message);
#else
            // LogErrorFormat() in itself expands an array when it takes only one array.
            UnityEngine.Debug.LogErrorFormat(InsertTimestamp(message));
#endif
        }

        public static void LogException(Exception exception, string channel = "")
        {
#if UNITY_EDITOR
            UberDebug.LogErrorChannel(channel, exception.Message);
#else
            UnityEngine.Debug.LogError(InsertTimestamp(exception.Message));
#endif
        }

        public static void LogException(Exception exception, UnityEngine.Object context, string channel = "")
        {
#if UNITY_EDITOR
            UberDebug.LogErrorChannel(channel, exception.Message, context);
#else
            UnityEngine.Debug.LogError(InsertTimestamp(exception.Message), context);
#endif
        }
    }
}
