using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;

namespace CSScriptingLang.LSP.Logs;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Window;

public class LSPSink : ILogEventSink
{
    private readonly ITextFormatter _formatProvider;

    public LSPSink(
        ITextFormatter formatProvider
    ) {
        _formatProvider = formatProvider;
    }
    private bool TryGetMessageType(LogEventLevel logLevel, out MessageType messageType) {
        switch (logLevel) {
            case LogEventLevel.Fatal:
            case LogEventLevel.Error:
                messageType = MessageType.Error;
                return true;
            case LogEventLevel.Warning:
                messageType = MessageType.Warning;
                return true;
            case LogEventLevel.Information:
                messageType = MessageType.Info;
                return true;
            case LogEventLevel.Debug:
            case LogEventLevel.Verbose:
                // TODO: Integrate with set trace?
                messageType = MessageType.Log;
                return true;
        }

        messageType = MessageType.Log;
        return false;
    }

    public void Emit(LogEvent logEvent) {
        var sb = new StringWriter(new StringBuilder(256));
        _formatProvider.Format(logEvent, sb);
        var message = sb.ToString();

        // var message = logEvent.RenderMessage(_formatProvider);

        if (!TryGetMessageType(logEvent.Level, out var messageType)) {
            throw new ArgumentOutOfRangeException(nameof(logEvent.Level), logEvent.Level, "Invalid log level.");
        }


        var msgObj = new {
            Exception        = logEvent.Exception?.ToString(),
            Category         = logEvent.Properties.TryGetValue("SourceContext", out var category) ? category.ToString() : "",
            Timestamp        = logEvent.Timestamp.ToString("HH:mm:ss"),
            Message          = JsonConvert.DeserializeObject<Dictionary<string, object>>(message),
            FormattedMessage = logEvent.RenderMessage(),
        };
        var msgParams = new LogMessageParams {
            Type    = messageType,
            Message = JsonConvert.SerializeObject(msgObj),
        };

        if (LSPContext.Server == null) {
            Console.WriteLine("Server is null when trying to log message: " + msgParams.Message);
            return;
        }

        LSPContext.Server.Services.GetService<ILanguageServerFacade>().Window.Log(msgParams);
    }
}