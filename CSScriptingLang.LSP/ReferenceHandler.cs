using CSScriptingLang.Interpreter.Modules;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace CSScriptingLang.LSP;

public class ReferenceHandler : ReferencesHandlerBase
{
    private readonly ILogger<ReferenceHandler>    _logger;
    private readonly ILanguageServerConfiguration _configuration;

    public ReferenceHandler(ILogger<ReferenceHandler> logger, ILanguageServerConfiguration configuration) {
        _logger        = logger;
        _configuration = configuration;
    }

    public override Task<LocationContainer> Handle(ReferenceParams request, CancellationToken cancellationToken) {
        _logger.LogWarning("[ReferenceHandler] {@Request} - uri: {@uri}", request, request.TextDocument.Uri);

        return Task.FromResult(new LocationContainer());
    }

    protected override ReferenceRegistrationOptions CreateRegistrationOptions(ReferenceCapability capability, ClientCapabilities clientCapabilities) {
        return new ReferenceRegistrationOptions {
            DocumentSelector = TextDocumentSelector.ForLanguage(Script.LanguageId),
        };
    }

}