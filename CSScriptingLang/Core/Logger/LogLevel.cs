
using CSScriptingLang.Utils;

namespace Engine.Engine.Logging;


public enum LogLevel
{
    /// <summary>
    /// Display all logs.
    /// </summary>
    All = 0,

    /// <summary>
    /// Debug logging, used for internal debugging, it should be disabled on release builds.
    /// </summary>
    Debug,

    /// <summary>
    /// Info logging, used for program execution info.
    /// </summary>
    Info,

    /// <summary>
    /// Warning logging, used on recoverable failures.
    /// </summary>
    Warning,

    /// <summary>
    /// Error logging, used on unrecoverable failures.
    /// </summary>
    Error,

    /// <summary>
    /// Fatal logging, used to abort program: exit(EXIT_FAILURE).
    /// </summary>
    Fatal,

    /// <summary>
    /// Disable logging.
    /// </summary>
    None
}

public static class LogLevelExtensions
{
    public static string ToString(this LogLevel logLevel) {
        return logLevel switch {
            LogLevel.Debug   => "DEBUG",
            LogLevel.Info    => "INFO",
            LogLevel.Warning => "WARNING",
            LogLevel.Error   => "ERROR",
            LogLevel.Fatal   => "FATAL",
            _                => "NONE"
        };
    }

    public static string ToColoredString(this LogLevel logLevel) {
        return logLevel switch {
            LogLevel.Debug   => "DEBUG".BrightGray(),
            LogLevel.Info    => "INFO".Cyan(),
            LogLevel.Warning => "WARNING".Yellow(),
            LogLevel.Error   => "ERROR".Red(),
            LogLevel.Fatal   => "FATAL".Red(),
            _                => "NONE".Gray(),
        };
    }

    /*
    public static Span ToSpan(this LogLevel logLevel) {
        return logLevel switch {
            LogLevel.Debug   => "DEBUG".DarkGray(),
            LogLevel.Info    => "INFO".Cyan(),
            LogLevel.Warning => "WARNING".Yellow(),
            LogLevel.Error   => "ERROR".Red(),
            LogLevel.Fatal   => "FATAL".Red(),
            _                => "NONE".Gray(),
        };
    }*/
}