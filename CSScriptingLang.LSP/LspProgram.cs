using System.Diagnostics;
using System.Text;
using CSScriptingLang.Core;
using CSScriptingLang.Core.Diagnostics;
using CSScriptingLang.Core.Logging;
using CSScriptingLang.Interpreter;
using CSScriptingLang.LSP.Logs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.General;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Server;
using Serilog;
using Serilog.Events;
using Diagnostic = CSScriptingLang.Core.Diagnostics.Diagnostic;
using Log = Serilog.Log;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;


namespace CSScriptingLang.LSP;

public class LSPLogWriter : LogWriter_Syntax
{
    public override bool CanWrite(ref LogMessage msg) {
        return true;
    }
    public override void Output(StringBuilder output, LogMessage message) {

        Log.Logger.Write(message.Severity switch {
            CSScriptingLang.Core.Logging.LogLevel.All     => LogEventLevel.Verbose,
            CSScriptingLang.Core.Logging.LogLevel.Debug   => LogEventLevel.Debug,
            CSScriptingLang.Core.Logging.LogLevel.Info    => LogEventLevel.Information,
            CSScriptingLang.Core.Logging.LogLevel.Warning => LogEventLevel.Warning,
            CSScriptingLang.Core.Logging.LogLevel.Error   => LogEventLevel.Error,
            CSScriptingLang.Core.Logging.LogLevel.Fatal   => LogEventLevel.Fatal,
            _                                             => LogEventLevel.Verbose,
        }, output.ToString());
    }
}

public class LSPDiagnosticConsumer : DiagnosticConsumer
{

    public override bool Consume(Diagnostic diagnostic) {
        var diagnosticParams = new PublishDiagnosticsParams() {
            Uri         = diagnostic.File,
            Diagnostics = Container<LSPDiagnostic>.From(diagnostic),
            Version     = diagnostic.Range?.Script?.File?.Version,
        };

        LSPContext.Server.TextDocument.PublishDiagnostics(diagnosticParams);

        Core.Logging.Logs.Get<LSPDiagnosticConsumer>().Error($"Diagnostic:{diagnostic}");

        return true;
    }

}

public static class LSPContext
{
    public static LanguageServer     Server          { get; set; } = null!;
    public static IServiceCollection Services        { get; set; } = new ServiceCollection();
    public static IServiceProvider   ServiceProvider { get; set; }

    public static IServiceProvider BuildServiceProvider() {
        ServiceProvider = Services.BuildServiceProvider();
        return ServiceProvider;
    }

}

internal class Program
{
    private static async Task Main(string[] args) {
        await MainAsync(args).ConfigureAwait(false);
    }

    // public static LogLevel       LogLevel      { get; private set; } = LogLevel.Trace;
    // public static LogEventLevel  LogEventLevel { get; private set; } = LogEventLevel.Verbose;
    public static LogLevel      LogLevel      { get; private set; } = LogLevel.Information;
    public static LogEventLevel LogEventLevel { get; private set; } = LogEventLevel.Information;

    private static async Task MainAsync(string[] args) {

        // Environment.Exit(0);
        InterpreterConfig.Mode     = InterpreterMode.Lsp;
        InterpreterConfig.ExecMode = InterpreterExecMode.IncrementalSyntaxTree;


        Log.Logger = new LoggerConfiguration()
           .Enrich.FromLogContext()
           .WriteTo.File("log.txt")
           .WriteTo.Console(new Serilog.Formatting.Json.JsonFormatter())
           .WriteTo.LspSink(new Serilog.Formatting.Json.JsonFormatter())
           .MinimumLevel.Is(LogEventLevel)
            // .MinimumLevel.Verbose()
           .CreateLogger();

        Core.Logging.Logs.ClearGlobalWriters();
        Core.Logging.Logs.AddGlobalWriter(new LSPLogWriter());
        DiagnosticManager.AddConsumer(new LSPDiagnosticConsumer());

        LSPContext.Services.AddSingleton<IncrementalInterpreterFileSystem>();
        LSPContext.Services.AddSingleton<Interpreter.Interpreter>();

        // Debugger.Launch();
        // while (!Debugger.IsAttached) {
        // Log.Logger.Information("Waiting for debugger to attach...");
        // await Task.Delay(100);
        // }

        var options = new LanguageServerOptions();

        options
           .WithInput(Console.OpenStandardInput())
           .WithOutput(Console.OpenStandardOutput())
           .ConfigureLogging(x => x.AddSerilog(Log.Logger).SetMinimumLevel(LogLevel))
           .WithContentModifiedSupport(true)
           .WithHandler<TextDocumentHandler>()
            //.WithHandler<DidChangeWatchedFilesHandler>()
           .WithHandler<FoldingRangeHandler>()
           .WithHandler<MyWorkspaceSymbolsHandler>()
           .WithHandler<DocumentSymbolHandler>()
            // .WithHandler<SemanticTokensHandler>()
           .WithHandler<SemanticTokensFullHandler>()
           .WithHandler<ReferenceHandler>()
           .WithHandler<DefinitionHandler>()
           .WithHandler<DocumentDiagnosticHandler>()
           .WithServices(x => x.AddLogging(b => b.SetMinimumLevel(LogLevel)))
            // .WithServices(services => {
            // services.AddSingleton<IncrementalInterpreterFileSystem>();
            // services.AddSingleton<Interpreter.Interpreter>();
            // })
           .OnInitialize((server, request, token) => {
                var logger = server.Services.GetService<ILogger<ILanguageServer>>();
                logger.LogDebug("Initializing Interpreter & File System");

                var interpreter = server.Services.GetRequiredService<Interpreter.Interpreter>();

                InterpreterConfig.ExecutionPath = request.RootPath;

                var fs = server.Services.GetRequiredService<IncrementalInterpreterFileSystem>();
                fs.Initialize(request.RootPath, true, false);

                interpreter.Configure(fs);


                return Task.CompletedTask;
            })
           .OnInitialized((server, request, response, token) => {

                server.Register(registry => {
                    // registry.OnShutdown(@params => {
                    // Server.ForcefulShutdown();
                    // });
                    registry.OnExit(@params => {
                        LSPContext.Server.ForcefulShutdown();
                    });
                });

                if (response.Capabilities.SemanticTokensProvider == null) {
                    response.Capabilities.SemanticTokensProvider = new();
                }
                response.Capabilities.SemanticTokensProvider.Full = true;
                response.Capabilities.SemanticTokensProvider.Legend = new SemanticTokensLegend {
                    TokenModifiers = request.Capabilities?.TextDocument?.SemanticTokens.Value?.TokenModifiers ?? new(),
                    TokenTypes     = request.Capabilities?.TextDocument?.SemanticTokens.Value?.TokenTypes ?? new(),
                };

                if (response.Capabilities.DiagnosticProvider == null)
                    response.Capabilities.DiagnosticProvider = new();

                return Task.CompletedTask;
            })
           .OnStarted((server, token) => {
                var logger = server.Services.GetService<ILogger<ILanguageServer>>();

                // using var manager = await server.WorkDoneManager.Create(new WorkDoneProgressBegin {Title = "Doing some work..."})
                // .ConfigureAwait(false);
                // manager.OnNext(new WorkDoneProgressReport {Message = "doing things..."});

                /*
                var configuration = await server.Configuration.GetConfiguration(
                    new ConfigurationItem {
                        Section = "typescript",
                    }, new ConfigurationItem {
                        Section = "terminal",
                    }
                ).ConfigureAwait(false);

                var baseConfig = new JObject();
                foreach (var config in server.Configuration.AsEnumerable()) {
                    baseConfig.Add(config.Key, config.Value);
                }

                // logger.LogInformation("Base Config: {@Config}", baseConfig);

                var scopedConfig = new JObject();
                foreach (var config in configuration.AsEnumerable()) {
                    scopedConfig.Add(config.Key, config.Value);
                }
                */

                // logger.LogInformation("Scoped Config: {@Config}", scopedConfig);
                return Task.CompletedTask;
            });

        LSPContext.Server = LanguageServer.Create(options, LSPContext.BuildServiceProvider());


        await LSPContext.Server.Initialize(CancellationToken.None).ConfigureAwait(false);
        await LSPContext.Server.WaitForExit.ConfigureAwait(false);
    }
}