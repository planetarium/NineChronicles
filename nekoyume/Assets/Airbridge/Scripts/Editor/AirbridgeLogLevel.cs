public class AirbridgeLogLevel
{ 
    /**
     * +------------------+---------------+---------------+
     * | Unity Log Level  | Andoid Level  | iOS Level     |
     * +------------------+---------------+---------------+
     * | Fault            | Assert        | Crash         |
     * | Error            | Error         | Critical      |
     * | Warning          | Warn          | Warning       |
     * | Info             | Info          | Info          |
     * | Debug            | Verbose       | Debug         |
     * +------------------+---------------+---------------+
     */
    public static readonly string[] LogLevel = { "Debug", "Info", "Warning", "Error", "Fault" };
    public static readonly int Default = 2;   // Default is "Warning"
    
    
    public static string GetAndroidLogLevel(int index)
    {
        return index switch
        {
            // Unity Log Level: Debug   [0]
            // Andoid Level:    VERBOSE [2]
            0 => "2",
            // Unity Log Level: Info    [1]
            // Andoid Level:    INFO    [4]
            1 => "4",
            // Unity Log Level: Warning [2]
            // Andoid Level:    WARN    [5]
            2 => "5",
            // Unity Log Level: Error   [3]
            // Andoid Level:    ERROR   [6]
            3 => "6",
            // Unity Log Level: Fault   [4]
            // Andoid Level:    Assert  [7]
            _ => "7"
        };
    }
    
    public static string GetIOSLogLevel(int index)
    {
        return index switch
        {
            // Unity Log Level: Debug           [0]
            // iOS Level:       AB_LOG_DEBUG    [5]
            0 => "5",
            // Unity Log Level: Info            [1]
            // iOS Level:       AB_LOG_INFO     [4]
            1 => "4",
            // Unity Log Level: Warning         [2]
            // iOS Level:       AB_LOG_WARNING  [3]
            2 => "3",
            // Unity Log Level: Error           [3]
            // iOS Level:       AB_LOG_CRITICAL [2]
            3 => "2",
            // Unity Log Level: Fault           [4]
            // iOS Level:       AB_LOG_CRASH    [1]
            _ => "1"
        };
    }
}