using System.Diagnostics;
using System.Runtime.CompilerServices;
using CSScriptingLang.Core.Logging;


namespace CSScriptingLang.Utils;

public static class AsyncLogWriter
{
    public struct QueuedLogString
    {
        public string       Message;
        public ConsoleColor Color;
    }

    public static List<QueuedLogString> QueuedLogs = new();

    public static Task   WriteDebouncedTask;
    public static Action WriteDebounced;
    static AsyncLogWriter() {
        WriteDebounced = Debounce(DebounceWrite);
    }

    public static void Log(string message, ConsoleColor color = ConsoleColor.White) {
        QueuedLogs.Add(new QueuedLogString {Message = message, Color = color});
        WriteDebounced();
    }
    public static void LogInstant(string message, ConsoleColor color = ConsoleColor.White) {
        Console.ForegroundColor = color;
        Console.Write(message);
    }

    public static Action Debounce(this Action func, int milliseconds = 100) {
        var last = 0;
        return () => {
            var current = Interlocked.Increment(ref last);
            WriteDebouncedTask = Task.Delay(milliseconds).ContinueWith(task => {
                if (current == last)
                    func();
                task.Dispose();
            });
        };
    }

    public static void DebounceWrite() {
        if (QueuedLogs.Count == 0)
            return;

        foreach (var log in QueuedLogs) {
            Console.ForegroundColor = log.Color;
            Console.Write(log.Message);
        }

        Console.ResetColor();
        QueuedLogs.Clear();
    }
}

public struct ScopeTimer : IDisposable
{
    private static Logger Logger = Logs.Get<ScopeTimer>().SetName("TIMER");

    private readonly Stopwatch _stopwatch;
    private readonly string    _scopeName;

    public ScopeTimer([CallerMemberName] string scopeName = "") {
        _scopeName = scopeName;
        _stopwatch = ObjectPool<Stopwatch>.Rent();
        _stopwatch.Start();
    }

    public static ScopeTimer NewWith(string scopeName) {
        return new ScopeTimer(scopeName);
    }
    public static ScopeTimer New([CallerMemberName] string scopeName = "") {
        return new ScopeTimer(scopeName);
    }
    public static ScopeTimer NewPrefixed(string prefix, [CallerMemberName] string scopeName = "") {
        return new ScopeTimer($"{prefix} -> {scopeName}");
    }

    public void Start() => _stopwatch.Restart();
    public void Stop() {

        _stopwatch.Stop();

        var elapsed = _stopwatch.Elapsed;

        /*var els = new ElementCollection(new Span($"[{_scopeName}] -> ")) {
            elapsed.ToColoredTimeString()
        };
        Logger.Debug(els);*/

        Logger.Debug($"[{_scopeName}] -> {elapsed.ToColoredTimeString()}");

        _stopwatch.Reset();
        ObjectPool<Stopwatch>.Return(_stopwatch);
    }

    public void Dispose() => Stop();
}

public struct ClassScopedTimer<T>
{
    public static Logger Logger = Logs.Get($"TIMER<{typeof(T).Name}>");

    public static void SetColorFn(Func<string, string> fn) {
        Logger.ColorName = fn;
    }

    public static void SetName(string name) {
        Logger.SetName(name);
    }

    public static ScopedTimer<T> NewWith(string scopeName) => ScopedTimer<T>.NewWith(scopeName);

    public static ScopedTimer<T> New([CallerMemberName] string scopeName = "") => ScopedTimer<T>.New(scopeName);

    public static ScopedTimer<T> NewPrefixed(string prefix, [CallerMemberName] string scopeName = "") => ScopedTimer<T>.NewPrefixed(prefix, scopeName);
}

public class ClassScopedTimerInst
{
    public Logger Logger = Logs.Get($"TIMER");

    public ClassScopedTimerInst(Logger inLogger = null, string name = null) {
        if (name != null)
            Logger.SetName(name);

        if (inLogger != null) {
            Logger.SetLogLevel(inLogger.GetLogLevel());
            Logger.ColorName = inLogger.ColorName;
            Logger.SetName($"TIMER<{inLogger.GetName()}>");
        }
    }

    public ClassScopedTimerInst SetColorFn(Func<string, string> fn) {
        Logger.ColorName = fn;
        return this;
    }

    public ClassScopedTimerInst SetName(string name) {
        Logger.SetName(name);
        return this;
    }

    public static ClassScopedTimerInst Create(Logger inLogger) => new(inLogger);


    public ScopedTimer NewWith(string scopeName) => ScopedTimer.NewWith(Logger, scopeName);

    public ScopedTimer New([CallerMemberName] string scopeName = "") => ScopedTimer.New(Logger, scopeName);

    public ScopedTimer NewPrefixed(string prefix, [CallerMemberName] string scopeName = "") => ScopedTimer.NewPrefixed(Logger, prefix, scopeName);
}

public class ClassScopedTimerInst<T>
{
    public static Logger Logger = null;

    public ClassScopedTimerInst(Logger inLogger = null) {
        if (Logger == null)
            Logger = Logs.Get($"TIMER<{typeof(T).Name}>");

        if (inLogger != null) {
            Logger.SetLogLevel(inLogger.GetLogLevel());
            Logger.ColorName = inLogger.ColorName;
            Logger.SetName($"TIMER<{inLogger.GetName()}>");
        }
    }

    public ClassScopedTimerInst<T> SetColorFn(Func<string, string> fn) {
        Logger.ColorName = fn;
        return this;
    }

    public ClassScopedTimerInst<T> SetName(string name) {
        Logger.SetName(name);
        return this;
    }

    public static ClassScopedTimerInst<T> Create(Logger inLogger) => new(inLogger);

    public ScopedTimer NewWith(string scopeName) => ScopedTimer.NewWith(Logger, scopeName);

    public ScopedTimer New([CallerMemberName] string scopeName = "") => ScopedTimer.New(Logger, scopeName);

    public ScopedTimer NewPrefixed(string prefix, [CallerMemberName] string scopeName = "") => ScopedTimer.NewPrefixed(Logger, prefix, scopeName);
}

public struct ScopedTimer : IDisposable
{
    private readonly Stopwatch _stopwatch;
    private readonly string    _scopeName;

    private Logger Logger;

    public ScopedTimer(Logger logger, [CallerMemberName] string scopeName = "") {
        Logger = logger;
        _scopeName = scopeName;
        _stopwatch = ObjectPool<Stopwatch>.Rent();
        _stopwatch.Start();
    }

    public static ScopedTimer NewWith(Logger logger, string scopeName) {
        return new ScopedTimer(logger, scopeName);
    }
    public static ScopedTimer New(Logger logger, [CallerMemberName] string scopeName = "") {
        return new ScopedTimer(logger, scopeName);
    }
    public static ScopedTimer NewPrefixed(Logger logger, string prefix, [CallerMemberName] string scopeName = "") {
        return new ScopedTimer(logger, $"{prefix} -> {scopeName}");
    }

    public void Start() => _stopwatch.Restart();
    public void Stop() {

        _stopwatch.Stop();

        var elapsed = _stopwatch.Elapsed;

        Logger.Debug($"[{_scopeName}] -> {elapsed.ToColoredTimeString()}");

        _stopwatch.Reset();
        ObjectPool<Stopwatch>.Return(_stopwatch);
    }

    public void Dispose() => Stop();
}

public struct ScopedTimer<T> : IDisposable
{
    private readonly Stopwatch _stopwatch;
    private readonly string    _scopeName;

    public ScopedTimer([CallerMemberName] string scopeName = "") {
        _scopeName = scopeName;
        _stopwatch = ObjectPool<Stopwatch>.Rent();
        _stopwatch.Start();
    }

    public static ScopedTimer<T> NewWith(string scopeName) {
        return new ScopedTimer<T>(scopeName);
    }
    public static ScopedTimer<T> New([CallerMemberName] string scopeName = "") {
        return new ScopedTimer<T>(scopeName);
    }
    public static ScopedTimer<T> NewPrefixed(string prefix, [CallerMemberName] string scopeName = "") {
        return new ScopedTimer<T>($"{prefix} -> {scopeName}");
    }

    public void Start() => _stopwatch.Restart();
    public void Stop() {

        _stopwatch.Stop();

        var elapsed = _stopwatch.Elapsed;

        ClassScopedTimer<T>.Logger.Debug($"[{_scopeName}] -> {elapsed.ToColoredTimeString()}");

        _stopwatch.Reset();
        ObjectPool<Stopwatch>.Return(_stopwatch);
    }

    public void Dispose() => Stop();
}