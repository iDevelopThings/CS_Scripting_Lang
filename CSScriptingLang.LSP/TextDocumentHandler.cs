using System.Diagnostics;
using CSScriptingLang.Core.Diagnostics;
using CSScriptingLang.Interpreter;
using CSScriptingLang.Interpreter.Modules;
using CSScriptingLang.Utils;
using MediatR;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;

namespace CSScriptingLang.LSP;

public class TextDocumentHandler : TextDocumentSyncHandlerBase
{
    private readonly ILogger<TextDocumentHandler>     _logger;
    private readonly ILanguageServerConfiguration     _configuration;
    private readonly Interpreter.Interpreter          _interpreter;
    private readonly IncrementalInterpreterFileSystem _fs;

    private readonly TextDocumentSelector _textDocumentSelector = new(
        new TextDocumentFilter {
            Pattern = "**/*" + Script.Extension
        }
    );

    public TextDocumentHandler(
        ILogger<TextDocumentHandler>     logger,
        ILanguageServerConfiguration     configuration,
        Interpreter.Interpreter          interpreter,
        IncrementalInterpreterFileSystem fs
    ) {
        _logger        = logger;
        _configuration = configuration;
        _interpreter   = interpreter;
        _fs            = fs;
    }

    public TextDocumentSyncKind Change { get; } = TextDocumentSyncKind.Full;


    private void PublishScriptDiagnostics(
        Script      script,
        DocumentUri uri,
        int?        version,
        bool        clear = true
    ) {
        DiagnosticManager.TryConsumeScriptDiagnostics(
            script.Id,
            diagnostics => {
                LSPContext.Server.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams {
                    Uri         = uri,
                    Diagnostics = diagnostics != null ? Container<LSPDiagnostic>.From(diagnostics.Select(d => (LSPDiagnostic) d)) : new Container<LSPDiagnostic>(),
                    Version     = version ?? script.File.Version
                });
            }
        );

        if (clear) {
            DiagnosticManager.ClearScriptDiagnostics(script.Id);
        }
    }

    public override Task<Unit> Handle(DidChangeTextDocumentParams notification, CancellationToken token) {
        _logger.LogInformation("[DidChangeTextDocumentParams] File: {@File}, Version: {@Version}", notification.TextDocument.Uri.ToString(), notification.TextDocument.Version);

        var uri = notification.TextDocument.Uri;
        if (!_fs.Exists(uri.GetFileSystemPath()))
            return Unit.Task;

        if (notification.TextDocument.Version == null) {
            throw new InvalidOperationException("Version is null");
        }

        try {

            var file = _fs.GetFile(uri.GetFileSystemPath());
            file.Version = notification.TextDocument.Version.Value;

            var source = new SourceWrapper(file.Content);

            if (notification.ContentChanges.Count() == 1 && notification.ContentChanges.First().Range == null) {
                file.UpdateContent(notification.ContentChanges.First().Text);
            } else {
                foreach (var change in notification.ContentChanges) {
                    var text = change.Text;

                    _logger.LogInformation("[DidChangeTextDocumentParams] Content Change Range = {@Range}", change.Range);

                    if (change.Range == null) {
                        file.UpdateContent(text);
                        continue;
                    }

                    source.Source = source.InsertAtRange(change.Range, text);
                }

                file.UpdateContent(source.Source);
            }
        }
        catch (FatalDiagnosticException) { }

        PublishScriptDiagnostics(_interpreter.GetScript(uri), uri, notification.TextDocument.Version);

        return Unit.Task;
    }

    public override Task<Unit> Handle(DidOpenTextDocumentParams notification, CancellationToken token) {
        _logger.LogInformation("[DidOpenTextDocumentParams] File: {@File}, Version: {@Version}", notification.TextDocument.Uri.ToString(), notification.TextDocument.Version);
        var uri  = notification.TextDocument.Uri;
        var text = notification.TextDocument.Text;

        var path = uri.GetFileSystemPath();

        var f = _fs.AddFile(path, text);
        
        PublishScriptDiagnostics(_interpreter.GetScript(uri), uri, notification.TextDocument.Version, false);
        
        if (notification.TextDocument.Version != null) {
            f.Version = notification.TextDocument.Version.Value;
        }

        PublishScriptDiagnostics(_interpreter.GetScript(uri), uri, notification.TextDocument.Version, false);


        return Unit.Task;
    }

    public override Task<Unit> Handle(DidCloseTextDocumentParams notification, CancellationToken token) {
        _logger.LogInformation("[DidCloseTextDocumentParams] File: {@File}", notification.TextDocument.Uri.ToString());

        var uri    = notification.TextDocument.Uri;
        var script = _interpreter.GetScript(uri);

        PublishScriptDiagnostics(script, uri, script.File.Version, true);

        return Unit.Task;
    }

    public override Task<Unit> Handle(DidSaveTextDocumentParams notification, CancellationToken token) {
        _logger.LogInformation("[DidSaveTextDocumentParams] File: {@File}", notification.TextDocument.Uri.ToString());

        var uri = notification.TextDocument.Uri;
        if (!_fs.Exists(uri.GetFileSystemPath()))
            return Unit.Task;

        return Unit.Task;
    }

    protected override TextDocumentSyncRegistrationOptions CreateRegistrationOptions(TextSynchronizationCapability capability, ClientCapabilities clientCapabilities) => new() {
        DocumentSelector = _textDocumentSelector,
        Change           = Change,
        Save             = new SaveOptions() {IncludeText = true},
    };

    public override TextDocumentAttributes GetTextDocumentAttributes(DocumentUri uri) => new(uri, Script.LanguageId);
}