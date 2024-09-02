using System.Diagnostics;
using System.Runtime.CompilerServices;
using Alba.CsConsoleFormat;
using Engine.Engine.Logging;


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