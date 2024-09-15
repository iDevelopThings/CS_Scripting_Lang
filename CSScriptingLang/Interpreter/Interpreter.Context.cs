using System.Diagnostics;
using System.Runtime.CompilerServices;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Coroutines;
using CSScriptingLang.Interpreter.Modules;
using CSScriptingLang.Lexing;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.Utils;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.RuntimeValues.Values;
using Engine.Engine.Logging;
using SharpX;
using SharpX.Extensions;

namespace CSScriptingLang.Interpreter;

public interface IScopedValueStack
{
    public IEnumerable<object> AllValues { get; }
    public void                Clear();
}

public class ScopedValueStack : Stack<object>
{
    public object Current => Count > 0 ? Peek() : default;

    public UsingCallbackHandle Using(object value) {
        Push(value);

        return new UsingCallbackHandle(() => {
            if (TryPeek(out var peeked) && peeked.Equals(value)) {
                Pop();
            }
        }, value);
    }
}

public class ScopedValueStack<T> : Stack<T>, IScopedValueStack
{
    public static ScopedValueStack<T> Global = new();

    public IEnumerable<object> AllValues => this.ToArray().Cast<object>();

    // private Stack<UsingCallbackHandle> _handles = new();

    public T Current => Count > 0 ? Peek() : default;

    public ScopedValueStack() {
        ValueStackContainer.Register(this, typeof(T));
    }

    /*
    public new void Push(T item) {
        base.Push(item);
    }

    public new T Pop() {
        if(_handles.Count > 0) {
            var handle = _handles.Peek();
            if(handle.Value is T value) {
                _handles.Pop();
                return value;
            }
        }

        return base.Pop();
    }
    public bool TryPop([MaybeNullWhen(false)] out T result) {
        if(_handles.Count > 0) {
            var handle = _handles.Peek();
            if(handle.Value is T value) {
                _handles.Pop();
                result = value;
                return true;
            }
        }

        result = default;
        return base.TryPop(out result);
    }
    */

    public UsingCallbackHandle Using(T value) {
        Push(value);

        return new UsingCallbackHandle(() => {
            if (TryPeek(out var peeked) && peeked.Equals(value)) {
                Pop();
            }
        }, value);
        // var handle = new UsingCallbackHandle(value, () => Pop());
        // _handles.Push(handle);
        // return handle;
    }
}

public class ValueStackContainer
{
    public static Dictionary<Type, IScopedValueStack> ExecutionScopeStacks = new();
    public static void Register<T>(ScopedValueStack<T> stack, Type type) {
        ExecutionScopeStacks[type] = stack;
    }

    public static IEnumerable<KeyValuePair<Type, object>> AllValues {
        get {
            foreach (var stack in ExecutionScopeStacks) {
                foreach (var value in stack.Value.AllValues) {
                    yield return new KeyValuePair<Type, object>(stack.Key, value);
                }
            }
        }
    }

    public static void ClearAll() {
        foreach (var stack in ExecutionScopeStacks.Values) {
            stack.Clear();
        }
    }

    public static void Push<T>(T value) {
        ScopedValueStack<T>.Global.Push(value);
    }

    public static T Pop<T>() {
        return ScopedValueStack<T>.Global.Pop();
    }

    public static T Pop<T>(T value) {
        return ScopedValueStack<T>.Global.Pop();
    }
    public static T Current<T>() {
        return ScopedValueStack<T>.Global.Current;
    }

    public static UsingCallbackHandle Using<T>(T value) {
        return ScopedValueStack<T>.Global.Using(value);
    }

    public static void Clear<T>() {
        ScopedValueStack<T>.Global.Clear();
    }

    public static bool Contains<T>(T value) {
        return ScopedValueStack<T>.Global.Contains(value);
    }

    public static T Peek<T>() {
        return ScopedValueStack<T>.Global.Peek();
    }

    public static T[] ToArray<T>() {
        return ScopedValueStack<T>.Global.ToArray();
    }

    public static int Count<T>() {
        return ScopedValueStack<T>.Global.Count;
    }
}

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

public partial class Interpreter
{
    public static Logger Logger = Logs.Get<Interpreter>(LogLevel.Debug);

    private static ClassScopedTimerInst<Interpreter> Timer = ClassScopedTimerInst<Interpreter>.Create(Logger)
       .SetColorFn(n => n.BoldBrightBlue())
       .SetName("Interpreter");

    public TypeTable             TypeTable  => TypeTable.Current;
    public InterpreterFileSystem FileSystem { get; set; }

    public ModuleResolver ModuleResolver { get; set; }

    public Module Module { get; set; }

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
        if(value.MatchJust(out var reference)) {
            return NewResult(reference);
        }
        return NewResult();
    }
    
    [DebuggerStepThrough]
    private ExecResult NewResult(IEnumerable<Maybe<ValueReference>> value) {
        var result = NewResult();
        value.ForEach(v => {
            if (v.MatchJust(out var reference)) {
                result += reference;
            }
        });
        return result;
    }


    private void LogError(BaseNode node, string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "") {
        ErrorWriter.Configure(node?.GetScript(), file, line, member);

        throw new FatalInterpreterException(message, node)
           .WithCaller(Caller.FromAttributes(file, line, member));
    }
    private void LogWarning(BaseNode node, string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "") {
        ErrorWriter.Create(node, file, line, member).LogWarning(message, node);
    }
}