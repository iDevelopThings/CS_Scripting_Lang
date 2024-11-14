using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using CSScriptingLang.Common.Extensions;
using CSScriptingLang.Interpreter;
using CSScriptingLang.Interpreter.Modules;
using CSScriptingLang.Lexing;
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


    public override async Task<SemanticTokens> Handle(SemanticTokensParams request, CancellationToken cancellationToken) {
        Debugger.Launch();

        _logger.LogWarning("[SemanticTokensParams] Handling request {@Request}", request.ToString());
        var result = await base.Handle(request, cancellationToken).ConfigureAwait(false);
        return result;
    }

    public override async Task<SemanticTokens> Handle(SemanticTokensRangeParams request, CancellationToken cancellationToken) {
        Debugger.Launch();
        _logger.LogWarning("[SemanticTokensRangeParams] Handling request {@Request}", request.ToString());
        var result = await base.Handle(request, cancellationToken).ConfigureAwait(false);
        return result;
    }

    public override async Task<SemanticTokensFullOrDelta> Handle(SemanticTokensDeltaParams request, CancellationToken cancellationToken) {
        Debugger.Launch();
        _logger.LogWarning("[SemanticTokensDeltaParams] Handling request {@Request}", request.ToString());
        var result = await base.Handle(request, cancellationToken).ConfigureAwait(false);
        return result;
    }

    protected override Task Tokenize(SemanticTokensBuilder builder, ITextDocumentIdentifierParams identifier, CancellationToken cancellationToken) {
        Debugger.Launch();

        // using var typesEnumerator     = RotateEnum(SemanticTokenType.Defaults).GetEnumerator();
        // using var modifiersEnumerator = RotateEnum(SemanticTokenModifier.Defaults).GetEnumerator();

        var fsPath = DocumentUri.GetFileSystemPath(identifier)!;

        var script = _interpreter.GetScript(identifier);
        if (script == null) {
            _logger.LogWarning(
                "Could not find script for path {Path}, available paths are: {Paths}, Modules: {Modules}",
                fsPath,
                _interpreter.ModuleResolver.ScriptsByAbsPath.Keys.Join(", "),
                _interpreter.ModuleResolver.Modules.Select(m => m.Key + " -> " + m.Value.Name).Join(", ")
            );
            return Task.CompletedTask;
        }

        var lexer = script.IncrementalLexer.CreateChildWithState(
            LexerState.CanOutputComments | LexerState.CanOutputNewLines | LexerState.CanOutputWhitespace,
            false
        );

        (bool IsValid, SemanticTokenType type) GetSemanticTokenType(Token token) {
            if (token.IsBlockComment || token.IsLineComment) {
                return (true, SemanticTokenType.Comment);
            }

            if (token.IsKeyword) {
                if (token.IsModuleKeyword || token.IsImportKeyword)
                    return (true, SemanticTokenType.Namespace);
                if (token.IsTypeDeclarationKeyword)
                    return (true, SemanticTokenType.Class);
                if (token.IsFunctionKeyword)
                    return (true, SemanticTokenType.Function);

                return (true, SemanticTokenType.Keyword);
            }

            if (token.IsIdentifier)
                return (true, SemanticTokenType.Variable);
            if (token.IsBoolean)
                return (true, SemanticTokenType.Variable);
            if (token.IsNumber)
                return (true, SemanticTokenType.Number);
            if (token.IsString)
                return (true, SemanticTokenType.String);

            return (false, SemanticTokenType.Variable);
        }

        foreach (var token in lexer.Tokenize()) {
            var r = token.Range;

            var (IsValid, type) = GetSemanticTokenType(token);
            if (!IsValid)
                continue;

            builder.Push(
                r.StartLine - 1,
                r.StartColumn,
                token.Value.Length,
                type,
                new SemanticTokenModifier[] { }
            );
        }


        // Debugger.Launch();

        return Task.CompletedTask;
    }

    protected override Task<SemanticTokensDocument> GetSemanticTokensDocument(ITextDocumentIdentifierParams @params, CancellationToken cancellationToken) {
        Debugger.Launch();
        return Task.FromResult(new SemanticTokensDocument(RegistrationOptions.Legend));
    }

    protected override SemanticTokensRegistrationOptions CreateRegistrationOptions(SemanticTokensCapability capability, ClientCapabilities clientCapabilities) {
        var opts = new SemanticTokensRegistrationOptions();

        opts.DocumentSelector = Script.LanguageSelector;
        opts.Id               = Guid.NewGuid().ToString();
        opts.Legend = new SemanticTokensLegend {
            TokenModifiers = capability?.TokenModifiers ?? new(),
            TokenTypes     = capability?.TokenTypes ?? new(),
        };
        opts.Full = new SemanticTokensCapabilityRequestFull {
            Delta = true,
        };
        opts.Range = false;

        return opts;
    }
}

public class SemanticTokensFullHandler : SemanticTokensFullHandlerBase
{
    private readonly ILogger                          _logger;
    private readonly Interpreter.Interpreter          _interpreter;
    private readonly IncrementalInterpreterFileSystem _fs;

    public SemanticTokensFullHandler(
        ILogger<SemanticTokensHandler>   logger,
        Interpreter.Interpreter          interpreter,
        IncrementalInterpreterFileSystem fs
    ) {
        _logger      = logger;
        _interpreter = interpreter;
        _fs          = fs;
    }


    public override async Task<SemanticTokens> Handle(SemanticTokensParams request, CancellationToken cancellationToken) {
        if (RegistrationOptions == null || RegistrationOptions.Legend == null) {
            Debugger.Launch();
        }
        var document = new SemanticTokensDocument(RegistrationOptions.Legend);
        // var document = await GetSemanticTokensDocument(request, cancellationToken).ConfigureAwait(false);
        var builder = document.Create();
        await Tokenize(builder, request, cancellationToken).ConfigureAwait(false);
        return builder.Commit().GetSemanticTokens();
    }

    protected Task Tokenize(SemanticTokensBuilder builder, ITextDocumentIdentifierParams identifier, CancellationToken cancellationToken) {
        // using var typesEnumerator     = RotateEnum(SemanticTokenType.Defaults).GetEnumerator();
        // using var modifiersEnumerator = RotateEnum(SemanticTokenModifier.Defaults).GetEnumerator();

        var fsPath = DocumentUri.GetFileSystemPath(identifier)!;

        var script = _interpreter.GetScript(identifier);
        if (script == null) {
            _logger.LogWarning(
                "Could not find script for path {Path}, available paths are: {Paths}, Modules: {Modules}",
                fsPath,
                _interpreter.ModuleResolver.ScriptsByAbsPath.Keys.Join(", "),
                _interpreter.ModuleResolver.Modules.Select(m => m.Key + " -> " + m.Value.Name).Join(", ")
            );
            return Task.CompletedTask;
        }

        var lexer = script.IncrementalLexer.CreateChildWithState(
            LexerState.CanOutputComments | LexerState.CanOutputNewLines | LexerState.CanOutputWhitespace,
            false
        );

        (bool IsValid, SemanticTokenType type) GetSemanticTokenType(Token token) {
            if (token.IsBlockComment || token.IsLineComment) {
                return (true, SemanticTokenType.Comment);
            }

            if (token.IsKeyword) {
                if (token.IsModuleKeyword || token.IsImportKeyword)
                    return (true, SemanticTokenType.Namespace);
                if (token.IsTypeDeclarationKeyword)
                    return (true, SemanticTokenType.Class);
                if (token.IsFunctionKeyword)
                    return (true, SemanticTokenType.Function);

                return (true, SemanticTokenType.Keyword);
            }

            if (token.IsIdentifier)
                return (true, SemanticTokenType.Variable);
            if (token.IsBoolean)
                return (true, SemanticTokenType.Variable);
            if (token.IsNumber)
                return (true, SemanticTokenType.Number);
            if (token.IsString)
                return (true, SemanticTokenType.String);

            return (false, SemanticTokenType.Variable);
        }

        foreach (var token in lexer.Tokenize()) {
            var r = token.Range;

            var (IsValid, type) = GetSemanticTokenType(token);
            if (!IsValid)
                continue;

            builder.Push(
                r.StartLine - 1,
                r.StartColumn,
                token.Value.Length,
                type,
                new SemanticTokenModifier[] { }
            );
        }


        // Debugger.Launch();

        return Task.CompletedTask;
    }

    protected override SemanticTokensRegistrationOptions CreateRegistrationOptions(SemanticTokensCapability capability, ClientCapabilities clientCapabilities) {
        var opts = new SemanticTokensRegistrationOptions();

        opts.DocumentSelector = Script.LanguageSelector;
        // opts.Id               = Guid.NewGuid().ToString();

        opts.Legend = new SemanticTokensLegend {
            TokenModifiers = capability?.TokenModifiers ?? new(),
            TokenTypes     = capability?.TokenTypes ?? new(),
        };
        opts.Full  = true;
        opts.Range = false;

        return opts;
    }
}