
using CSScriptingLang.Utils;

namespace CSScriptingLang.Core.Logging;

public struct LogMessage : IEquatable<LogMessage>
{
    public Caller       Caller;
    public Logger       Logger;
    public LogLevel     Severity;
    public string       Message;
    public List<object> Args = new();
    public DateTime     Timestamp;

    public List<object> Context = new();

    public static LogMessage Empty = new();

    public bool IsEmpty => Logger == null;

    public Logger Consumer { get; set; }
    public bool   Consumed => Consumer != null;

    public void Consume(Logger consumer = null) {
        consumer ??= Consumer ?? Logger;
        if (consumer != null) {
            Consumer = consumer;
        }
    }

    public LogMessage(Logger logger, Caller caller) : this() {
        Logger    = logger;
        Caller    = caller;
        Timestamp = DateTime.Now;
    }

    public LogMessage(Logger logger, Caller caller, LogLevel severity, string message) : this(logger, caller) {
        Severity  = severity;
        Message   = message;
        Timestamp = DateTime.Now;
    }

    public LogMessage WithSeverity(LogLevel severity) {
        Severity = severity;
        return this;
    }

    public LogMessage WithMessage(string message) {
        Message = message;
        return this;
    }

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

    public LogMessage WithContext(object context) {
        Context.Add(context);
        return this;
    }
    public bool HasContext<T>() {
        return Context.Any(x => x is T);
    }
    public T GetContext<T>() {
        return Context.OfType<T>().FirstOrDefault();
    }
    public bool GetContext<T>(out T context) {
        context = Context.OfType<T>().FirstOrDefault();
        return context != null;
    }
    
    public void Log() {
        Logger.Write(this);
    }
}