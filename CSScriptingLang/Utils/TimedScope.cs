using System.Diagnostics;
using System.Runtime.CompilerServices;
using Alba.CsConsoleFormat;
using Engine.Engine.Logging;


namespace CSScriptingLang.Utils;

public struct TimedScope : IDisposable, IEquatable<TimedScope>
{
    private readonly Stopwatch _stopwatch;

    public TimeSpan Elapsed { get; set; }
    
    public static TimedScope Empty => new(false);

    public TimedScope(bool init = true) {
        if(init) {
            _stopwatch = SimpleObjectPool<Stopwatch>.Get();
            _stopwatch.Start();
        }
    }

    public void Start() => _stopwatch.Restart();

    public void Stop() {
        _stopwatch.Stop();

        Elapsed = _stopwatch.Elapsed;

        _stopwatch.Reset();
        SimpleObjectPool<Stopwatch>.Return(_stopwatch);
    }

    public string ElapsedString() => Elapsed.ToColoredTimeString();

    public void Print(Logger logger, string scopeName) => logger.Debug($"[{scopeName}] -> {ElapsedString()}");

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