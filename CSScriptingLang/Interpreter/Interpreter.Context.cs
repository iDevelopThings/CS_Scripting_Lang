using System.Diagnostics;
using System.Runtime.CompilerServices;
using CSScriptingLang.Core.Diagnostics;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Coroutines;
using CSScriptingLang.Interpreter.Modules;
using CSScriptingLang.Lexing;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.Utils;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.RuntimeValues.Values;
using CSScriptingLang.Core.Logging;
using CSScriptingLang.Mixins;
using CSScriptingLang.IncrementalParsing.Syntax;
using SharpX;
using SharpX.Extensions;

namespace CSScriptingLang.Interpreter;

public struct ExecResult
{
    private Interpreter interpreter;

    public bool DidSetValue { get; set; }

    public int ValuesPushed { get; set; }

    private ValueReference[] _references { get; set; }
    public ValueReference[] References {
        get => _references ??= [];
        set => _references = value;
    }


    private List<object> _values { get; set; }
    public List<object> Values {
        get => _values ??= new();
        set => _values = value;
    }

    public ExecResult(Interpreter interpreter) {
        this.interpreter = interpreter;
        _values          = null;
        ValuesPushed     = 0;
        DidSetValue      = false;
    }

    public object this[int index] {
        get => Values[index];
        set {
            if (Values.Count <= index) {
                Values.Add(value);
                DidSetValue = true;
                return;
            }

            Values[index] = value;
            DidSetValue   = true;
        }
    }

    public T Get<T>() {
        return TryGet<T>(out var value) ? value : default;
    }
    public T Get<T>(Func<T, bool> predicate) {
        foreach (var value in Values) {
            if (value is T typedValue && predicate(typedValue)) {
                return typedValue;
            }
        }
        return default;
    }
    public T Get<T>(int atIndex) {
        var i = 0;
        foreach (var value in Values) {
            if (value is T typedValue) {
                if (i == atIndex) {
                    return typedValue;
                }
                i++;
            }
        }
        return default;
    }
    public Value Get(RTVT type) {
        foreach (var value in Values) {
            if (value is Value typedValue && typedValue.Type == type) {
                return typedValue;
            }
        }
        return null;
    }
    public (T1, T2) Get<T1, T2>() {
        return (Get<T1>(), Get<T2>());
    }
    public bool TryGetLast<T>(out T value) {
        if (Values.Count > 0) {
            var val = Values.OfType<T>().LastOrDefault();
            if (val != null) {
                value = val;
                return true;
            }
        }
        value = default;
        return false;
    }
    public bool TryGet<T>(out T value) {
        foreach (var val in Values) {
            if (val is T typedValue) {
                value = typedValue;
                return true;
            }
        }
        value = default;

        return false;
    }
    public static ExecResult operator +(ExecResult a, object b) {
        a.Values.Add(b);
        return a;
    }
    public static ExecResult operator +(ExecResult a, ValueReference b) {
        a.Values.Add(b.Value);
        return a;
    }
    public static ExecResult operator +(ExecResult a, IEnumerable<Value> b) {
        a.Values.AddRange(b);
        return a;
    }
    public static ExecResult operator +(ExecResult a, (VariableSymbol, Value) b) {
        a.Values.Add(b.Item1);
        a.Values.Add(b.Item2);
        return a;
    }

    public ExecResult Add(ExecResult b) {
        ValuesPushed += b.ValuesPushed;
        DidSetValue  |= b.DidSetValue;
        if (b._values != null)
            Values.AddRange(b.Values);
        return this;
    }

    public static ExecResult operator +(ExecResult a, ExecResult b) {
        a.ValuesPushed += b.ValuesPushed;
        a.DidSetValue  |= b.DidSetValue;
        if (b._values != null)
            a.Values.AddRange(b.Values);
        return a;
    }

    public bool HasValues() {
        return Values.Count > 0 || ValuesPushed > 0;
    }

    public bool TryGet(out (VariableSymbol, Value) value) {
        VariableSymbol symbol;
        Value          rtValue;

        if (!TryGet(out rtValue)) { }

        if (TryGet(out symbol)) {
            if (rtValue == null) {
                rtValue = symbol.Val;
            }
        }

        if (rtValue == null && symbol == null) {
            value = (null, null);
            return false;
        }

        value = (symbol, rtValue);

        return true;
    }

    public IEnumerable<T> PopValues<T>() {
        foreach (var value in Values) {
            if (value is T typedValue) {
                yield return typedValue;
            }
        }
    }

    public IEnumerable<object> GetValues() {
        foreach (var value in Values) {
            yield return value;
        }
    }

    public static explicit operator Value(ExecResult result) {
        if (result.TryGet(out Value value)) {
            return value;
        }

        return null;
    }

    public static explicit operator VariableSymbol(ExecResult result) {
        if (result.TryGet(out VariableSymbol value)) {
            return value;
        }

        return null;
    }

    public static explicit operator (VariableSymbol, Value)(ExecResult result) {
        if (result.TryGet(out (VariableSymbol, Value) value)) {
            return value;
        }

        return (null, null);
    }
    public ExecResult AddReference(ValueReference reference) {
        References = References.Append(reference).ToArray();
        return this;
    }

    public bool TryGetReference(out ValueReference reference) {
        if (References.Length > 0) {
            reference = References[^1];
            return true;
        }

        reference = default;
        return false;
    }
    public ref ValueReference GetReference(int index) => ref References[index];
    public ref ValueReference GetLastReference()      => ref References[^1];
    public ref ValueReference GetFirstReference()     => ref References[0];


}

[AddMixin(typeof(DiagnosticLoggingMixin))]
public partial class Interpreter
{
    public static Logger Logger = Logs.Get<Interpreter>(LogLevel.Debug);

    private static ClassScopedTimerInst<Interpreter> Timer = ClassScopedTimerInst<Interpreter>.Create(Logger)
       .SetColorFn(n => n.BoldBrightBlue())
       .SetName("Interpreter");

    public InterpreterFileSystem FileSystem { get; set; }

    public ModuleResolver ModuleResolver { get; set; }

    public Module Module { get; set; }
    public Script Script { get; set; }

    public Scheduler Scheduler { get; set; }

    [DebuggerStepThrough]
    public ExecResult NewResult() => new(this);

    [DebuggerStepThrough]
    private ExecResult NewResult(ValueReference value) {
        var r = NewResult();
        r += value;
        return r;
    }

    [DebuggerStepThrough]
    private ExecResult NewResult(Maybe<ValueReference> value) {
        if (value.MatchJust(out var reference)) {
            return NewResult(reference);
        }
        return NewResult();
    }

    [DebuggerStepThrough]
    private ExecResult NewResult(IEnumerable<Maybe<ValueReference>> value) {
        var result = NewResult();
        value.ForEach(
            v => {
                if (v.MatchJust(out var reference)) {
                    result += reference;
                }
            }
        );
        return result;
    }

    public void LogError(BaseNode node, string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "") {
        Diagnostic_Error_Fatal().Message(message).Range(node).Caller(Caller.FromAttributes(file, line, member)).Report();
    }

    public void LogWarning(BaseNode node, string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "") {
        Diagnostic_Warning().Message(message).Range(node).Caller(Caller.FromAttributes(file, line, member)).Report();
    }

    /*private void LogError(BaseNode node, string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "") {
        ErrorWriter.Configure(node?.GetScript(), file, line, member);

        throw new FatalInterpreterException(message, node)
           .WithCaller(Caller.FromAttributes(file, line, member));
    }
    private void LogWarning(BaseNode node, string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "") {
        ErrorWriter.Create(node, file, line, member).LogWarning(message, node);
    }*/
}