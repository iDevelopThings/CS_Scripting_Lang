using CSScriptingLang.Interpreter.Modules;
using CSScriptingLang.Parsing.AST.NamedSymbol;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace CSScriptingLang.LSP;

public class DocumentSymbolHandler : DocumentSymbolHandlerBase
{
    private readonly ILogger<DocumentSymbolHandler> _logger;
    private readonly ILanguageServerConfiguration   _configuration;
    private readonly Interpreter.Interpreter        _interpreter;

    public DocumentSymbolHandler(
        ILogger<DocumentSymbolHandler> logger,
        ILanguageServerConfiguration   configuration,
        Interpreter.Interpreter        interpreter
    ) {
        _logger        = logger;
        _configuration = configuration;
        _interpreter   = interpreter;
    }

    public override Task<SymbolInformationOrDocumentSymbolContainer> Handle(
        DocumentSymbolParams request,
        CancellationToken    cancellationToken
    ) {
        _logger.LogWarning("[DocumentSymbolParams] {@Request} - uri: {@uri}", request, request.TextDocument.Uri);

        var script = _interpreter.GetScript(request);
        if (script == null) {
            _logger.LogCritical("No script found for {uri}", request.TextDocument.Uri);
            return Task.FromResult(new SymbolInformationOrDocumentSymbolContainer());
        }

        var symbols = new List<SymbolInformationOrDocumentSymbol>();
        foreach (var symbol in script.NamedSymbols.All()) {
            symbols.Add(new DocumentSymbol {
                Detail         = symbol.Name,
                Kind           = symbol.Kind.ToSymbolKind(),
                Range          = symbol.Range,
                SelectionRange = symbol.Range,
                Name           = symbol.Name,
            });
        }

        return Task.FromResult<SymbolInformationOrDocumentSymbolContainer>(symbols);
    }

    protected override DocumentSymbolRegistrationOptions CreateRegistrationOptions(DocumentSymbolCapability capability, ClientCapabilities clientCapabilities) {
        return new DocumentSymbolRegistrationOptions {
            DocumentSelector = TextDocumentSelector.ForLanguage(Script.LanguageId)
        };
    }
}