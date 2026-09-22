using UnityEngine;
using OneStore.Common.Internal;
using System;

namespace OneStore.Common
{
    public static class OneStoreLogger
    {
        private const string TAG = "ONE Store: ";

        private static readonly ILogger _logger = Debug.unityLogger;

        private static int _logLevel = 4; // default: 4 (android.util.Log.INFO)

        /// <summary>
        /// Logs a verbose-level message (for detailed debugging).
        /// This message will only be logged if log level is 2 or higher.
        /// </summary>
        public static void Verbose(string format, params object[] args)
        {
            if (_logLevel >= 2)
            {
                _logger.LogFormat(LogType.Log, TAG + format, args);
            }
        }

        /// <summary>
        /// Logs a standard log message (informational).
        /// This message will only be logged if log level is 4 or higher.
        /// </summary>
        public static void Log(string format, params object[] args)
        {
            if (_logLevel >= 4)
            {
                _logger.LogFormat(LogType.Log, TAG + format, args);
            }
        }

        /// <summary>
        /// Logs a formatted warning message with ILogger.
        /// </summary>
        public static void Warning(string format, params object[] args)
        {
            _logger.LogFormat(LogType.Warning, TAG + format, args);
        }

        /// <summary>
        /// Logs a formatted error message with ILogger.
        /// </summary>
        public static void Error(string format, params object[] args)
        {
            _logger.LogFormat(LogType.Error, TAG + format, args);
        }

        /// <summary>
        /// Logs an exception with full stack trace.
        /// </summary>
        public static void Exception(Exception exception)
        {
            _logger.LogException(exception);
        }

        /// <summary>
        /// Sets the log level of the library For the convenience of development.<br/>
        /// Caution! When deploying an app, you must set the logging level to its default value.<br/>
        /// https://developer.android.com/reference/android/util/Log#summary
        /// </summary>
        /// <param name="level">default: 4 (android.util.Log.INFO)</param>
        public static void SetLogLevel(int level)
        {
            _logLevel = level;
            var sdkLogger = new AndroidJavaObject(Constants.SdkLogger);
            sdkLogger.CallStatic(Constants.SdkLoggerSetLogLevelMethod, level);
        }

        /// <summary>
        /// Enables or disables detailed debug logging for development purposes.
        /// When enabled, sets log level to VERBOSE (2); otherwise, sets it to INFO (4).
        /// </summary>
        public static void EnableDebugLog(bool enable)
        {
            SetLogLevel(enable ? 2 : 4);
        }
    }
}
