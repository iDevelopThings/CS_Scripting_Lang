using System.Diagnostics;
using CSScriptingLang.Interpreter;
using CSScriptingLang.Interpreter.Modules;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace CSScriptingLang.LSP;

public class DocumentDiagnosticHandler : DocumentDiagnosticHandlerBase
{
    private readonly ILogger                 _logger;
    private readonly Interpreter.Interpreter _interpreter;

    public DocumentDiagnosticHandler(
        ILogger<DocumentDiagnosticHandler> logger,
        Interpreter.Interpreter            interpreter
    ) {
        _logger      = logger;
        _interpreter = interpreter;
    }

    protected override DiagnosticsRegistrationOptions CreateRegistrationOptions(DiagnosticClientCapabilities capability, ClientCapabilities clientCapabilities) {
        return new DiagnosticsRegistrationOptions {
            DocumentSelector = TextDocumentSelector.ForLanguage(Script.LanguageId)
        };
    }

    public override Task<RelatedDocumentDiagnosticReport> Handle(DocumentDiagnosticParams request, CancellationToken cancellationToken) {
        _logger.LogWarning("Collecting diagnostics for {uri}", request.TextDocument.Uri);

        Debugger.Launch();

        var result = new RelatedFullDocumentDiagnosticReport();

        return Task.FromResult((RelatedDocumentDiagnosticReport) result);
    }
}