using CSScriptingLang.Utils;

namespace Engine.Engine.Logging;

public class Logs
{
    private static readonly Dictionary<string, Logger> loggers = new();

    private static readonly List<LogWriter> GlobalWriters = [new LogWriter()];

    public static readonly Logger Global = Get(nameof(Global));

    public static IEnumerable<LogWriter> GetGlobalWriters() => GlobalWriters;

    public static void AddGlobalWriter(LogWriter    writer) => GlobalWriters.Add(writer);
    public static bool RemoveGlobalWriter(LogWriter writer) => GlobalWriters.Remove(writer);
    public static void ClearGlobalWriters()                 => GlobalWriters.Clear();

    public static Logger Get(string n, LogLevel logLevel = LogLevel.Debug)
        => loggers.GetOrAdd(n, () => new Logger(n, null, logLevel));

    public static Logger Get<T>(LogLevel logLevel = LogLevel.Debug)
        => loggers.GetOrAdd(typeof(T).Name, () => new Logger(typeof(T).Name, typeof(T), logLevel));

    public static bool DestroyLogger(string name) => loggers.Remove(name);
    public static void DestroyAll()               => loggers.Clear();
}