using CSScriptingLang.Utils;

namespace Engine.Engine.Logging;

public static class Log
{
    private static Logger GlobalLogger = Logs.Get("Global");

    public static void Debug(string text)                       => GlobalLogger.Debug(text, Caller.GetFromFrame());
    public static void Debug(string text, params object[] args) => GlobalLogger.Debug(text, Caller.GetFromFrame(), args);

    public static void Info(string text)                       => GlobalLogger.Info(text, Caller.GetFromFrame());
    public static void Info(string text, params object[] args) => GlobalLogger.Info(text, Caller.GetFromFrame(), args);

    public static void Warning(string text)                       => GlobalLogger.Warning(text, Caller.GetFromFrame());
    public static void Warning(string text, params object[] args) => GlobalLogger.Warning(text, Caller.GetFromFrame(), args);

    public static void Error(string text)                       => GlobalLogger.Error(text, Caller.GetFromFrame());
    public static void Error(string text, params object[] args) => GlobalLogger.Error(text, Caller.GetFromFrame(), args);

    public static void Fatal(string text)                       => GlobalLogger.Fatal(text, Caller.GetFromFrame());
    public static void Fatal(string text, params object[] args) => GlobalLogger.Fatal(text, Caller.GetFromFrame(), args);
}