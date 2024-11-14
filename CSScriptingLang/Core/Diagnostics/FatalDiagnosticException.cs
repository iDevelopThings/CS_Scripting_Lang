using CSScriptingLang.Utils;

namespace CSScriptingLang.Core.Diagnostics;

public class BaseDiagnosticException : Exception
{
    public BaseDiagnosticException(string message) : base(message) { }
    public BaseDiagnosticException(string message, Exception inner) : base(message, inner) { }
}

public class FatalDiagnosticException : BaseDiagnosticException
{
    public Diagnostic Diagnostic { get; }
    public Caller     Caller     => Diagnostic.SourceCallerInfo;


    public FatalDiagnosticException(Diagnostic diagnostic) : base(diagnostic.Message) {
        Diagnostic = diagnostic;
    }
    public FatalDiagnosticException(Diagnostic diagnostic, Exception inner) : base(diagnostic.Message, inner) {
        Diagnostic = diagnostic;
    }
}