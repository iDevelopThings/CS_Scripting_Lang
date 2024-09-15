using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Alba.CsConsoleFormat;
using CSScriptingLang.Lexing;
using CSScriptingLang.Utils;

namespace Engine.Engine.Logging;

[Flags]
public enum LoggerFlags
{
    None        = 0,
    NoTimestamp = 1 << 1,
    NoSeverity  = 1 << 2,
    NoName      = 1 << 3,
    NoCaller    = 1 << 4,

    All = NoTimestamp | NoSeverity | NoName | NoCaller
}

[DebuggerDisplay("{Name} - {LogLevel}")]
public class Logger
{
    public  Type        Type     { get; set; }
    private string      Name     { get; set; }
    private LogLevel    LogLevel { get; set; }
    public  LoggerFlags Flags    { get; set; } = LoggerFlags.None;

    public Func<string, string> ColorName { get; set; } = s => s.BrightGray();

    public Logger(string name, Type type, LogLevel logLevel = LogLevel.Debug) {
        Name     = name;
        Type     = type;
        LogLevel = logLevel;
    }

    public string GetColoredName() => ColorName(Name);
    public string GetName()        => Name;
    public Logger SetName(string name) {
        Name = name;
        return this;
    }

    public Logger SetLogLevel(LogLevel logLevel) {
        LogLevel = logLevel;
        return this;
    }
    public LogLevel GetLogLevel() => LogLevel;

    public Logger SetLogFlags(LoggerFlags flags) {
        Flags = flags;
        return this;
    }
    public Logger AddLogFlags(LoggerFlags flags) {
        Flags |= flags;
        return this;
    }
    public Logger RemoveLogFlags(LoggerFlags flags) {
        Flags &= ~flags;
        return this;
    }
    public bool HasLogFlag(LoggerFlags flags) => Flags.HasFlag(flags);

    public void Write(LogMessage message) {
        if (!CanLogMessage(message.Severity, message.Caller))
            return;

        var didWrite = false;

        foreach (var globalWriter in Logs.GetGlobalWriters()) {
            if (!globalWriter.CanWrite(ref message))
                continue;

            globalWriter.Write(ref message);
            didWrite = true;

            if (message.Consumed) {
                break;
            }

            /*
            if (message.Severity == LogLevel.Fatal) {
                throw new CompilationException(output.ToString());
            }

            Console.WriteLine(output.ToString());
            */
        }

        if (!didWrite) {
            throw new Exception("Logger message was not written");
        }

    }

    public LogMessage Create([CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string method = "") {
        var msg = new LogMessage(this, Caller.FromAttributes(file, line, method), LogLevel, Name);
        return msg;
    }
    public LogMessage Create(Caller caller) {
        var msg = new LogMessage(this, caller, LogLevel, Name);
        return msg;
    }

    private void ConstructAndLogMessage(LogLevel logLevel, Caller caller, string text, params object[] args) {
        var msg = Create(caller)
           .WithSeverity(logLevel)
           .WithMessage(text)
           .WithArgs(args);

        if (msg.IsEmpty)
            return;

        if (Logs.LogConsumer(msg))
            return;

        msg.Log();
    }

    private bool CanLogMessage(LogLevel level, Caller caller) {
        return level >= LogLevel && Logs.GetGlobalWriters().Any();
    }


    public void Debug(string text)                                               => ConstructAndLogMessage(LogLevel.Debug, Caller.GetFromFrame(), text);
    public void Debug(string text, Caller          caller)                       => ConstructAndLogMessage(LogLevel.Debug, caller, text);
    public void Debug(string text, params object[] args)                         => ConstructAndLogMessage(LogLevel.Debug, Caller.GetFromFrame(), string.Format(text, args));
    public void Debug(string text, Caller          caller, params object[] args) => ConstructAndLogMessage(LogLevel.Debug, caller, string.Format(text, args));

    public void Info(string text)                                               => ConstructAndLogMessage(LogLevel.Info, Caller.GetFromFrame(), text);
    public void Info(string text, Caller          caller)                       => ConstructAndLogMessage(LogLevel.Info, caller, text);
    public void Info(string text, params object[] args)                         => ConstructAndLogMessage(LogLevel.Info, Caller.GetFromFrame(), string.Format(text, args));
    public void Info(string text, Caller          caller, params object[] args) => ConstructAndLogMessage(LogLevel.Info, caller, string.Format(text, args));

    public void Warning(string text)                                               => ConstructAndLogMessage(LogLevel.Warning, Caller.GetFromFrame(), text);
    public void Warning(string text, Caller          caller)                       => ConstructAndLogMessage(LogLevel.Warning, caller, text);
    public void Warning(string text, params object[] args)                         => ConstructAndLogMessage(LogLevel.Warning, Caller.GetFromFrame(), string.Format(text, args));
    public void Warning(string text, Caller          caller, params object[] args) => ConstructAndLogMessage(LogLevel.Warning, caller, string.Format(text, args));

    public void Error(string text)                                               => ConstructAndLogMessage(LogLevel.Error, Caller.GetFromFrame(2), text);
    public void Error(string text, Caller          caller)                       => ConstructAndLogMessage(LogLevel.Error, caller, text);
    public void Error(string text, params object[] args)                         => ConstructAndLogMessage(LogLevel.Error, Caller.GetFromFrame(2), string.Format(text, args));
    public void Error(string text, Caller          caller, params object[] args) => ConstructAndLogMessage(LogLevel.Error, caller, string.Format(text, args));

    public void Fatal(string text)                                               => ConstructAndLogMessage(LogLevel.Fatal, Caller.GetFromFrame(2), text);
    public void Fatal(string text, Caller          caller)                       => ConstructAndLogMessage(LogLevel.Fatal, caller, text);
    public void Fatal(string text, params object[] args)                         => ConstructAndLogMessage(LogLevel.Fatal, Caller.GetFromFrame(2), string.Format(text, args));
    public void Fatal(string text, Caller          caller, params object[] args) => ConstructAndLogMessage(LogLevel.Fatal, caller, string.Format(text, args));

    public void Exception(Exception ex) {
        Caller caller;
        if (ex is BaseLanguageException ble) {
            caller = ble.Caller;
        } else {
            caller = Caller.FromException(ex);
        }
        
        if(!caller.IsValid())
            caller = Caller.FromException(ex);

        var isFatal = ex is FatalInterpreterException;
        Create(caller)
           .WithSeverity(isFatal ? LogLevel.Fatal : LogLevel.Error)
           .WithMessage(ex.Message)
           .WithContext(ex)

           .Log();
    }
}