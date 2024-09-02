using Alba.CsConsoleFormat;
using CSScriptingLang.Utils;

namespace Engine.Engine.Logging;

public struct LogMessage : IEquatable<LogMessage>
{
    public Caller       Caller;
    public Logger       Logger;
    public LogLevel     Severity;
    public string       Message;
    public List<object> Args = new();
    public DateTime     Timestamp;

    public LogMessage(Logger logger, Caller caller, LogLevel severity, string source, string message) : this() {
        Logger    = logger;
        Caller    = caller;
        Severity  = severity;
        Message   = new("[" + source + "]: " + message);
        Timestamp = DateTime.Now;
    }

    public LogMessage(Logger logger, Caller caller, LogLevel severity, string message) : this() {
        Logger    = logger;
        Caller    = caller;
        Severity  = severity;
        Message   = message;
        Timestamp = DateTime.Now;
    }
    /*
    public LogMessage(Logger logger, Caller caller, LogLevel severity, Span message) : this() {
        Logger    = logger;
        Caller    = caller;
        Severity  = severity;
        Message   = message;
        Timestamp = DateTime.Now;
    }
    public LogMessage(Logger logger, Caller caller, LogLevel severity, ElementCollection message) : this() {
        Logger    = logger;
        Caller    = caller;
        Severity  = severity;
        ElCollection = message;
        Timestamp = DateTime.Now;
    }
    public ElementCollection ElCollection { get; set; }
*/

    public LogMessage WithArg(object arg) {
        Args.Add(arg);
        return this;
    }

    public LogMessage WithArgs(params object[] args) {
        Args.AddRange(args);
        return this;
    }

    public readonly override bool Equals(object obj) {
        return obj is LogMessage other && Equals(other);
    }

    public readonly bool Equals(LogMessage other) {
        return EqualityComparer<Logger>.Default.Equals(Logger, other.Logger) && Severity == other.Severity && Message == other.Message && Timestamp == other.Timestamp;
    }

    public readonly override int GetHashCode() {
        return HashCode.Combine<Logger, LogLevel, string, DateTime>(Logger, Severity, Message, Timestamp);
    }

    public static bool operator ==(LogMessage left, LogMessage right) => left.Equals(right);
    public static bool operator !=(LogMessage left, LogMessage right) => !(left == right);
}