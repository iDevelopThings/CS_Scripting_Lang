using System.Text;
using Alba.CsConsoleFormat;
using CSScriptingLang.Lexing;
using CSScriptingLang.Utils;

namespace Engine.Engine.Logging;

public class Logger
{
    private Type     Type     { get; set; }
    private string   Name     { get; set; }
    private LogLevel LogLevel { get; set; }
    
    public Func<string, string> ColorName { get; set; } = s => s.BrightGray();

    public Logger(string name, Type type, LogLevel logLevel = LogLevel.Debug) {
        Name     = name;
        Type     = type;
        LogLevel = logLevel;
    }

    public string GetColoredName() => ColorName(Name);
    public string GetName() => Name;
    public Logger SetName(string name) {
        Name = name;
        return this;
    }

    private LogMessage ConstructMessage(LogLevel logLevel, Caller caller, string text, params object[] args) {
        return new LogMessage(this, caller, logLevel, string.Format(text, args));
    }

    // private void ConstructAndLogMessage(LogLevel logLevel, Caller caller, string text, params object[] args)
        // => ConstructAndLogMessage(logLevel, caller, new Span(text), args);

    private void ConstructAndLogMessage(LogLevel logLevel, Caller caller, string text, params object[] args) {
        if (!CanLogMessage(logLevel, caller))
            return;

        var msg = new LogMessage(this, caller, logLevel, text)
           .WithArgs(args);
        
        foreach (var globalWriter in Logs.GetGlobalWriters()) {
            if(msg.Severity == LogLevel.Fatal) {
                var output = new StringBuilder();
                globalWriter.Write(output, msg);
                throw new CompilationException(output.ToString());
            } else {
                globalWriter.Write(msg);
            }
        }
    }
    /*
    private void ConstructAndLogMessage(LogLevel logLevel, Caller caller, Span text, params object[] args) {
        if (!CanLogMessage(LogLevel, caller))
            return;

        var msg = new LogMessage(this, caller, logLevel, text)
           .WithArgs(args);

        foreach (var globalWriter in Logs.GetGlobalWriters())
            globalWriter.Write(msg);
    }
    private void ConstructAndLogMessage(LogLevel logLevel, Caller caller, ElementCollection text, params object[] args) {
        if (!CanLogMessage(LogLevel, caller))
            return;

        var msg = new LogMessage(this, caller, logLevel, text)
           .WithArgs(args);

        foreach (var globalWriter in Logs.GetGlobalWriters())
            globalWriter.Write(msg);
    }
    */

    private bool CanLogMessage(LogLevel level, Caller caller) {
        return level >= LogLevel && Logs.GetGlobalWriters().Any();
    }

    public void Debug(string            text)                                               => ConstructAndLogMessage(LogLevel.Debug, Caller.GetFromFrame(), text);
    // public void Debug(Span              text)                                               => ConstructAndLogMessage(LogLevel.Debug, Caller.GetFromFrame(), text);
    // public void Debug(ElementCollection text)                                               => ConstructAndLogMessage(LogLevel.Debug, Caller.GetFromFrame(), text);
    public void Debug(string            text, Caller          caller)                       => ConstructAndLogMessage(LogLevel.Debug, caller, text);
    public void Debug(string            text, params object[] args)                         => ConstructAndLogMessage(LogLevel.Debug, Caller.GetFromFrame(), string.Format(text, args));
    public void Debug(string            text, Caller          caller, params object[] args) => ConstructAndLogMessage(LogLevel.Debug, caller, string.Format(text, args));

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
}