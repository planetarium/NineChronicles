using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;
using System;
using System.Reflection;
using System.Text.RegularExpressions;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UberLogger
{
    //Use this to exclude methods from the stack trace
    [AttributeUsage(AttributeTargets.Method)]
    public class StackTraceIgnore : Attribute
    {
    }

    //Use this to stop UberLogger handling logs with this in the callstack.
    [AttributeUsage(AttributeTargets.Method)]
    public class LogUnityOnly : Attribute
    {
    }

    public enum LogSeverity
    {
        Message,
        Warning,
        Error
    }

    /// <summary>
    /// Interface for deriving new logger backends.
    /// Add a new logger via Logger.AddLogger()
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Logging backend entry point. logInfo contains all the information about the logging request.
        /// </summary>
        void Log(LogInfo logInfo);
    }

    /// <summary>
    /// Interface for implementing new log message filter methods.
    /// Filters will be applied to log messages before they are forwarded to any loggers or Unity itself.
    /// </summary>
    public interface IFilter
    {
        /// <summary>
        /// Apply filter to log message.
        /// Should return true if the message is to be kept, and false if the message is to be silenced.
        /// </summary>
        bool ApplyFilter(string channel, UnityEngine.Object source, LogSeverity severity, object message, params object[] par);
    }

    //Information about a particular frame of a callstack
    [Serializable]
    public class LogStackFrame
    {
        public string MethodName;
        public string DeclaringType;
        public string ParameterSig;

        public int LineNumber;
        public string FileName;

        private string FormattedMethodNameWithFileName;
        private string FormattedMethodName;
        private string FormattedFileName;

        /// <summary>
        /// Convert from a .Net stack frame
        /// </summary>
        public LogStackFrame(StackFrame frame)
        {
            var method = frame.GetMethod();
            MethodName = method.Name;
            DeclaringType = method.DeclaringType.FullName;

            var pars = method.GetParameters();
            for (var c1 = 0; c1 < pars.Length; c1++)
            {
                ParameterSig += string.Format("{0} {1}", pars[c1].ParameterType, pars[c1].Name);
                if (c1 + 1 < pars.Length)
                {
                    ParameterSig += ", ";
                }
            }

            FileName = frame.GetFileName();
            LineNumber = frame.GetFileLineNumber();
            MakeFormattedNames();
        }

        /// <summary>
        /// Convert from a Unity stack frame (for internal Unity errors rather than excpetions)
        /// </summary>
        public LogStackFrame(string unityStackFrame)
        {
            if (Logger.ExtractInfoFromUnityStackInfo(unityStackFrame, ref DeclaringType, ref MethodName, ref FileName, ref LineNumber))
            {
                MakeFormattedNames();
            }
            else
            {
                FormattedMethodNameWithFileName = unityStackFrame;
                FormattedMethodName = unityStackFrame;
                FormattedFileName = unityStackFrame;
            }
        }


        /// <summary>
        /// Basic stack frame info when we have nothing else
        /// </summary>
        public LogStackFrame(string message, string filename, int lineNumber)
        {
            FileName = filename;
            LineNumber = lineNumber;
            FormattedMethodNameWithFileName = message;
            FormattedMethodName = message;
            FormattedFileName = message;
        }


        public string GetFormattedMethodNameWithFileName()
        {
            return FormattedMethodNameWithFileName;
        }

        public string GetFormattedMethodName()
        {
            return FormattedMethodName;
        }

        public string GetFormattedFileName()
        {
            return FormattedFileName;
        }

        /// <summary>
        /// Make a nice string showing the stack information - used by the loggers
        /// </summary>
        private void MakeFormattedNames()
        {
            FormattedMethodName = string.Format("{0}.{1}({2})", DeclaringType, MethodName, ParameterSig);

            var filename = FileName;
            if (!string.IsNullOrEmpty(FileName))
            {
                var startSubName = FileName.IndexOf("Assets", StringComparison.OrdinalIgnoreCase);

                if (startSubName > 0)
                {
                    filename = FileName.Substring(startSubName);
                }
            }

            FormattedFileName = string.Format("{0}:{1}", filename, LineNumber);

            FormattedMethodNameWithFileName = string.Format("{0} (at {1})", FormattedMethodName, FormattedFileName);
        }
    }

    /// <summary>
    /// A single item of logging information
    /// </summary>
    [Serializable]
    public class LogInfo
    {
        public UnityEngine.Object Source;
        public string Channel;
        public LogSeverity Severity;
        public string Message;
        public List<LogStackFrame> Callstack;
        public LogStackFrame OriginatingSourceLocation;
        public double RelativeTimeStamp;
        private string RelativeTimeStampAsString;
        public DateTime AbsoluteTimeStamp;
        private string AbsoluteTimeStampAsString;

        public string GetRelativeTimeStampAsString()
        {
            return RelativeTimeStampAsString;
        }

        public string GetAbsoluteTimeStampAsString()
        {
            return AbsoluteTimeStampAsString;
        }

        public LogInfo(UnityEngine.Object source, string channel, LogSeverity severity, List<LogStackFrame> callstack, LogStackFrame originatingSourceLocation, object message, params object[] par)
        {
            Source = source;
            Channel = channel;
            Severity = severity;
            Message = "";
            OriginatingSourceLocation = originatingSourceLocation;

            var messageString = message as string;
            if (messageString != null)
            {
                if (par.Length > 0)
                {
                    Message = string.Format(messageString, par);
                }
                else
                {
                    Message = messageString;
                }
            }
            else
            {
                if (message != null)
                {
                    Message = message.ToString();
                }
            }

            Callstack = callstack;
            RelativeTimeStamp = Logger.GetRelativeTime();
            AbsoluteTimeStamp = DateTime.UtcNow;
            RelativeTimeStampAsString = string.Format("{0:0.0000}", RelativeTimeStamp);
            AbsoluteTimeStampAsString = AbsoluteTimeStamp.ToString("yyyy-MM-dd HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// The core of UberLogger - the entry point for logging information
    /// </summary>
    public static class Logger
    {
        // Controls how many historical messages to keep to pass into newly registered loggers
        public static int MaxMessagesToKeep = 1000;

        // If true, any logs sent to UberLogger will be forwarded on to the Unity logger.
        // Useful if you want to use both systems
        public static bool ForwardMessages = true;

        // Unity uses \n for line termination in internal multi-line strings. Use this instead of
        //  System.Environment.NewLine when you want to split multi-line strings which are emitted
        //  by Unity's internal APIs.
        public static string UnityInternalNewLine = "\n";

        // Unity uses forward-slashes as directory separators, regardless of OS.
        // Convert from this separator to System.IO.Path.DirectorySeparatorChar before passing any Unity-originated
        //  paths to APIs which expect OS-native paths.
        public static char UnityInternalDirectorySeparator = '/';

        private static List<ILogger> Loggers = new();
        private static LinkedList<LogInfo> RecentMessages = new();
        private static long StartTick;
        private static bool AlreadyLogging = false;
        private static Regex UnityMessageRegex;
        private static List<IFilter> Filters = new();

        static Logger()
        {
            // Register with Unity's logging system
// _OR_NEWER only available from 5.3+
#if UNITY_5 || UNITY_5_3_OR_NEWER
            Application.logMessageReceivedThreaded += UnityLogHandler;
#else
            Application.RegisterLogCallback(UnityLogHandler);
#endif
            StartTick = DateTime.Now.Ticks;
            UnityMessageRegex = new Regex(@"^([^\(]*)\((\d+)[^\)]*\)");
        }

        /// <summary>
        /// Registered Unity error handler
        /// </summary>
        [StackTraceIgnore]
        private static void UnityLogHandler(string logString, string stackTrace, LogType logType)
        {
            UnityLogInternal(logString, stackTrace, logType);
        }

        public static double GetRelativeTime()
        {
            var ticks = DateTime.Now.Ticks;
            return TimeSpan.FromTicks(ticks - StartTick).TotalSeconds;
        }

        /// <summary>
        /// Registers a new logger backend, which we be told every time there's a new log.
        /// if populateWithExistingMessages is true, UberLogger will immediately pump the new logger with past messages
        /// </summary>
        public static void AddLogger(ILogger logger, bool populateWithExistingMessages = true)
        {
            lock (Loggers)
            {
                if (populateWithExistingMessages)
                {
                    foreach (var oldLog in RecentMessages)
                    {
                        logger.Log(oldLog);
                    }
                }

                if (!Loggers.Contains(logger))
                {
                    Loggers.Add(logger);
                }
            }
        }

        /// <summary>
        /// Registers a new filter mechanism, which will be able to silence any future log messages
        /// </summary>
        public static void AddFilter(IFilter filter)
        {
            lock (Loggers)
            {
                Filters.Add(filter);
            }
        }

        /// <summary>
        /// Paths provided by Unity will contain forward slashes as directory separators on all OSes.
        /// This method changes all forward slashes to OS-specific directory separators.
        /// </summary>
        public static string ConvertDirectorySeparatorsFromUnityToOS(string unityFileName)
        {
            return unityFileName.Replace(UnityInternalDirectorySeparator, System.IO.Path.DirectorySeparatorChar);
        }

        /// <summary>
        /// Tries to extract useful information about the log from a Unity error message.
        /// Only used when handling a Unity error message and we can't get a useful callstack
        /// </summary>
        public static bool ExtractInfoFromUnityMessage(string log, ref string filename, ref int lineNumber)
        {
            // log = "Assets/Code/Debug.cs(140,21): warning CS0618: 'some error'
            var firstMatch = UnityMessageRegex.Match(log);

            if (firstMatch.Success)
            {
                filename = firstMatch.Groups[1].Value;
                lineNumber = Convert.ToInt32(firstMatch.Groups[2].Value);
                return true;
            }

            return false;
        }


        /// <summary>
        /// Tries to extract useful information about the log from a Unity stack trace
        /// </summary>
        public static bool ExtractInfoFromUnityStackInfo(string log, ref string declaringType, ref string methodName, ref string filename, ref int lineNumber)
        {
            // log = "DebugLoggerEditorWindow.DrawLogDetails () (at Assets/Code/Editor.cs:298)";
            var match = Regex.Matches(log, @"(.*)\.(.*)\s*\(.*\(at (.*):(\d+)");

            if (match.Count > 0)
            {
                declaringType = match[0].Groups[1].Value;
                methodName = match[0].Groups[2].Value;
                filename = match[0].Groups[3].Value;
                lineNumber = Convert.ToInt32(match[0].Groups[4].Value);
                return true;
            }

            return false;
        }


        private struct IgnoredUnityMethod
        {
            public enum Mode
            {
                Show,
                ShowIfFirstIgnoredMethod,
                Hide
            };

            public string DeclaringTypeName;
            public string MethodName;
            public Mode ShowHideMode;
        }

        // Example callstack when invoking Debug.LogWarning under Unity 5.5:
        //   Application.CallLogCallback
        //   DebugLogHandler.Internal_Log
        //   DebugLogHandler.LogFormat
        //   Logger.Log
        //   Debug.LogWarning
        //   <application callstack>


        private static IgnoredUnityMethod[] IgnoredUnityMethods = new IgnoredUnityMethod[]
        {
            // Internal trampoline, which invokes UberLogger's log callback
            new() { DeclaringTypeName = "Application", MethodName = "CallLogCallback", ShowHideMode = IgnoredUnityMethod.Mode.Hide },

            // Internal log-handling methods in Unity
            new() { DeclaringTypeName = "DebugLogHandler", MethodName = null, ShowHideMode = IgnoredUnityMethod.Mode.Hide },

            // There are several entry points to Logger. These are primarily called by Unity's own code, but could also be called directly by 3rd party code.
            // These are helpful to have on the callstack in case source code is not available (they help pinpoint the exact source code location that printed the message),
            //   but remaining ignored methods can safely be hidden
            new() { DeclaringTypeName = "Logger", MethodName = null, ShowHideMode = IgnoredUnityMethod.Mode.ShowIfFirstIgnoredMethod },

            // Many of the Debug.* entry points result in log messages being printed
            // These are helpful to have on the callstack in case source code is not available (they help pinpoint the exact source code location that printed the message),
            //   but remaining ignored methods can safely be hidden
            new() { DeclaringTypeName = "Debug", MethodName = null, ShowHideMode = IgnoredUnityMethod.Mode.ShowIfFirstIgnoredMethod },

            // Many of the Assert.* entry points result in log messages being printed
            // These are not helpful having on the callstack
            // These are helpful to have on the callstack in case source code is not available (they help pinpoint the exact source code location that printed the message),
            //   but remaining ignored methods can safely be hidden
            new() { DeclaringTypeName = "Assert", MethodName = null, ShowHideMode = IgnoredUnityMethod.Mode.ShowIfFirstIgnoredMethod }
        };

        /// <summary>
        /// Identify a number of Unity methods which we would like to scrub from a stacktrace
        /// Returns true if the method is part of scrubbing
        /// </summary>
        private static IgnoredUnityMethod.Mode ShowOrHideMethod(MethodBase method)
        {
            foreach (var ignoredUnityMethod in IgnoredUnityMethods)
            {
                if (method.DeclaringType.Name == ignoredUnityMethod.DeclaringTypeName && (ignoredUnityMethod.MethodName == null || method.Name == ignoredUnityMethod.MethodName))
                {
                    return ignoredUnityMethod.ShowHideMode;
                }
            }

            return IgnoredUnityMethod.Mode.Show;
        }

        /// <summary>
        /// Converts the curent stack trace into a list of UberLogger's LogStackFrame.
        /// Excludes any methods with the StackTraceIgnore attribute
        /// Excludes all methods (bar the first, sometimes) from a pre-defined list of ignored Unity methods/classes
        /// Returns false if the stack frame contains any methods flagged as LogUnityOnly
        /// </summary>
        [StackTraceIgnore]
        private static bool GetCallstack(ref List<LogStackFrame> callstack, out LogStackFrame originatingSourceLocation)
        {
            callstack.Clear();
            var stackTrace = new StackTrace(true); // get call stack
            var stackFrames = stackTrace.GetFrames(); // get method calls (frames)

            var encounteredIgnoredMethodPreviously = false;

            originatingSourceLocation = null;

            // Iterate backwards over stackframes; this enables us to show the "first" ignored Unity method if need be, but hide subsequent ones
            for (var i = stackFrames.Length - 1; i >= 0; i--)
            {
                var stackFrame = stackFrames[i];

                var method = stackFrame.GetMethod();
                if (method.IsDefined(typeof(LogUnityOnly), true))
                {
                    return true;
                }

                if (!method.IsDefined(typeof(StackTraceIgnore), true))
                {
                    var showHideMode = ShowOrHideMethod(method);

                    var setOriginatingSourceLocation = showHideMode == IgnoredUnityMethod.Mode.Show;

                    if (showHideMode == IgnoredUnityMethod.Mode.ShowIfFirstIgnoredMethod)
                    {
                        // "show if first ignored" methods are part of the stack trace only if no other ignored methods have been encountered already
                        if (!encounteredIgnoredMethodPreviously)
                        {
                            encounteredIgnoredMethodPreviously = true;
                            showHideMode = IgnoredUnityMethod.Mode.Show;
                        }
                        else
                        {
                            showHideMode = IgnoredUnityMethod.Mode.Hide;
                        }
                    }

                    if (showHideMode == IgnoredUnityMethod.Mode.Show)
                    {
                        var logStackFrame = new LogStackFrame(stackFrame);

                        callstack.Add(logStackFrame);

                        if (setOriginatingSourceLocation)
                        {
                            originatingSourceLocation = logStackFrame;
                        }
                    }
                }
            }

            // Callstack has been processed backwards -- correct order for presentation
            callstack.Reverse();

            return false;
        }

        /// <summary>
        /// Converts a Unity callstack string into a list of UberLogger's LogStackFrame
        /// Doesn't do any filtering, since this should only be dealing with internal Unity errors rather than client code
        /// </summary>
        private static List<LogStackFrame> GetCallstackFromUnityLog(string unityCallstack, out LogStackFrame originatingSourceLocation)
        {
            var lines = Regex.Split(unityCallstack, UnityInternalNewLine);

            var stack = new List<LogStackFrame>();
            foreach (var line in lines)
            {
                var frame = new LogStackFrame(line);
                if (!string.IsNullOrEmpty(frame.GetFormattedMethodNameWithFileName()))
                {
                    stack.Add(new LogStackFrame(line));
                }
            }

            if (stack.Count > 0)
            {
                originatingSourceLocation = stack[0];
            }
            else
            {
                originatingSourceLocation = null;
            }

            return stack;
        }

        /// <summary>
        /// The core entry point of all logging coming from Unity. Takes a log request, creates the call stack and pumps it to all the backends
        /// </summary>
        [StackTraceIgnore()]
        private static void UnityLogInternal(string unityMessage, string unityCallStack, LogType logType)
        {
            //Make sure only one thread can do this at a time.
            //This should mean that most backends don't have to worry about thread safety (unless they do complicated stuff)
            lock (Loggers)
            {
                //Prevent nasty recursion problems
                if (!AlreadyLogging)
                {
                    try
                    {
                        AlreadyLogging = true;

                        var callstack = new List<LogStackFrame>();
                        LogStackFrame originatingSourceLocation;
                        var unityOnly = GetCallstack(ref callstack, out originatingSourceLocation);
                        if (unityOnly)
                        {
                            return;
                        }

                        //If we have no useful callstack, fall back to parsing Unity's callstack 
                        if (callstack.Count == 0)
                        {
                            callstack = GetCallstackFromUnityLog(unityCallStack, out originatingSourceLocation);
                        }

                        LogSeverity severity;
                        switch (logType)
                        {
                            case LogType.Error:
                                severity = LogSeverity.Error;
                                break;
                            case LogType.Assert:
                                severity = LogSeverity.Error;
                                break;
                            case LogType.Exception:
                                severity = LogSeverity.Error;
                                break;
                            case LogType.Warning:
                                severity = LogSeverity.Warning;
                                break;
                            default:
                                severity = LogSeverity.Message;
                                break;
                        }

                        var filename = "";
                        var lineNumber = 0;

                        //Finally, parse the error message so we can get basic file and line information
                        if (ExtractInfoFromUnityMessage(unityMessage, ref filename, ref lineNumber))
                        {
                            callstack.Insert(0, new LogStackFrame(unityMessage, filename, lineNumber));
                        }

                        var logInfo = new LogInfo(null, "", severity, callstack, originatingSourceLocation, unityMessage);

                        //Add this message to our history
                        RecentMessages.AddLast(logInfo);

                        //Make sure our history doesn't get too big
                        TrimOldMessages();

                        //Delete any dead loggers and pump them with the new log
                        Loggers.RemoveAll(l => l == null);
                        Loggers.ForEach(l => l.Log(logInfo));
                    }
                    finally
                    {
                        AlreadyLogging = false;
                    }
                }
            }
        }


        /// <summary>
        /// The core entry point of all logging coming from client code.
        /// Takes a log request, creates the call stack and pumps it to all the backends
        /// </summary>
        [StackTraceIgnore()]
        public static void Log(string channel, UnityEngine.Object source, LogSeverity severity, object message, params object[] par)
        {
            lock (Loggers)
            {
                if (!AlreadyLogging)
                {
                    try
                    {
                        AlreadyLogging = true;

                        foreach (var filter in Filters)
                        {
                            if (!filter.ApplyFilter(channel, source, severity, message, par))
                            {
                                return;
                            }
                        }

                        var callstack = new List<LogStackFrame>();
                        LogStackFrame originatingSourceLocation;
                        var unityOnly = GetCallstack(ref callstack, out originatingSourceLocation);
                        if (unityOnly)
                        {
                            return;
                        }

                        var logInfo = new LogInfo(source, channel, severity, callstack, originatingSourceLocation, message, par);

                        //Add this message to our history
                        RecentMessages.AddLast(logInfo);

                        //Make sure our history doesn't get too big
                        TrimOldMessages();

                        //Delete any dead loggers and pump them with the new log
                        Loggers.RemoveAll(l => l == null);
                        Loggers.ForEach(l => l.Log(logInfo));

                        //If required, pump this message back into Unity
                        if (ForwardMessages)
                        {
                            ForwardToUnity(source, severity, message, par);
                        }
                    }
                    finally
                    {
                        AlreadyLogging = false;
                    }
                }
            }
        }

        /// <summary>
        /// Forwards an UberLogger log to Unity so it's visible in the built-in console
        /// </summary>
        [LogUnityOnly()]
        private static void ForwardToUnity(UnityEngine.Object source, LogSeverity severity, object message, params object[] par)
        {
            object showObject = null;
            if (message != null)
            {
                var messageAsString = message as string;
                if (messageAsString != null)
                {
                    if (par.Length > 0)
                    {
                        showObject = string.Format(messageAsString, par);
                    }
                    else
                    {
                        showObject = message;
                    }
                }
                else
                {
                    showObject = message;
                }
            }

            if (source == null)
            {
                if (severity == LogSeverity.Message)
                {
                    UnityEngine.Debug.Log(showObject);
                }
                else if (severity == LogSeverity.Warning)
                {
                    UnityEngine.Debug.LogWarning(showObject);
                }
                else if (severity == LogSeverity.Error)
                {
                    UnityEngine.Debug.LogError(showObject);
                }
            }
            else
            {
                if (severity == LogSeverity.Message)
                {
                    UnityEngine.Debug.Log(showObject, source);
                }
                else if (severity == LogSeverity.Warning)
                {
                    UnityEngine.Debug.LogWarning(showObject, source);
                }
                else if (severity == LogSeverity.Error)
                {
                    UnityEngine.Debug.LogError(showObject, source);
                }
            }
        }

        /// <summary>
        /// Finds a registered logger, if it exists
        /// </summary>
        public static T GetLogger<T>() where T : class
        {
            foreach (var logger in Loggers)
            {
                if (logger is T)
                {
                    return logger as T;
                }
            }

            return null;
        }

        private static void TrimOldMessages()
        {
            while (RecentMessages.Count > MaxMessagesToKeep)
            {
                RecentMessages.RemoveFirst();
            }
        }
    }
}
