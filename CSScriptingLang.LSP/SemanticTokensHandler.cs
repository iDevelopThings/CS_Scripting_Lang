using CSScriptingLang.Interpreter;
using CSScriptingLang.Interpreter.Modules;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace CSScriptingLang.LSP;

public class SemanticTokensHandler : SemanticTokensHandlerBase
{
    private readonly ILogger                          _logger;
    private readonly Interpreter.Interpreter          _interpreter;
    private readonly IncrementalInterpreterFileSystem _fs;

    public SemanticTokensHandler(
        ILogger<SemanticTokensHandler>   logger,
        Interpreter.Interpreter          interpreter,
        IncrementalInterpreterFileSystem fs
    ) {
        _logger      = logger;
        _interpreter = interpreter;
        _fs          = fs;
    }


    public override async Task<SemanticTokens> Handle(
        SemanticTokensParams request, CancellationToken cancellationToken
    ) {
        var result = await base.Handle(request, cancellationToken).ConfigureAwait(false);
        return result;
    }

    public override async Task<SemanticTokens> Handle(
        SemanticTokensRangeParams request, CancellationToken cancellationToken
    ) {
        var result = await base.Handle(request, cancellationToken).ConfigureAwait(false);
        return result;
    }

    public override async Task<SemanticTokensFullOrDelta> Handle(
        SemanticTokensDeltaParams request,
        CancellationToken         cancellationToken
    ) {
        var result = await base.Handle(request, cancellationToken).ConfigureAwait(false);
        return result;
    }

    protected override Task Tokenize(
        SemanticTokensBuilder         builder,
        ITextDocumentIdentifierParams identifier,
        CancellationToken             cancellationToken
    ) {
        using var typesEnumerator     = RotateEnum(SemanticTokenType.Defaults).GetEnumerator();
        using var modifiersEnumerator = RotateEnum(SemanticTokenModifier.Defaults).GetEnumerator();

        var fsPath = DocumentUri.GetFileSystemPath(identifier)!;

        if (!_interpreter.ModuleResolver.ScriptsByAbsPath.TryGetValue(fsPath, out var script)) {
            _logger.LogWarning("Could not find script for path {Path}", fsPath);
            return Task.CompletedTask;
        }

        var tokens = script.AstData.Lexer.Tokens;

        foreach (var token in tokens) {

            typesEnumerator.MoveNext();
            modifiersEnumerator.MoveNext();

            var r = token.Range;

            SemanticTokenType type;
            if (token.IsKeyword) {
                type = SemanticTokenType.Keyword;
                if(token.IsModuleKeyword || token.IsImportKeyword) {
                    type = SemanticTokenType.Namespace;
                } else if (token.IsTypeDeclarationKeyword) {
                    type = SemanticTokenType.Class;
                } else if (token.IsFunctionKeyword) {
                    type = SemanticTokenType.Function;
                } else if (token.IsCoroutineKeyword) {
                    type = SemanticTokenType.Method;
                } 
            } else if (token.IsIdentifier) {
                type = SemanticTokenType.Variable;
            } else if (token.IsNumber) {
                type = SemanticTokenType.Number;
            } else if (token.IsString) {
                type = SemanticTokenType.String;
            } else {
                type = SemanticTokenType.Variable;
            }

            builder.Push(r.StartLine, r.Start, r.Total, type, new SemanticTokenModifier[] {});
        }

        return Task.CompletedTask;

        /*
        // you would normally get this from a common source that is managed by current open editor, current active editor, etc.
        var content = await File.ReadAllTextAsync(DocumentUri.GetFileSystemPath(identifier), cancellationToken).ConfigureAwait(false);
        await Task.Yield();

        foreach (var (line, text) in content.Split('\n').Select((text, line) => (line, text))) {
            var parts = text.TrimEnd().Split(';', ' ', '.', '"', '(', ')');
            var index = 0;
            foreach (var part in parts) {
                typesEnumerator.MoveNext();
                modifiersEnumerator.MoveNext();
                if (string.IsNullOrWhiteSpace(part)) continue;
                index = text.IndexOf(part, index, StringComparison.Ordinal);
                builder.Push(line, index, part.Length, typesEnumerator.Current, modifiersEnumerator.Current);
            }
        }*/
    }

    protected override Task<SemanticTokensDocument>
        GetSemanticTokensDocument(ITextDocumentIdentifierParams @params, CancellationToken cancellationToken) {
        return Task.FromResult(new SemanticTokensDocument(RegistrationOptions.Legend));
    }


    private IEnumerable<T> RotateEnum<T>(IEnumerable<T> values) {
        while (true) {
            foreach (var item in values)
                yield return item;
        }
    }

    protected override SemanticTokensRegistrationOptions CreateRegistrationOptions(
        SemanticTokensCapability capability, ClientCapabilities clientCapabilities
    ) {
        return new SemanticTokensRegistrationOptions {
            DocumentSelector = TextDocumentSelector.ForLanguage(Script.LanguageId),
            Legend = new SemanticTokensLegend {
                TokenModifiers = capability.TokenModifiers,
                TokenTypes     = capability.TokenTypes
            },
            Full = new SemanticTokensCapabilityRequestFull {
                Delta = true
            },
            Range = true
        };
    }
}