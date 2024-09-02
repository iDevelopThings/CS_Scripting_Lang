using CSScriptingLang.Core.FileSystem;
using CSScriptingLang.Interpreter.Modules;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.RuntimeValues;
using CSScriptingLang.Utils;
using CSScriptingLang.VM.Tables;
using Engine.Engine.Logging;

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

public class FunctionExecutionFrame : IDisposable
{
    private BaseNode _functionDeclaration;

    public BaseNode FunctionDeclaration {
        get => _functionDeclaration;
        set {
            if (value is InlineFunctionDeclarationNode inlineFunctionDeclaration) {
                InlineFn = inlineFunctionDeclaration;
            }

            if (value is FunctionDeclarationNode functionDeclaration) {
                Fn = functionDeclaration;
            }

            _functionDeclaration = value;
        }
    }

    public InlineFunctionDeclarationNode InlineFn { get; set; } = null;
    public FunctionDeclarationNode       Fn       { get; set; } = null;

    public string Name => Fn?.Name ?? InlineFn?.ToString();

    public List<Symbol> Args   { get; set; } = new();
    public Symbol       Object { get; set; } = null;

    public InterpreterExecutionContext Context { get; set; } = null;

    public BlockNode CurrentBlock { get; set; } = null;

    public BlockNode ReturnBlock { get; set; } = null;
    public bool      HasReturned { get; set; } = false;

    private RuntimeValue _returnValue = null;
    public RuntimeValue ReturnValue {
        get => _returnValue;
        set {
            ReturnBlock  = CurrentBlock;
            HasReturned  = true;
            _returnValue = value;
        }
    }

    public List<DeferStatementNode> DeferStatements { get; set; } = new();

    public void Dispose() {
        Context.Interpreter.PopFunctionFrame(true);
    }
}

public struct ExecResult
{
    private Interpreter interpreter;

    public bool DidSetValue { get; set; }

    public int ValuesPushed { get; set; }

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

    public bool TryPopSymbolOrRTValue(out RuntimeValue value) {
        if (ValuesPushed > 0) {
            return interpreter.TryPopSymbolOrRTValue(out value);
        }

        value = null;
        return false;
    }

    public static ExecResult operator +(ExecResult a, ExecResult b) {
        a.ValuesPushed += b.ValuesPushed;
        a.DidSetValue  |= b.DidSetValue;
        if (b._values != null)
            a.Values.AddRange(b.Values);
        return a;
    }
}

public partial class Interpreter
{
    private Logger Logger = Logs.Get<Interpreter>();

    private bool logPushPopEnabled = false;

    public FileSystem FileSystem { get; set; }

    public Stack<InterpreterExecutionContext> ContextStack = new();

    public Stack<FunctionExecutionFrame> CallStack = new();
    public FunctionExecutionFrame        CurrentFrame => CallStack.TryPeek(out var frame) ? frame : null;

    public InterpreterExecutionContext Context => ContextStack.Peek();

    public SymbolTable Symbols   => Context.SymbolTable;
    public TypeTable   TypeTable => Context.TypeTable;

    public ModuleRegistry ModuleRegistry => Context.ModuleRegistry;
    public Module Module {
        get => Context.Module;
        set => Context.Module = value;
    }

    public ScopedValueStack ValueStack = new();

    public InterpreterExecutionContext PushFrame(object contextObject = null, bool isRoot = false) {
        var ctx = InterpreterExecutionContext.Create(this, isRoot);
        ctx.ContextObject = contextObject;
        ContextStack.Push(ctx);

        InterpreterEvents.OnExecutionScopePushed?.Invoke(ctx);

        return ctx;
    }

    public InterpreterExecutionContext PopFrame() {
        if (ContextStack.Count == 0) {
            Logger.Error($"Attempted to pop symbol frame when there are no more frames to pop");
            return null;
        }

        return Context.Pop();
    }

    public FunctionExecutionFrame PushFunctionFrame(InlineFunctionDeclarationNode declaration, Symbol objectSymbol = null) {
        var frame = new FunctionExecutionFrame {
            FunctionDeclaration = declaration,
            Object              = objectSymbol,
            Context             = Context,
            CurrentBlock        = declaration.Body
        };

        CallStack.Push(frame);

        InterpreterEvents.OnFunctionFramePushed?.Invoke(frame);

        return frame;
    }

    public FunctionExecutionFrame PopFunctionFrame(bool isFromDispose = false) {
        if (!CallStack.TryPeek(out var frame)) {
            Logger.Error($"Attempted to pop function frame when there are no more frames to pop");
            return null;
        }

        if (frame.DeferStatements.Count > 0) {
            foreach (var defer in frame.DeferStatements) {
                Context.Interpreter.Execute(defer);
            }
        }

        InterpreterEvents.OnFunctionFramePopped?.Invoke(frame);

        CallStack.Pop();

        return frame;
    }

    private void LogPushPop(Caller caller, string prefixStr, object value = null) {
        if (!logPushPopEnabled)
            return;

        var valueTypeName = "";
        var stringValue   = "";
        if (value != null) {
            valueTypeName = value.GetType().Name.Split(".").Last();
            stringValue   = value.ToString();
        }

        Logger.Debug($"{prefixStr} '{stringValue}' -> '{valueTypeName}' from {caller}");
    }

    public UsingCallbackHandle PushValue(object value) {
        LogPushPop(Caller.GetFromFrame(2), "Pushing", value);

        ValueStack.Push(value);

        return new UsingCallbackHandle(() => {
            if (ValueStack.TryPeek(out var peeked) && peeked.Equals(value)) {
                ValueStack.Pop();
                LogPushPop(Caller.GetFromFrame(3), "Popping", value);
            } else {
                LogPushPop(Caller.GetFromFrame(3), "Already popped", value);
            }
        }, value);
        // return ValueStack.Using(value);
    }

    public object PopValue(int frameOffset = 2) {
        var popped = ValueStack.Pop();
        LogPushPop(Caller.GetFromFrame(frameOffset), "Popping", popped);
        return popped;
    }

    public T PopValue<T>() => (T) PopValue(3);

    public bool TryPopValue<T>(out T value, bool throwOnFail = true) {
        if (ValueStack.TryPeek(out var peeked) && peeked is T typedValue) {
            ValueStack.Pop();

            value = typedValue;

            LogPushPop(Caller.GetFromFrame(2), "Popping", value);

            return true;
        }

        if (!throwOnFail) {
            value = default;
            return false;
        }

        throw new Exception("Value is not of type T");
    }

    public bool TryPopSymbolAndRTValue(out RuntimeValue value, out Symbol symbol) {
        if (ValueStack.TryPeek(out var peeked)) {
            if (peeked is RuntimeValue rtValue) {
                ValueStack.Pop();
                LogPushPop(Caller.GetFromFrame(2), "Popping", rtValue);
                value  = rtValue;
                symbol = rtValue.Symbol;
                return true;
            }

            if (peeked is Symbol sym) {
                ValueStack.Pop();
                LogPushPop(Caller.GetFromFrame(2), "Popping", sym);
                value  = sym.Value;
                symbol = sym;
                return true;
            }
        }

        value  = null;
        symbol = null;

        return false;
    }
    public bool TryPopSymbolOrRTValue(out RuntimeValue value) {
        if (ValueStack.TryPeek(out var peeked)) {
            if (peeked is RuntimeValue rtValue) {
                ValueStack.Pop();
                LogPushPop(Caller.GetFromFrame(2), "Popping", rtValue);
                value = rtValue;
                return true;
            }

            if (peeked is Symbol symbol) {
                ValueStack.Pop();
                LogPushPop(Caller.GetFromFrame(2), "Popping", symbol);
                value = symbol.Value;
                return true;
            }
        }

        value = null;
        return false;
    }

    public ExecResult NewResult() => new(this);

    /*public UsingCallbackHandle PushValue<T>(T value) {
        if (value is RuntimeValue runtimeValue) {
            return PushValue(runtimeValue);
        }

        LogPushPop(Caller.GetFromFrame(2), true, value);

        return ValueStackContainer.Using(value);
    }
    public T PopValue<T>() {
        if (typeof(T) == typeof(RuntimeValue)) {
            return (T) (object) PopValue();
        }

        var popped = ValueStackContainer.Pop<T>();
        LogPushPop(Caller.GetFromFrame(2), false, popped);
        return popped;
    }*/
}