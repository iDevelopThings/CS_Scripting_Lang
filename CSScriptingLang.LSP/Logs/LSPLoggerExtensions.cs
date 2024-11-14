using Serilog;
using Serilog.Configuration;
using Serilog.Formatting;

namespace CSScriptingLang.LSP.Logs;

public static class LSPLoggerExtensions
{
    
    public static LoggerConfiguration LspSink(
        this LoggerSinkConfiguration loggerConfiguration,
        ITextFormatter               formatProvider = null)
    {
        return loggerConfiguration.Sink(new LSPSink(formatProvider));
    }
}