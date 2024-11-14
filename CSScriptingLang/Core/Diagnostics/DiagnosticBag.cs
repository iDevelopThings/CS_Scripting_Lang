using CSScriptingLang.Interpreter.Modules;
using CSScriptingLang.Parsing.AST.NamedSymbol;
using CSScriptingLang.Utils;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace CSScriptingLang.Core.Diagnostics;

public class DiagnosticBag
{
    private readonly List<Diagnostic> _diagnostics = new();

    public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics;

    public int    ScriptId { get; }
    public Script Script   => ModuleResolver.GetScriptById(ScriptId);

    public event EventHandler<Diagnostic> OnDiagnosticPublished;

    public DiagnosticBag(int scriptId) {
        ScriptId = scriptId;

        if (Script != null) {
            Script.OnVersionChanged += _ => {
                Clear();
            };
        }
    }

    public void Report(Diagnostic diagnostic) {
        _diagnostics.Add(diagnostic);
        OnDiagnosticPublished?.Invoke(this, diagnostic);
    }
    public void Report(DiagnosticSeverity severity, string message, NamedSymbolRange range, Caller sourceCallerInfo) {
        var diagnostic = new Diagnostic(severity, message, range, sourceCallerInfo);
        Report(diagnostic);
    }

    public void Report(DiagnosticSeverity severity, string message, NamedSymbolRange range)
        => Report(severity, message, range, Caller.GetFromFrame(2));

    public void ReportError(string message, NamedSymbolRange range)
        => Report(DiagnosticSeverity.Error, message, range);

    public void ReportWarning(string message, NamedSymbolRange range)
        => Report(DiagnosticSeverity.Warning, message, range);

    public void Clear() {
        _diagnostics.Clear();
    }
}