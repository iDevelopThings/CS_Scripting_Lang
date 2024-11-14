using CSScriptingLang.Mixins;
using CSScriptingLang.Parsing.AST;

namespace CSScriptingLang.Core.Diagnostics;


// [AddMixin(typeof(DiagnosticLoggingMixin))]

[Mixin(
    "CSScriptingLang.Core.Diagnostics",
    "CSScriptingLang.Lexing",
    "CSScriptingLang.Mixins",
    "CSScriptingLang.Utils",
    "CSScriptingLang.Parsing.AST"
)]
public class DiagnosticLoggingMixin
{

    // ReSharper disable once RedundantNameQualifier
    public DiagnosticBuilder BuildDiagnostic() => DiagnosticManager.Build(CSScriptingLang.Utils.Caller.GetFromFrame(3));

    public DiagnosticBuilder Diagnostic_Error(string message = null)       => BuildDiagnostic().Error().Message(message);
    public DiagnosticBuilder Diagnostic_Error_Fatal(string message = null) => BuildDiagnostic().Error().AsFatal().Message(message);
    public DiagnosticBuilder Diagnostic_Warning(string message = null)     => BuildDiagnostic().Warning().Message(message);
    public DiagnosticBuilder Diagnostic_Info(string message = null)        => BuildDiagnostic().Info().Message(message);
    public DiagnosticBuilder Diagnostic_Hint(string message = null)        => BuildDiagnostic().Hint().Message(message);

    public DiagnosticBuilder ExpectedBuilder(string message) =>
        BuildDiagnostic().Error().AsFatal().Message(message);

    public void Expected(string message, BaseNode node) {
        ExpectedBuilder(message)
           .Range(node)
           .Report();
    }
    
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
    */

    
    /*
    public void Diagnostic_ErrorFatal(string message, Token token, Caller caller) {
        DiagnosticManager.ThrowFatal(
            new ParserException(message, token.Range, token.Range, token.GetScript())
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
    */


/*
    public void Diagnostic_Error(string message, Token token, Caller caller) => Diagnostic(DiagnosticSeverity.Error, message, token, caller);
    public void Diagnostic_Error(string message, Token token) => Diagnostic_Error(message, token, Caller.GetFromFrame());

    public void Diagnostic_Warning(string message, Token token, Caller caller) => Diagnostic(DiagnosticSeverity.Warning, message, token, caller);
    public void Diagnostic_Warning(string message, Token token) => Diagnostic_Warning(message, token, Caller.GetFromFrame());
    */

}

