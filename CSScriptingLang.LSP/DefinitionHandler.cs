using System.Diagnostics;
using CSScriptingLang.Common.Extensions;
using CSScriptingLang.IncrementalParsing.Syntax.SyntaxNodes;
using CSScriptingLang.Interpreter.Modules;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace CSScriptingLang.LSP;

public class DefinitionHandler : DefinitionHandlerBase
{
    private readonly ILogger<DefinitionHandler>   _logger;
    private readonly ILanguageServerConfiguration _configuration;
    private readonly Interpreter.Interpreter      _interpreter;

    public DefinitionHandler(
        ILogger<DefinitionHandler>   logger,
        ILanguageServerConfiguration configuration,
        Interpreter.Interpreter      interpreter
    ) {
        _logger        = logger;
        _configuration = configuration;
        _interpreter   = interpreter;
    }


    public override Task<LocationOrLocationLinks> Handle(DefinitionParams request, CancellationToken cancellationToken) {
        var script = _interpreter.GetScript(request);
        if (script == null) {
            _logger.LogCritical("No script found for {uri}", request.TextDocument.Uri);
            return Task.FromResult(new LocationOrLocationLinks());
        }
        
        var node = script.SyntaxTree.SyntaxRoot.NodeAt(request.Position.Line, request.Position.Character);
        if (node == null) {
            _logger.LogWarning("No node found for {uri} at {position}", request.TextDocument.Uri, request.Position);
            return Task.FromResult(new LocationOrLocationLinks());
        }
        // Debugger.Launch();
        // if(node.Parent is MemberAccessExpr memberAccessExpr) {
            // node = memberAccessExpr;
        // }
        
        _logger.LogWarning("Matched node {node}", node.ToSimpleDebugString());
        
        var references = node.FindReferences().ToList();

        _logger.LogWarning("Found {count} references: {references}", references.Count, references.Select(r => r.ToDebugString()).Join(", "));

        var referenceLinks = references.Select(reference => {
            return new LocationOrLocationLink(
                new Location {
                    Uri   = reference.Script.Uri,
                    Range = reference.Range,
                }
            );
        });


        /*
        if (!script.NamedSymbols[script].TryGetByPosition(request.Position, out var symbol)) {
            _logger.LogWarning("No symbol found for {uri} at {position}", request.TextDocument.Uri, request.Position);
            var availableSymbols = script.NamedSymbols.Values.Select(s => $"{s.Name} -> {s.Position}").Join(",\n ");
            _logger.LogWarning("Available symbols: {symbols}", availableSymbols);
            return Task.FromResult(new LocationOrLocationLinks());
        }

        var references = symbol.Node.FindReferences()
           .Select(reference => {
                return new LocationOrLocationLink(
                    new Location {
                        Uri   = reference.Script.Uri,
                        Range = reference.Range,
                    }
                );
            });
            */


        var location = new LocationOrLocationLinks(referenceLinks);

        /*var location = new LocationOrLocationLinks(new[] {
            new LocationOrLocationLink(
                new Location {
                    Uri   = script.Uri,
                    Range = symbol.Range,
                }
            )
        });*/

        // _logger.LogWarning("[DefinitionHandler] Found symbol: {symbol}", symbol);

        return Task.FromResult(location);
    }

    protected override DefinitionRegistrationOptions CreateRegistrationOptions(DefinitionCapability capability, ClientCapabilities clientCapabilities) {
        return new DefinitionRegistrationOptions {
            DocumentSelector = TextDocumentSelector.ForLanguage(Script.LanguageId),
        };
    }

}