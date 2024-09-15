using System;
using System.Diagnostics;
using System.Threading.Tasks;
using CSScriptingLang.Core;
using CSScriptingLang.Core.FileSystem;
using CSScriptingLang.Interpreter;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Server;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;


namespace CSScriptingLang.LSP;

internal class Program
{
    private static async Task Main(string[] args) {
        await MainAsync(args).ConfigureAwait(false);
    }

    private static async Task MainAsync(string[] args) {


        Log.Logger = new LoggerConfiguration()
           .Enrich.FromLogContext()
           .WriteTo.File("log.txt")
           .WriteTo.Console()
           .MinimumLevel.Verbose()
           .CreateLogger();

        Log.Logger.Information("This only goes file...");

        Debugger.Launch();
        while (!Debugger.IsAttached) {
            Log.Logger.Information("Waiting for debugger to attach...");
            await Task.Delay(100);
        }

        IObserver<WorkDoneProgressReport> workDone = null!;

        var server = await LanguageServer.From(
            options =>
                options
                   .WithInput(Console.OpenStandardInput())
                   .WithOutput(Console.OpenStandardOutput())
                   .ConfigureLogging(
                        x => x
                           .AddSerilog(Log.Logger)
                           .AddLanguageProtocolLogging()
                           .SetMinimumLevel(LogLevel.Trace)
                    )
                   .WithHandler<TextDocumentHandler>()
                   .WithHandler<DidChangeWatchedFilesHandler>()
                   .WithHandler<FoldingRangeHandler>()
                   .WithHandler<MyWorkspaceSymbolsHandler>()
                   .WithHandler<MyDocumentSymbolHandler>()
                   .WithHandler<SemanticTokensHandler>()
                   .WithServices(x => x.AddLogging(b => b.SetMinimumLevel(LogLevel.Trace)))

                    // register filesystem and interpreter
                   .WithServices(services => {
                        services.AddSingleton<IncrementalInterpreterFileSystem>();
                        services.AddSingleton<Interpreter.Interpreter>();
                    })
                   .OnInitialize((server, request, token) => {
                        var logger = server.Services.GetService<ILogger<ILanguageServer>>();

                        var interpreter = server.Services.GetRequiredService<Interpreter.Interpreter>();

                        InterpreterConfig.Mode          = InterpreterMode.Lsp;
                        InterpreterConfig.ExecutionPath = request.RootPath;

                        var fs = server.Services.GetRequiredService<IncrementalInterpreterFileSystem>();
                        fs.Initialize(request.RootPath, true, false);

                        interpreter.Configure(fs);
                        Interpreter.Interpreter.Ctx = interpreter.GetNewExecContext();


                        logger.LogWarning("Initialize: Workspace: {@RootPath}", request.RootPath);

                        var manager = server.WorkDoneManager.For(
                            request, new WorkDoneProgressBegin {
                                Title      = "Server is starting...",
                                Percentage = 10,
                            }
                        );
                        workDone = manager;


                        // await Task.Delay(2000).ConfigureAwait(false);

                        manager.OnNext(
                            new WorkDoneProgressReport {
                                Percentage = 20,
                                Message    = "loading in progress"
                            }
                        );
                        return Task.CompletedTask;
                    })
                   .OnInitialized((server, request, response, token) => {
                        workDone.OnNext(
                            new WorkDoneProgressReport {
                                Percentage = 40,
                                Message    = "loading almost done",
                            }
                        );

                        // await Task.Delay(2000).ConfigureAwait(false);

                        workDone.OnNext(
                            new WorkDoneProgressReport {
                                Message    = "loading done",
                                Percentage = 100,
                            }
                        );
                        workDone.OnCompleted();
                        return Task.CompletedTask;
                    })
                   .OnStarted(async (server, token) => {
                        using var manager = await server.WorkDoneManager.Create(new WorkDoneProgressBegin {Title = "Doing some work..."})
                           .ConfigureAwait(false);

                        // manager.OnNext(new WorkDoneProgressReport {Message = "doing things..."});

                        var logger = server.Services.GetService<ILogger<ILanguageServer>>();

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

                        logger.LogInformation("Base Config: {@Config}", baseConfig);

                        var scopedConfig = new JObject();
                        foreach (var config in configuration.AsEnumerable()) {
                            scopedConfig.Add(config.Key, config.Value);
                        }

                        logger.LogInformation("Scoped Config: {@Config}", scopedConfig);
                    })
        ).ConfigureAwait(false);

        await server.WaitForExit.ConfigureAwait(false);
    }
}