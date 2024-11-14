using System.Reflection;
using CSScriptingLang.Lexing;
using CSScriptingLang.Mixins;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.Parsing.AST.NamedSymbol;
using CSScriptingLang.IncrementalParsing.Syntax;
using CSScriptingLang.Utils;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace CSScriptingLang.Core.Diagnostics;

/*[AttributeUsage(AttributeTargets.Module | AttributeTargets.Assembly)]
public class InterceptCallerAttribute : Attribute
{
    public InterceptCallerAttribute() { }

    public void Init(object instance, MethodBase method, object[] args) {

    }

    public void OnEntry()                        { }

    public void OnExit()                         { }

    public void OnException(Exception exception) { }
}*/

public class DiagnosticConsumer
{
    public virtual bool CanConsume(Diagnostic diagnostic) => true;

    // Return true to "Consume" it and prevent other processors from processing it
    // Returning false will allow other consumers to process the diagnostic
    public virtual bool Consume(Diagnostic diagnostic) {
        var message          = diagnostic.Message;
        var severity         = diagnostic.Severity;
        var range            = diagnostic.Range;
        var sourceCallerInfo = diagnostic.SourceCallerInfo;
        var file             = diagnostic.File;

        return true;
    }
}

public struct DiagnosticBuilder
{
    private int                _scriptId = -1;
    private DiagnosticSeverity _severity = DiagnosticSeverity.Error;
    private string             _message;
    private NamedSymbolRange   _range;
    private Caller             _caller;
    private bool               _asFatal        = false;
    private Exception          _fatalException = null;

    public DiagnosticBuilder() {
        _caller = Utils.Caller.GetFromFrame(2);
    }
    public DiagnosticBuilder(Caller caller) {
        _caller = caller;
    }

    public DiagnosticBuilder(
        int                scriptId,
        DiagnosticSeverity severity,
        string             message,
        NamedSymbolRange   range,
        Caller             caller
    ) {
        this._scriptId = scriptId;
        this._severity = severity;
        _message       = message;
        _range         = range;
        _caller        = caller;
    }

    public DiagnosticBuilder ScriptId(int scriptId) {
        this._scriptId = scriptId;
        return this;
    }

    public DiagnosticBuilder Severity(DiagnosticSeverity severity) {
        this._severity = severity;
        return this;
    }

    public DiagnosticBuilder Message(string message) {
        _message = message;
        return this;
    }

    public DiagnosticBuilder Range(BaseNode node) {
        _range = node;
        if (_scriptId == -1) {
            _scriptId = node.ScriptId;
        }
        return this;
    }
    public DiagnosticBuilder Range(SyntaxElement node) {
        _range = node.GetSymbolRange();
        if (_scriptId == -1) {
            _scriptId = node.ScriptId;
        }
        return this;
    }
    public DiagnosticBuilder Range(NamedSymbolRange range) {
        _range = range;
        if (_scriptId == -1) {
            _scriptId = range.Script?.Id ?? -1;
        }
        return this;
    }

    public DiagnosticBuilder Caller(Caller caller) {
        _caller = caller;
        return this;
    }

    public DiagnosticBuilder AsFatal(Exception ex = null) {
        _asFatal        = true;
        _fatalException = ex;
        return this;
    }


    public DiagnosticBuilder Error()   => Severity(DiagnosticSeverity.Error);
    public DiagnosticBuilder Warning() => Severity(DiagnosticSeverity.Warning);
    public DiagnosticBuilder Info()    => Severity(DiagnosticSeverity.Information);
    public DiagnosticBuilder Hint()    => Severity(DiagnosticSeverity.Hint);



    public static implicit operator Diagnostic(DiagnosticBuilder builder) {
        return new Diagnostic(
            builder._severity,
            builder._message,
            builder._range,
            builder._caller
        ) {
            IsFatal = builder._asFatal,
        };
    }

    public void Report() {
        if (_asFatal) {
            DiagnosticManager.ThrowFatal(_fatalException, this);
            return;
        }

        DiagnosticManager.Report(this);
    }

    public void ReportToSyntaxTree(SyntaxTree tree) {
        tree.PushDiagnostic(this);
    }
}


[AddMixin(typeof(DiagnosticLoggingMixin))]
public static partial class DiagnosticManager
{
    private static Dictionary<int, DiagnosticBag> scriptDiagnosticsMap = new();

    public static event EventHandler<Diagnostic> OnDiagnosticPublished;

    public static List<DiagnosticConsumer> DiagnosticConsumers { get; } = new();

    private static void OnPublish(object sender, Diagnostic diagnostic) {
        OnDiagnosticPublished?.Invoke(sender, diagnostic);
    }

    public static void TryConsumeScriptDiagnostics(int scriptId, Action<IReadOnlyList<Diagnostic>> onConsume = null) {
        if (!scriptDiagnosticsMap.TryGetValue(scriptId, out var bag)) {
            onConsume?.Invoke(null);
            return;
        }
        if (onConsume != null) {
            onConsume(bag.Diagnostics);
            return;
        }
        foreach (var diagnostic in bag.Diagnostics) {

            foreach (var consumer in DiagnosticConsumers) {

                if (!consumer.CanConsume(diagnostic))
                    break;
                if (consumer.Consume(diagnostic))
                    break;
            }
        }
    }
    public static void TryConsumeAll() {
        foreach (var pair in scriptDiagnosticsMap) {
            TryConsumeScriptDiagnostics(pair.Key);
        }
    }

    public static DiagnosticBag GetOrCreateDiagnostics(int scriptId) {
        return scriptDiagnosticsMap.GetOrAdd(scriptId, () => {
            var bag = new DiagnosticBag(scriptId);
            bag.OnDiagnosticPublished += OnPublish;
            return bag;
        });
    }

    public static DiagnosticBuilder Build()              => new();
    public static DiagnosticBuilder Build(Caller caller) => new(caller);

    public static void Report(Diagnostic diagnostic) {
        GetOrCreateDiagnostics(diagnostic.Range.Script?.Id ?? throw new ArgumentNullException(nameof(diagnostic.Range.Script)))
           .Report(diagnostic);
    }

    public static void Report(DiagnosticSeverity severity, string message, NamedSymbolRange range, Caller sourceCallerInfo)
        => GetOrCreateDiagnostics(range.Script?.Id ?? throw new ArgumentNullException(nameof(range.Script)))
           .Report(severity, message, range, sourceCallerInfo);

    public static void ReportError(string message, NamedSymbolRange range)
        => GetOrCreateDiagnostics(range.Script?.Id ?? throw new ArgumentNullException(nameof(range.Script)))
           .ReportError(message, range);

    public static void ReportError(string message, NamedSymbolRange range, Caller sourceCallerInfo)
        => GetOrCreateDiagnostics(range.Script?.Id ?? throw new ArgumentNullException(nameof(range.Script)))
           .Report(DiagnosticSeverity.Error, message, range, sourceCallerInfo);

    public static void ReportWarning(string message, NamedSymbolRange range)
        => GetOrCreateDiagnostics(range.Script?.Id ?? throw new ArgumentNullException(nameof(range.Script)))
           .ReportWarning(message, range);

    public static void ReportWarning(string message, NamedSymbolRange range, Caller sourceCallerInfo)
        => GetOrCreateDiagnostics(range.Script?.Id ?? throw new ArgumentNullException(nameof(range.Script)))
           .Report(DiagnosticSeverity.Warning, message, range, sourceCallerInfo);

    public static IEnumerable<DiagnosticBag> GetAllDiagnostics() {
        return scriptDiagnosticsMap.Values;
    }

    public static void ThrowFatal(Exception ex, Diagnostic diagnostic) {
        GetOrCreateDiagnostics(diagnostic.Range.Script?.Id ?? throw new ArgumentNullException(nameof(diagnostic.Range.Script)))
           .Report(diagnostic);
        
        if (ex == null) {
            throw new FatalDiagnosticException(diagnostic);
        }

        throw new FatalDiagnosticException(diagnostic, ex);
    }
    public static void ThrowFatal(Exception ex, Token fromToken, Caller sourceCallerInfo) {
        var diagnostic = new Diagnostic(DiagnosticSeverity.Error, ex.Message, fromToken, sourceCallerInfo);
        ThrowFatal(ex, diagnostic);
    }
    public static void ThrowFatal(Exception ex, Token fromToken)
        => ThrowFatal(ex, fromToken, Caller.GetFromFrame(2));

    public static void ThrowFatal(Exception ex, BaseNode fromNode, Caller sourceCallerInfo) {
        var diagnostic = new Diagnostic(DiagnosticSeverity.Error, ex.Message, fromNode, sourceCallerInfo);
        GetOrCreateDiagnostics(fromNode.ScriptId).Report(diagnostic);
        throw new FatalDiagnosticException(diagnostic, ex);
    }
    public static void ThrowFatal(Exception ex, BaseNode fromNode)
        => ThrowFatal(ex, fromNode, Caller.GetFromFrame(2));


    public static void AddConsumer(DiagnosticConsumer consumer) {
        DiagnosticConsumers.Add(consumer);
    }

    public static void ClearScriptDiagnostics(int id) {
        if (scriptDiagnosticsMap.ContainsKey(id))
            scriptDiagnosticsMap.Remove(id);
    }
}