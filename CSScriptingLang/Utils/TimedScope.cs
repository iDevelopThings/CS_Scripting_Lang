using System.Diagnostics;
using System.Runtime.CompilerServices;
using CSScriptingLang.Core.Logging;


namespace CSScriptingLang.Utils;

public struct TimedScope : IDisposable, IEquatable<TimedScope>
{
    private static Logger Logger = Logs.Get<TimedScope>();

    private readonly Stopwatch _stopwatch;

    public Dictionary<string, TimedScope> Scopes { get; set; }

    public bool     PrintOnDispose { get; set; }
    public TimeSpan Elapsed        { get; set; }
    public string   ScopeName      { get; set; }
    public Caller   Caller         { get; set; }
    public int      Idx = 0;

    public static TimedScope Empty => new(false);

    public TimedScope(bool init = true, string scopeName = "") {
        ScopeName = scopeName;

        if (!init)
            return;

        _stopwatch = SimpleObjectPool<Stopwatch>.Get();
        _stopwatch.Start();
    }
    private TimedScope(string scopeName, Caller callerAttrs, Dictionary<string, TimedScope> scopes) {
        ScopeName  = scopeName;
        Caller     = callerAttrs;
        Scopes     = scopes;
        _stopwatch = SimpleObjectPool<Stopwatch>.Get();
        _stopwatch.Start();
    }

    public static TimedScope Scoped([CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "") {
        var scope = new TimedScope(true, $"{file}:{line}::{member}");
        scope.Start();
        return scope;
    }
    public static TimedScope Scoped_PrintWithCaller([CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "") {
        var scope = Scoped(file, line, member);
        scope.PrintOnDispose = true;
        return scope;
    }
    public static TimedScope Scoped_Print(string scopeName, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "") {
        var scope = Scoped_PrintWithCaller(file, line, member);
        scope.ScopeName      = $"{scopeName} -> {scope.ScopeName}";
        scope.PrintOnDispose = true;
        return scope;
    }

    public static T Scoped_Fn<T>(string scopeName, Func<T> fn, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "") {
        var scope = Scoped_PrintWithCaller(file, line, member);
        scope.ScopeName = $"{scopeName} -> {scope.ScopeName}";
        var value = fn();
        scope.Dispose();
        scope.Print(Logger);
        return value;
    }

    public static TimedScope Scoped(
        Dictionary<string, TimedScope> scopes,
        string                         scopeName,
        [CallerFilePath]
        string file = "",
        [CallerLineNumber]
        int line = 0,
        [CallerMemberName]
        string member = ""
    ) {
        var scope = new TimedScope(scopeName, Caller.FromAttributes(file, line, member), scopes);
        scope.Idx = scopes.Count;

        var s = scopes.GetOrAdd(scopeName, () => scope);
        s.Reset();

        s.Start();

        return scope;
    }

    public void Start() => _stopwatch.Restart();
    public void Reset() => _stopwatch.Restart();

    public void Stop() {
        _stopwatch.Stop();

        Elapsed = _stopwatch.Elapsed;

        _stopwatch.Reset();
        SimpleObjectPool<Stopwatch>.Return(_stopwatch);

        if (Scopes != null) {
            Scopes[ScopeName] = this;
        }

        if (PrintOnDispose) {
            Print(Logger);
        }
    }

    public string ElapsedString() => Elapsed.ToColoredTimeString();

    public void Print(Logger logger) {
        logger.Debug($"[{ScopeName}] -> {ElapsedString()}");
    }
    public void Print(Logger logger, string scopeName) {
        logger.Debug($"[{scopeName}] -> {ElapsedString()}");
    }

    public void Dispose() {
        Stop();
    }

    public bool Equals(TimedScope other) {
        return Equals(_stopwatch, other._stopwatch) && Elapsed.Equals(other.Elapsed);
    }
    public override bool Equals(object obj) {
        return obj is TimedScope other && Equals(other);
    }
    public override int GetHashCode() {
        return HashCode.Combine(_stopwatch, Elapsed);
    }
}