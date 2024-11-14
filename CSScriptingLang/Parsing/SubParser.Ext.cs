using System.Runtime.CompilerServices;
using CSScriptingLang.Core.Diagnostics;
using CSScriptingLang.Lexing;
using CSScriptingLang.Mixins;
using CSScriptingLang.Parsing.AST.NamedSymbol;
using CSScriptingLang.Utils;

namespace CSScriptingLang.Parsing;

[AddMixin(typeof(DiagnosticLoggingMixin))]
public partial class SubParser
{


    /*
    public void Diagnostic(
        DiagnosticSeverity severity,
        string             message,
        Token              token,
        Caller             caller
    ) => DiagnosticManager.Report(severity, message, token, caller);

    public void Diagnostic(
        DiagnosticSeverity severity,
        string             message,
        Token              token
    ) => Diagnostic(severity, message, token, Caller.GetFromFrame());

    public void Diagnostic_ErrorFatal(string message, Token token, Caller caller) {
        DiagnosticManager.ThrowFatal(
            new ParserException(message, token.Range, token.Range, Script)
               .WithCaller(caller)
               .WithInput(Lexer.GetInput()),
            token,
            caller
        );
    }
    public void Diagnostic_ErrorFatal(string message, Token token) => Diagnostic_ErrorFatal(message, token, Caller.GetFromFrame(2));

    public void Diagnostic_ErrorFatal(string message, Token fromToken, Token toToken, Caller caller) {
        DiagnosticManager.ThrowFatal(
            new ParserException(message, fromToken.Range, toToken.Range, Script)
               .WithCaller(caller)
               .WithInput(Lexer.GetInput()),
            fromToken,
            caller
        );
    }
    public void Diagnostic_ErrorFatal(string message, Token fromToken, Token toToken) => Diagnostic_ErrorFatal(message, fromToken, toToken, Caller.GetFromFrame(2));


    public void Diagnostic_Error(string message, Token token, Caller caller) => Diagnostic(DiagnosticSeverity.Error, message, token, caller);
    public void Diagnostic_Error(string message, Token token) => Diagnostic_Error(message, token, Caller.GetFromFrame());

    public void Diagnostic_Warning(string message, Token token, Caller caller) => Diagnostic(DiagnosticSeverity.Warning, message, token, caller);
    public void Diagnostic_Warning(string message, Token token) => Diagnostic_Warning(message, token, Caller.GetFromFrame());

    public void LogError(string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "") {
        Diagnostic_Error(message, Token, Caller.FromAttributes(file, line, member));

        RawLogError(message, file, line, member, Token.Range, Next.Range);
    }
    public void LogError(string message, Token fromToken, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "") {
        Diagnostic_Error(message, fromToken, Caller.FromAttributes(file, line, member));
        RawLogError(message, file, line, member, fromToken.Range, Next.Range);
    }
    public void LogError(string message, Token fromToken, Token toToken, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "") {
        Diagnostic_Error(message, fromToken, Caller.FromAttributes(file, line, member));
        RawLogError(message, file, line, member, fromToken.Range, toToken.Range);
    }

    public void RawLogError(
        string     message,
        string     file      = "",      int        line    = 0, string member = "",
        TokenRange fromRange = default, TokenRange toRange = default
    ) {
        // ErrorWriter.Create(Script, file, line, member)
        // .SetSourceIfNull(Lexer.GetInput())
        // .LogFatal(message, fromRange, toRange);
    }
    */

   
    public void Expected(string message, NamedSymbolRange range = null) {
        ExpectedBuilder(message)
           .Range(range ?? Token)
           .Report();
    }
    
    public void Unexpected(string message = "", NamedSymbolRange range = null) {
        ExpectedBuilder($"Unexpected token: {Token} {message}")
           .Range(range ?? Token)
           .Report();
    }


    public bool Ensure(TokenType type, string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "") {
        if (Token.Is(type))
            return true;
        
        ExpectedBuilder($"{message}; got {Token}")
           .Range((Token, Next))
           .Caller(Caller.FromAttributes(file, line, member))
           .Report();

        return false;
    }

    public Token EnsureAnyOfAndConsume(TokenType type, string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "") {
        var curToken = Token;
        foreach (var tokenType in type.GetFlags()) {
            if (Token.Is(tokenType)) {
                Advance();
                return curToken;
            }
        }

        ExpectedBuilder($"{message}; got {Token}")
           .Range((Token, Next))
           .Caller(Caller.FromAttributes(file, line, member))
           .Report();
        
        return curToken;
        // throw new ParserException($"{message}; got {Token}", Token.Range, Next.Range, Script)
        // .WithCaller(Caller.FromAttributes(file, line, member))
        // .WithInput(Lexer.GetInput());
    }
    public Token EnsureAndConsume(TokenType type, string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "") {
        var curToken = Token;
        if (Token.Is(type))
            Advance();
        else {
            
            ExpectedBuilder($"{message}; got {Token}")
               .Range((Token, Next))
               .Caller(Caller.FromAttributes(file, line, member))
               .Report();
            
            // throw new ParserException($"{message}; got {Token}", Token.Range, Next.Range, Script)
            // .WithCaller(Caller.FromAttributes(file, line, member))
            // .WithInput(Lexer.GetInput());
        }

        return curToken;
    }

    public Token EnsureAndConsume(Keyword type, string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "") {
        var curToken = Token;
        if (Token.Is(type))
            Advance();
        else {
            ExpectedBuilder($"{message}; got {Token}")
               .Range((Token, Next))
               .Caller(Caller.FromAttributes(file, line, member))
               .Report();
            
            // throw new ParserException($"{message}; got {Token}", Token.Range, Next.Range, Script)
            // .WithCaller(Caller.FromAttributes(file, line, member))
            // .WithInput(Lexer.GetInput());
        }
        // RawLogError($"{message}; got {Token}", file, line, member);

        return curToken;
    }
}