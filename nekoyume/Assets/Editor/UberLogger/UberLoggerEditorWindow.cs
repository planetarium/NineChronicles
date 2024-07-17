using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;
using UberLogger;
using System.Text.RegularExpressions;

/// <summary>
/// The console logging frontend.
/// Pulls data from the UberLoggerEditor backend
/// </summary>
public class UberLoggerEditorWindow : EditorWindow, UberLoggerEditor.ILoggerWindow
{
    [MenuItem("Window/Show Uber Console")]
    public static void ShowLogWindow()
    {
        Init();
    }

    public static void Init()
    {
        var window = CreateInstance<UberLoggerEditorWindow>();
        window.Show();
        window.position = new Rect(200, 200, 400, 300);
        window.CurrentTopPaneHeight = window.position.height / 2;
    }

    public void OnLogChange(LogInfo logInfo)
    {
        Dirty = true;
        // Repaint();
    }


    private void OnInspectorUpdate()
    {
        // Debug.Log("Update");
        if (Dirty)
        {
            Repaint();
        }
    }

    private void OnEnable()
    {
        // Connect to or create the backend
        if (!EditorLogger)
        {
            EditorLogger = UberLogger.Logger.GetLogger<UberLoggerEditor>();
            if (!EditorLogger)
            {
                EditorLogger = UberLoggerEditor.Create();
            }
        }

        // UberLogger doesn't allow for duplicate loggers, so this is safe
        // And, due to Unity serialisation stuff, necessary to do to it here.
        UberLogger.Logger.AddLogger(EditorLogger);
        EditorLogger.AddWindow(this);

// _OR_NEWER only became available from 5.3
#if UNITY_5 || UNITY_5_3_OR_NEWER
        titleContent.text = "Uber Console";
#else
        title = "Uber Console";

#endif

        ClearSelectedMessage();

        SmallErrorIcon = EditorGUIUtility.FindTexture("d_console.erroricon.sml");
        SmallWarningIcon = EditorGUIUtility.FindTexture("d_console.warnicon.sml");
        SmallMessageIcon = EditorGUIUtility.FindTexture("d_console.infoicon.sml");
        ErrorIcon = SmallErrorIcon;
        WarningIcon = SmallWarningIcon;
        MessageIcon = SmallMessageIcon;
        Dirty = true;
        Repaint();
    }

    /// <summary>
    /// Converts the entire message log to a multiline string
    /// </summary>
    public string ExtractLogListToString()
    {
        var result = "";
        foreach (var log in RenderLogs)
        {
            var logInfo = log.Log;
            result += logInfo.GetRelativeTimeStampAsString() + ": " + logInfo.Severity + ": " + logInfo.Message + "\n";
        }

        return result;
    }

    /// <summary> 
    /// Converts the currently-displayed stack to a multiline string 
    /// </summary> 
    public string ExtractLogDetailsToString()
    {
        var result = "";
        if (RenderLogs.Count > 0 && SelectedRenderLog >= 0)
        {
            var countedLog = RenderLogs[SelectedRenderLog];
            var log = countedLog.Log;

            for (var c1 = 0; c1 < log.Callstack.Count; c1++)
            {
                var frame = log.Callstack[c1];
                var methodName = frame.GetFormattedMethodName();
                result += methodName + "\n";
            }
        }

        return result;
    }

    /// <summary> 
    /// Handle "Copy" command; copies log & stacktrace contents to clipboard
    /// </summary> 
    public void HandleCopyToClipboard()
    {
        const string copyCommandName = "Copy";

        var e = Event.current;
        if (e.type == EventType.ValidateCommand && e.commandName == copyCommandName)
        {
            // Respond to "Copy" command

            // Confirm that we will consume the command; this will result in the command being re-issued with type == EventType.ExecuteCommand
            e.Use();
        }
        else if (e.type == EventType.ExecuteCommand && e.commandName == copyCommandName)
        {
            // Copy current message log and current stack to the clipboard 

            // Convert all messages to a single long string 
            // It would be preferable to only copy one of the two, but that requires UberLogger to have focus handling 
            // between the message log and stack views 
            var result = ExtractLogListToString();

            result += "\n";

            // Convert current callstack to a single long string 
            result += ExtractLogDetailsToString();

            GUIUtility.systemCopyBuffer = result;
        }
    }

    private Vector2 DrawPos;

    public void OnGUI()
    {
        //Set up the basic style, based on the Unity defaults
        //A bit hacky, but means we don't have to ship an editor guistyle and can fit in to pro and free skins
        var defaultLineColor = GUI.backgroundColor;
        GUIStyle unityLogLineEven = null;
        GUIStyle unityLogLineOdd = null;
        GUIStyle unitySmallLogLine = null;

        foreach (var style in GUI.skin.customStyles)
        {
            if (style.name == "CN EntryBackEven")
            {
                unityLogLineEven = style;
            }
            else if (style.name == "CN EntryBackOdd")
            {
                unityLogLineOdd = style;
            }
            else if (style.name == "CN StatusInfo")
            {
                unitySmallLogLine = style;
            }
        }

        EntryStyleBackEven = new GUIStyle(unitySmallLogLine);

        EntryStyleBackEven.normal = unityLogLineEven.normal;
        EntryStyleBackEven.margin = new RectOffset(0, 0, 0, 0);
        EntryStyleBackEven.border = new RectOffset(0, 0, 0, 0);
        EntryStyleBackEven.fixedHeight = 0;

        EntryStyleBackOdd = new GUIStyle(EntryStyleBackEven);
        EntryStyleBackOdd.normal = unityLogLineOdd.normal;
        // EntryStyleBackOdd = new GUIStyle(unityLogLine);


        SizerLineColour = new Color(defaultLineColor.r * 0.5f, defaultLineColor.g * 0.5f, defaultLineColor.b * 0.5f);

        // GUILayout.BeginVertical(GUILayout.Height(topPanelHeaderHeight), GUILayout.MinHeight(topPanelHeaderHeight));
        ResizeTopPane();
        DrawPos = Vector2.zero;
        DrawToolbar();
        DrawFilter();

        DrawChannels();

        var logPanelHeight = CurrentTopPaneHeight - DrawPos.y;

        if (Dirty)
        {
            CurrentLogList = EditorLogger.CopyLogInfo();
        }

        DrawLogList(logPanelHeight);

        DrawPos.y += DividerHeight;

        DrawLogDetails();

        HandleCopyToClipboard();

        //If we're dirty, do a repaint
        Dirty = false;
        if (MakeDirty)
        {
            Dirty = true;
            MakeDirty = false;
            Repaint();
        }
    }

    //Some helper functions to draw buttons that are only as big as their text
    private bool ButtonClamped(string text, GUIStyle style, out Vector2 size)
    {
        var content = new GUIContent(text);
        size = style.CalcSize(content);
        var rect = new Rect(DrawPos, size);
        return GUI.Button(rect, text, style);
    }

    private bool ToggleClamped(bool state, string text, GUIStyle style, out Vector2 size)
    {
        var content = new GUIContent(text);
        return ToggleClamped(state, content, style, out size);
    }

    private bool ToggleClamped(bool state, GUIContent content, GUIStyle style, out Vector2 size)
    {
        size = style.CalcSize(content);
        var drawRect = new Rect(DrawPos, size);
        return GUI.Toggle(drawRect, state, content, style);
    }

    private void LabelClamped(string text, GUIStyle style, out Vector2 size)
    {
        var content = new GUIContent(text);
        size = style.CalcSize(content);

        var drawRect = new Rect(DrawPos, size);
        GUI.Label(drawRect, text, style);
    }

    /// <summary>
    /// Draws the thin, Unity-style toolbar showing error counts and toggle buttons
    /// </summary>
    private void DrawToolbar()
    {
        var toolbarStyle = EditorStyles.toolbarButton;

        Vector2 elementSize;
        if (ButtonClamped("Clear", EditorStyles.toolbarButton, out elementSize))
        {
            EditorLogger.Clear();
        }

        DrawPos.x += elementSize.x;
        EditorLogger.ClearOnPlay = ToggleClamped(EditorLogger.ClearOnPlay, "Clear On Play", EditorStyles.toolbarButton, out elementSize);
        DrawPos.x += elementSize.x;
        EditorLogger.PauseOnError = ToggleClamped(EditorLogger.PauseOnError, "Error Pause", EditorStyles.toolbarButton, out elementSize);
        DrawPos.x += elementSize.x;
        var showTimes = ToggleClamped(ShowTimes, "Times", EditorStyles.toolbarButton, out elementSize);
        if (showTimes != ShowTimes)
        {
            MakeDirty = true;
            ShowTimes = showTimes;
        }

        DrawPos.x += elementSize.x;
        var showChannels = ToggleClamped(ShowChannels, "Channels", EditorStyles.toolbarButton, out elementSize);
        if (showChannels != ShowChannels)
        {
            MakeDirty = true;
            ShowChannels = showChannels;
        }

        DrawPos.x += elementSize.x;
        var collapse = ToggleClamped(Collapse, "Collapse", EditorStyles.toolbarButton, out elementSize);
        if (collapse != Collapse)
        {
            MakeDirty = true;
            Collapse = collapse;
            SelectedRenderLog = -1;
        }

        DrawPos.x += elementSize.x;

        ScrollFollowMessages = ToggleClamped(ScrollFollowMessages, "Follow", EditorStyles.toolbarButton, out elementSize);
        DrawPos.x += elementSize.x;

        var errorToggleContent = new GUIContent(EditorLogger.NoErrors.ToString(), SmallErrorIcon);
        var warningToggleContent = new GUIContent(EditorLogger.NoWarnings.ToString(), SmallWarningIcon);
        var messageToggleContent = new GUIContent(EditorLogger.NoMessages.ToString(), SmallMessageIcon);

        var totalErrorButtonWidth = toolbarStyle.CalcSize(errorToggleContent).x + toolbarStyle.CalcSize(warningToggleContent).x + toolbarStyle.CalcSize(messageToggleContent).x;

        var errorIconX = position.width - totalErrorButtonWidth;
        if (errorIconX > DrawPos.x)
        {
            DrawPos.x = errorIconX;
        }

        var showErrors = ToggleClamped(ShowErrors, errorToggleContent, toolbarStyle, out elementSize);
        DrawPos.x += elementSize.x;
        var showWarnings = ToggleClamped(ShowWarnings, warningToggleContent, toolbarStyle, out elementSize);
        DrawPos.x += elementSize.x;
        var showMessages = ToggleClamped(ShowMessages, messageToggleContent, toolbarStyle, out elementSize);
        DrawPos.x += elementSize.x;

        DrawPos.y += elementSize.y;
        DrawPos.x = 0;

        //If the errors/warning to show has changed, clear the selected message
        if (showErrors != ShowErrors || showWarnings != ShowWarnings || showMessages != ShowMessages)
        {
            ClearSelectedMessage();
            MakeDirty = true;
        }

        ShowWarnings = showWarnings;
        ShowMessages = showMessages;
        ShowErrors = showErrors;
    }

    /// <summary>
    /// Draws the channel selector
    /// </summary>
    private void DrawChannels()
    {
        var channels = GetChannels();
        var currentChannelIndex = 0;
        for (var c1 = 0; c1 < channels.Count; c1++)
        {
            if (channels[c1] == CurrentChannel)
            {
                currentChannelIndex = c1;
                break;
            }
        }

        var content = new GUIContent("S");
        var size = GUI.skin.button.CalcSize(content);
        var drawRect = new Rect(DrawPos, new Vector2(position.width, size.y));
        currentChannelIndex = GUI.SelectionGrid(drawRect, currentChannelIndex, channels.ToArray(), channels.Count);
        if (CurrentChannel != channels[currentChannelIndex])
        {
            CurrentChannel = channels[currentChannelIndex];
            ClearSelectedMessage();
            MakeDirty = true;
        }

        DrawPos.y += size.y;
    }

    /// <summary>
    /// Based on filter and channel selections, should this log be shown?
    /// </summary>
    private bool ShouldShowLog(Regex regex, LogInfo log)
    {
        if (log.Channel == CurrentChannel || CurrentChannel == "All" || (CurrentChannel == "No Channel" && string.IsNullOrEmpty(log.Channel)))
        {
            if ((log.Severity == LogSeverity.Message && ShowMessages)
                || (log.Severity == LogSeverity.Warning && ShowWarnings)
                || (log.Severity == LogSeverity.Error && ShowErrors))
            {
                if (regex == null || regex.IsMatch(log.Message))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Converts a given log element into a piece of gui content to be displayed
    /// </summary>
    private GUIContent GetLogLineGUIContent(LogInfo log, bool showTimes, bool showChannels)
    {
        var showMessage = log.Message;

        //Make all messages single line
        showMessage = showMessage.Replace(UberLogger.Logger.UnityInternalNewLine, " ");

        // Format the message as follows:
        //     [channel] 0.000 : message  <-- Both channel and time shown
        //     0.000 : message            <-- Time shown, channel hidden
        //     [channel] : message        <-- Channel shown, time hidden
        //     message                    <-- Both channel and time hidden
        var showChannel = showChannels && !string.IsNullOrEmpty(log.Channel);
        var channelMessage = showChannel ? string.Format("[{0}]", log.Channel) : "";
        var channelTimeSeparator = showChannel && showTimes ? " " : "";
        var timeMessage = showTimes ? string.Format("{0}", log.GetRelativeTimeStampAsString()) : "";
        var prefixMessageSeparator = showChannel || showTimes ? " : " : "";
        showMessage = string.Format("{0}{1}{2}{3}{4}",
            channelMessage,
            channelTimeSeparator,
            timeMessage,
            prefixMessageSeparator,
            showMessage
        );

        var content = new GUIContent(showMessage, GetIconForLog(log));
        return content;
    }

    /// <summary>
    /// Draws the main log panel
    /// </summary>
    public void DrawLogList(float height)
    {
        var oldColor = GUI.backgroundColor;


        float buttonY = 0;

        Regex filterRegex = null;

        if (!string.IsNullOrEmpty(FilterRegex))
        {
            filterRegex = new Regex(FilterRegex);
        }

        var collapseBadgeStyle = EditorStyles.miniButton;
        var logLineStyle = EntryStyleBackEven;

        // If we've been marked dirty, we need to recalculate the elements to be displayed
        if (Dirty)
        {
            LogListMaxWidth = 0;
            LogListLineHeight = 0;
            CollapseBadgeMaxWidth = 0;
            RenderLogs.Clear();

            //When collapsed, count up the unique elements and use those to display
            if (Collapse)
            {
                var collapsedLines = new Dictionary<string, CountedLog>();
                var collapsedLinesList = new List<CountedLog>();

                foreach (var log in CurrentLogList)
                {
                    if (ShouldShowLog(filterRegex, log))
                    {
                        var matchString = log.Message + "!$" + log.Severity + "!$" + log.Channel;

                        CountedLog countedLog;
                        if (collapsedLines.TryGetValue(matchString, out countedLog))
                        {
                            countedLog.Count++;
                        }
                        else
                        {
                            countedLog = new CountedLog(log, 1);
                            collapsedLines.Add(matchString, countedLog);
                            collapsedLinesList.Add(countedLog);
                        }
                    }
                }

                foreach (var countedLog in collapsedLinesList)
                {
                    var content = GetLogLineGUIContent(countedLog.Log, ShowTimes, ShowChannels);
                    RenderLogs.Add(countedLog);
                    var logLineSize = logLineStyle.CalcSize(content);
                    LogListMaxWidth = Mathf.Max(LogListMaxWidth, logLineSize.x);
                    LogListLineHeight = Mathf.Max(LogListLineHeight, logLineSize.y);

                    var collapseBadgeContent = new GUIContent(countedLog.Count.ToString());
                    var collapseBadgeSize = collapseBadgeStyle.CalcSize(collapseBadgeContent);
                    CollapseBadgeMaxWidth = Mathf.Max(CollapseBadgeMaxWidth, collapseBadgeSize.x);
                }
            }
            //If we're not collapsed, display everything in order
            else
            {
                foreach (var log in CurrentLogList)
                {
                    if (ShouldShowLog(filterRegex, log))
                    {
                        var content = GetLogLineGUIContent(log, ShowTimes, ShowChannels);
                        RenderLogs.Add(new CountedLog(log, 1));
                        var logLineSize = logLineStyle.CalcSize(content);
                        LogListMaxWidth = Mathf.Max(LogListMaxWidth, logLineSize.x);
                        LogListLineHeight = Mathf.Max(LogListLineHeight, logLineSize.y);
                    }
                }
            }

            LogListMaxWidth += CollapseBadgeMaxWidth;
        }

        var scrollRect = new Rect(DrawPos, new Vector2(position.width, height));
        var lineWidth = Mathf.Max(LogListMaxWidth, scrollRect.width);

        var contentRect = new Rect(0, 0, lineWidth, RenderLogs.Count * LogListLineHeight);
        var lastScrollPosition = LogListScrollPosition;
        LogListScrollPosition = GUI.BeginScrollView(scrollRect, LogListScrollPosition, contentRect);

        //If we're following the messages but the user has moved, cancel following
        if (ScrollFollowMessages)
        {
            if (lastScrollPosition.y - LogListScrollPosition.y > LogListLineHeight)
            {
                UberDebug.UnityLog(string.Format("{0} {1}", lastScrollPosition.y, LogListScrollPosition.y));
                ScrollFollowMessages = false;
            }
        }

        var logLineX = CollapseBadgeMaxWidth;

        //Render all the elements
        var firstRenderLogIndex = (int)(LogListScrollPosition.y / LogListLineHeight);
        var lastRenderLogIndex = firstRenderLogIndex + (int)(height / LogListLineHeight);

        firstRenderLogIndex = Mathf.Clamp(firstRenderLogIndex, 0, RenderLogs.Count);
        lastRenderLogIndex = Mathf.Clamp(lastRenderLogIndex, 0, RenderLogs.Count);
        buttonY = firstRenderLogIndex * LogListLineHeight;

        for (var renderLogIndex = firstRenderLogIndex; renderLogIndex < lastRenderLogIndex; renderLogIndex++)
        {
            var countedLog = RenderLogs[renderLogIndex];
            var log = countedLog.Log;
            logLineStyle = renderLogIndex % 2 == 0 ? EntryStyleBackEven : EntryStyleBackOdd;
            if (renderLogIndex == SelectedRenderLog)
            {
                GUI.backgroundColor = new Color(0.5f, 0.5f, 1);
            }
            else
            {
                GUI.backgroundColor = Color.white;
            }

            //Make all messages single line
            var content = GetLogLineGUIContent(log, ShowTimes, ShowChannels);
            var drawRect = new Rect(logLineX, buttonY, contentRect.width, LogListLineHeight);
            if (GUI.Button(drawRect, content, logLineStyle))
            {
                //Select a message, or jump to source if it's double-clicked
                if (renderLogIndex == SelectedRenderLog)
                {
                    if (EditorApplication.timeSinceStartup - LastMessageClickTime < DoubleClickInterval)
                    {
                        LastMessageClickTime = 0;
                        // Attempt to display source code associated with messages. Search through all stackframes,
                        //   until we find a stackframe that can be displayed in source code view
                        for (var frame = 0; frame < log.Callstack.Count; frame++)
                        {
                            if (JumpToSource(log.Callstack[frame]))
                            {
                                break;
                            }
                        }
                    }
                    else
                    {
                        LastMessageClickTime = EditorApplication.timeSinceStartup;
                    }
                }
                else
                {
                    SelectedRenderLog = renderLogIndex;
                    SelectedCallstackFrame = -1;
                    LastMessageClickTime = EditorApplication.timeSinceStartup;
                }


                //Always select the game object that is the source of this message
                var go = log.Source as GameObject;
                if (go != null)
                {
                    Selection.activeGameObject = go;
                }
            }

            if (Collapse)
            {
                var collapseBadgeContent = new GUIContent(countedLog.Count.ToString());
                var collapseBadgeSize = collapseBadgeStyle.CalcSize(collapseBadgeContent);
                var collapseBadgeRect = new Rect(0, buttonY, collapseBadgeSize.x, collapseBadgeSize.y);
                GUI.Button(collapseBadgeRect, collapseBadgeContent, collapseBadgeStyle);
            }

            buttonY += LogListLineHeight;
        }

        //If we're following the log, move to the end
        if (ScrollFollowMessages && RenderLogs.Count > 0)
        {
            LogListScrollPosition.y = (RenderLogs.Count + 1) * LogListLineHeight - scrollRect.height;
        }

        GUI.EndScrollView();
        DrawPos.y += height;
        DrawPos.x = 0;
        GUI.backgroundColor = oldColor;
    }


    /// <summary>
    /// The bottom of the panel - details of the selected log
    /// </summary>
    public void DrawLogDetails()
    {
        var oldColor = GUI.backgroundColor;

        SelectedRenderLog = Mathf.Clamp(SelectedRenderLog, 0, CurrentLogList.Count);

        if (RenderLogs.Count > 0 && SelectedRenderLog >= 0)
        {
            var countedLog = RenderLogs[SelectedRenderLog];
            var log = countedLog.Log;
            var logLineStyle = EntryStyleBackEven;

            var sourceStyle = new GUIStyle(GUI.skin.textArea);
            sourceStyle.richText = true;

            var drawRect = new Rect(DrawPos, new Vector2(position.width - DrawPos.x, position.height - DrawPos.y));

            //Work out the content we need to show, and the sizes
            var detailLines = new List<GUIContent>();
            float contentHeight = 0;
            float contentWidth = 0;
            float lineHeight = 0;


            for (var c1 = 0; c1 < log.Callstack.Count; c1++)
            {
                var frame = log.Callstack[c1];
                var methodName = frame.GetFormattedMethodNameWithFileName();
                if (!string.IsNullOrEmpty(methodName))
                {
                    var content = new GUIContent(methodName);
                    detailLines.Add(content);

                    var contentSize = logLineStyle.CalcSize(content);
                    contentHeight += contentSize.y;
                    lineHeight = Mathf.Max(lineHeight, contentSize.y);
                    contentWidth = Mathf.Max(contentSize.x, contentWidth);
                    if (ShowFrameSource && c1 == SelectedCallstackFrame)
                    {
                        var sourceContent = GetFrameSourceGUIContent(frame);
                        if (sourceContent != null)
                        {
                            var sourceSize = sourceStyle.CalcSize(sourceContent);
                            contentHeight += sourceSize.y;
                            contentWidth = Mathf.Max(sourceSize.x, contentWidth);
                        }
                    }
                }
            }

            //Render the content
            var contentRect = new Rect(0, 0, Mathf.Max(contentWidth, drawRect.width), contentHeight);

            LogDetailsScrollPosition = GUI.BeginScrollView(drawRect, LogDetailsScrollPosition, contentRect);

            float lineY = 0;
            for (var c1 = 0; c1 < detailLines.Count; c1++)
            {
                var lineContent = detailLines[c1];
                if (lineContent != null)
                {
                    logLineStyle = c1 % 2 == 0 ? EntryStyleBackEven : EntryStyleBackOdd;
                    if (c1 == SelectedCallstackFrame)
                    {
                        GUI.backgroundColor = new Color(0.5f, 0.5f, 1);
                    }
                    else
                    {
                        GUI.backgroundColor = Color.white;
                    }

                    var frame = log.Callstack[c1];
                    var lineRect = new Rect(0, lineY, contentRect.width, lineHeight);

                    // Handle clicks on the stack frame
                    if (GUI.Button(lineRect, lineContent, logLineStyle))
                    {
                        if (c1 == SelectedCallstackFrame)
                        {
                            if (Event.current.button == 1)
                            {
                                ToggleShowSource(frame);
                                Repaint();
                            }
                            else
                            {
                                if (EditorApplication.timeSinceStartup - LastFrameClickTime < DoubleClickInterval)
                                {
                                    LastFrameClickTime = 0;
                                    JumpToSource(frame);
                                }
                                else
                                {
                                    LastFrameClickTime = EditorApplication.timeSinceStartup;
                                }
                            }
                        }
                        else
                        {
                            SelectedCallstackFrame = c1;
                            LastFrameClickTime = EditorApplication.timeSinceStartup;
                        }
                    }

                    lineY += lineHeight;
                    //Show the source code if needed
                    if (ShowFrameSource && c1 == SelectedCallstackFrame)
                    {
                        GUI.backgroundColor = Color.white;

                        var sourceContent = GetFrameSourceGUIContent(frame);
                        if (sourceContent != null)
                        {
                            var sourceSize = sourceStyle.CalcSize(sourceContent);
                            var sourceRect = new Rect(0, lineY, contentRect.width, sourceSize.y);

                            GUI.Label(sourceRect, sourceContent, sourceStyle);
                            lineY += sourceSize.y;
                        }
                    }
                }
            }

            GUI.EndScrollView();
        }

        GUI.backgroundColor = oldColor;
    }

    private Texture2D GetIconForLog(LogInfo log)
    {
        if (log.Severity == LogSeverity.Error)
        {
            return ErrorIcon;
        }

        if (log.Severity == LogSeverity.Warning)
        {
            return WarningIcon;
        }

        return MessageIcon;
    }

    private void ToggleShowSource(LogStackFrame frame)
    {
        ShowFrameSource = !ShowFrameSource;
    }

    private bool JumpToSource(LogStackFrame frame)
    {
        if (frame.FileName != null)
        {
            var osFileName = UberLogger.Logger.ConvertDirectorySeparatorsFromUnityToOS(frame.FileName);
            var filename = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), osFileName);
            if (System.IO.File.Exists(filename))
            {
                if (UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(filename, frame.LineNumber))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private GUIContent GetFrameSourceGUIContent(LogStackFrame frame)
    {
        var source = GetSourceForFrame(frame);
        if (!string.IsNullOrEmpty(source))
        {
            var content = new GUIContent(source);
            return content;
        }

        return null;
    }


    private void DrawFilter()
    {
        Vector2 size;
        LabelClamped("Filter Regex", GUI.skin.label, out size);
        DrawPos.x += size.x;

        string filterRegex = null;
        var clearFilter = false;
        if (ButtonClamped("Clear", GUI.skin.button, out size))
        {
            clearFilter = true;

            GUIUtility.keyboardControl = 0;
            GUIUtility.hotControl = 0;
        }

        DrawPos.x += size.x;

        var drawRect = new Rect(DrawPos, new Vector2(position.width - DrawPos.x, size.y));
        filterRegex = EditorGUI.TextArea(drawRect, FilterRegex);

        if (clearFilter)
        {
            filterRegex = null;
        }

        //If the filter has changed, invalidate our currently selected message
        if (filterRegex != FilterRegex)
        {
            ClearSelectedMessage();
            FilterRegex = filterRegex;
            MakeDirty = true;
        }

        DrawPos.y += size.y;
        DrawPos.x = 0;
    }

    private List<string> GetChannels()
    {
        if (Dirty)
        {
            CurrentChannels = EditorLogger.CopyChannels();
        }

        var categories = CurrentChannels;

        var channelList = new List<string>();
        channelList.Add("All");
        channelList.Add("No Channel");
        channelList.AddRange(categories);
        return channelList;
    }

    /// <summary>
    ///   Handles the split window stuff, somewhat bodgily
    /// </summary>
    private void ResizeTopPane()
    {
        //Set up the resize collision rect
        CursorChangeRect = new Rect(0, CurrentTopPaneHeight, position.width, DividerHeight);

        var oldColor = GUI.color;
        GUI.color = SizerLineColour;
        GUI.DrawTexture(CursorChangeRect, EditorGUIUtility.whiteTexture);
        GUI.color = oldColor;
        EditorGUIUtility.AddCursorRect(CursorChangeRect, MouseCursor.ResizeVertical);

        if (Event.current.type == EventType.MouseDown && CursorChangeRect.Contains(Event.current.mousePosition))
        {
            Resize = true;
        }

        //If we've resized, store the new size and force a repaint
        if (Resize)
        {
            CurrentTopPaneHeight = Event.current.mousePosition.y;
            CursorChangeRect.Set(CursorChangeRect.x, CurrentTopPaneHeight, CursorChangeRect.width, CursorChangeRect.height);
            Repaint();
        }

        if (Event.current.type == EventType.MouseUp)
        {
            Resize = false;
        }

        CurrentTopPaneHeight = Mathf.Clamp(CurrentTopPaneHeight, 100, position.height - 100);
    }

    //Cache for GetSourceForFrame
    private string SourceLines;
    private LogStackFrame SourceLinesFrame;

    /// <summary>
    ///If the frame has a valid filename, get the source string for the code around the frame
    ///This is cached, so we don't keep getting it.
    /// </summary>
    private string GetSourceForFrame(LogStackFrame frame)
    {
        if (SourceLinesFrame == frame)
        {
            return SourceLines;
        }


        if (frame.FileName == null)
        {
            return "";
        }

        var osFileName = UberLogger.Logger.ConvertDirectorySeparatorsFromUnityToOS(frame.FileName);
        var filename = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), osFileName);
        if (!System.IO.File.Exists(filename))
        {
            return "";
        }

        var lineNumber = frame.LineNumber - 1;
        var linesAround = 3;
        var lines = System.IO.File.ReadAllLines(filename);
        var firstLine = Mathf.Max(lineNumber - linesAround, 0);
        var lastLine = Mathf.Min(lineNumber + linesAround + 1, lines.Count());

        SourceLines = "";
        if (firstLine != 0)
        {
            SourceLines += "...\n";
        }

        for (var c1 = firstLine; c1 < lastLine; c1++)
        {
            var str = lines[c1] + "\n";
            if (c1 == lineNumber)
            {
                str = "<color=#ff0000ff>" + str + "</color>";
            }

            SourceLines += str;
        }

        if (lastLine != lines.Count())
        {
            SourceLines += "...\n";
        }

        SourceLinesFrame = frame;
        return SourceLines;
    }

    private void ClearSelectedMessage()
    {
        SelectedRenderLog = -1;
        SelectedCallstackFrame = -1;
        ShowFrameSource = false;
    }

    private Vector2 LogListScrollPosition;
    private Vector2 LogDetailsScrollPosition;

    private Texture2D ErrorIcon;
    private Texture2D WarningIcon;
    private Texture2D MessageIcon;
    private Texture2D SmallErrorIcon;
    private Texture2D SmallWarningIcon;
    private Texture2D SmallMessageIcon;

    private bool ShowChannels = true;
    private bool ShowTimes = true;
    private bool Collapse = false;
    private bool ScrollFollowMessages = false;
    private float CurrentTopPaneHeight = 200;
    private bool Resize = false;
    private Rect CursorChangeRect;
    private int SelectedRenderLog = -1;
    private bool Dirty = false;
    private bool MakeDirty = false;
    private float DividerHeight = 5;

    private double LastMessageClickTime = 0;
    private double LastFrameClickTime = 0;

    private const double DoubleClickInterval = 0.3f;

    //Serialise the logger field so that Unity doesn't forget about the logger when you hit Play
    [SerializeField]
    private UberLoggerEditor EditorLogger;

    private List<LogInfo> CurrentLogList = new();
    private HashSet<string> CurrentChannels = new();

    //Standard unity pro colours
    private Color SizerLineColour;

    private GUIStyle EntryStyleBackEven;
    private GUIStyle EntryStyleBackOdd;
    private string CurrentChannel = null;
    private string FilterRegex = null;
    private bool ShowErrors = true;
    private bool ShowWarnings = true;
    private bool ShowMessages = true;
    private int SelectedCallstackFrame = 0;
    private bool ShowFrameSource = false;

    private class CountedLog
    {
        public LogInfo Log = null;
        public int Count = 1;

        public CountedLog(LogInfo log, int count)
        {
            Log = log;
            Count = count;
        }
    }

    private List<CountedLog> RenderLogs = new();
    private float LogListMaxWidth = 0;
    private float LogListLineHeight = 0;
    private float CollapseBadgeMaxWidth = 0;
}
