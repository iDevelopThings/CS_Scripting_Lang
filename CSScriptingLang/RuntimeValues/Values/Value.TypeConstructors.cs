using System.Diagnostics;
using CSScriptingLang.Common.Extensions;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.Lexing;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.RuntimeValues.Types;

namespace CSScriptingLang.RuntimeValues.Values;

public class FnClosure
{
    public enum FnClosureType
    {
        None,
        Interpreted,
        Instance,
        Static,
        InstanceGetter,
    }

    public FnClosureType             Type        { get; set; }
    public string                    Name        { get; set; }
    public InlineFunctionDeclaration Declaration { get; set; }


    public delegate Value StaticFunction(FunctionExecContext      ctx, params Value[] arguments);
    public delegate Value InstanceFunction(FunctionExecContext    ctx, Value          instance, params Value[] arguments);
    public delegate Value InstanceGetterFunction(ExecContext      ctx, Value          instance);
    public delegate Value InterpretedFunction(FunctionExecContext ctx, Value          instance, params Value[] arguments);

    public StaticFunction         StaticCallable   { get; set; }
    public InstanceFunction       InstanceCallable { get; set; }
    public InstanceGetterFunction InstanceGetter   { get; set; }
    public InterpretedFunction    Interpreted      { get; set; }



    public FnClosure() { }

    public FnClosure(InlineFunctionDeclaration declaration) {
        Declaration = declaration;
        Name        = declaration.Name;
        Type        = FnClosureType.Interpreted;
    }
    public FnClosure(StaticFunction callable, string name = null) {
        StaticCallable = callable;
        Type           = FnClosureType.Static;
        Name           = name ?? "Anonymous Function";
    }
    public FnClosure(InstanceFunction callable, string name = null) {
        InstanceCallable = callable;
        Type             = FnClosureType.Instance;
        Name             = name ?? "Anonymous Function";
    }
    public FnClosure(InstanceGetterFunction callable, string name = null) {
        InstanceGetter = callable;
        Type           = FnClosureType.InstanceGetter;
        Name           = name ?? "Anonymous Function";
    }

    public Value Call(ExecContext ctx, Value instance, params Value[] arguments) {
        return Type switch {
            FnClosureType.Static         => StaticCallable((FunctionExecContext) ctx, arguments),
            FnClosureType.Instance       => InstanceCallable((FunctionExecContext) ctx, instance, arguments),
            FnClosureType.InstanceGetter => InstanceGetter(ctx, instance),
            FnClosureType.Interpreted    => Interpreted((FunctionExecContext) ctx, instance, arguments),
            _                            => throw new InterpreterRuntimeException("Invalid function type")
        };
    }

}

public partial class Value
{
    private ExecContext _context;

    public static Value Unit(params  object[] args) => new() {Type = RTVT.Unit};
    public static Value Null(params  object[] args) => new() {Type = RTVT.Null};
    public static Value True(params  object[] args) => new(true);
    public static Value False(params object[] args) => new(false);


    public Value() {
        Type = RTVT.Null;
    }

    public void InitializeFromType(RTVT type, Value prototype = null) {
        Type = type;

        switch (type) {
            case RTVT.Int32: {
                value = Type.ZeroValue();
                // Prototype = NumberPrototype.Instance;
                break;
            }
            case RTVT.Int64: {
                value = Type.ZeroValue();
                // Prototype = NumberPrototype.Instance;
                break;
            }
            case RTVT.Float: {
                value = Type.ZeroValue();
                // Prototype = NumberPrototype.Instance;
                break;
            }
            case RTVT.Double: {
                value = Type.ZeroValue();
                // Prototype = NumberPrototype.Instance;
                break;
            }
            case RTVT.String: {
                value = Type.ZeroValue();
                // Prototype = StringPrototype.Instance;
                break;
            }
            case RTVT.Boolean: {
                value = Type.ZeroValue();
                // Prototype = BooleanPrototype.Instance;
                break;
            }
            case RTVT.Function: {
                value = Type.ZeroValue();
                // Prototype = FunctionPrototype.Instance;
                break;
            }
            case RTVT.Array: {
                value = Type.ZeroValue();
                // Prototype = ArrayPrototype.Instance;
                break;
            }
            case RTVT.Signal: {
                value = Type.ZeroValue();
                // Prototype = SignalPrototype.Instance;
                break;
            }
            case RTVT.Struct: {
                value = Type.ZeroValue();
                // Prototype = ObjectPrototype.Instance;
                break;
            }
            case RTVT.Object:
                // Prototype = ObjectPrototype.Instance;
                // Object uses `Members` for it's value
                break;
            case RTVT.Unit: {
                value = Type.ZeroValue();
                // Prototype = UnitPrototype.Instance;
                break;
            }
            case RTVT.ValueReference:
            case RTVT.Null:
                value = null;
                break;
            default:
                throw new InterpreterRuntimeException($"Invalid/unsupported/unhandled value type: {type}");
        }

        if (prototype != null) {
            Prototype = prototype;
        }
    }

    protected Value(RTVT type, Value prototype = null) : this() {
        InitializeFromType(type, prototype);
    }
    protected Value(RTVT type, ExecContext ctx, Value prototype = null) : this(type, prototype) {
        _context = ctx ?? throw new ArgumentNullException(nameof(ctx));
    }

    protected Value(int v) : this(RTVT.Int32) {
        value = v;
    }

    protected Value(long v) : this(RTVT.Int64) {
        value = v;
    }

    protected Value(float v) : this(RTVT.Float) {
        value = v;
    }

    protected Value(double v) : this(RTVT.Double) {
        value = v;
    }

    protected Value(string v) : this(RTVT.String) {
        value = v;
    }

    protected Value(FnClosure v) : this(RTVT.Function) {
        value = v;
    }

    protected Value(bool v) : this(RTVT.Boolean) {
        value = v;
    }

    protected Value(IEnumerable<Value> v) : this(RTVT.Array) {
        if (ReferenceEquals(v, null))
            throw new ArgumentNullException(nameof(v));

        As.List().AddRange(v);
    }

    public static Value Make(object value) {
        return value switch {
            int i                                      => Number(i),
            long l                                     => Number(l),
            float f                                    => Number(f),
            double d                                   => Number(d),
            string s                                   => String(s),
            bool b                                     => Boolean(b),
            InlineFunctionDeclaration f                => Function(f),
            FnClosure fn                               => Function(fn),
            IEnumerable<Value> v                       => Array(v),
            IEnumerable<(string, Value)> o             => Object(o),
            IEnumerable<KeyValuePair<string, Value>> o => Object(o),

            RTVT type => Make(type),

            _ => throw new InterpreterRuntimeException("Invalid value type")
        };
    }
    public static Value Make(RTVT type) => new(type);
    
    [DebuggerStepThrough]
    public static bool TryMakeFromName(ExecContext ctx, string name, out Value type, params object[] args) {
        return TypesTable.TryMakeFromName(ctx, name, out type, args);
    }

    public static Value Number(int    value) => new(value);
    public static Value Int32(int     value) => new(value);
    public static Value Number(long   value) => new(value);
    public static Value Int64(long    value) => new(value);
    public static Value Number(float  value) => new(value);
    public static Value Float(float   value) => new(value);
    public static Value Number(double value) => new(value);
    public static Value Double(double value) => new(value);

    public static Value Number(object value) => value switch {
        int i    => new Value(i),
        long l   => new Value(l),
        float f  => new Value(f),
        double d => new Value(d),
        _        => throw new InterpreterRuntimeException("Invalid number type")
    };

    public static Value Boolean(object[] args) => args.Length switch {
        0 => new Value(false),
        1 => new Value((bool) args[0]),
        _ => throw new InterpreterRuntimeException("Invalid arguments")
    };

    public static Value Boolean(bool value) => new(value);

    public static Value String(object value) => value switch {
        string s => new Value(s),
        _        => throw new InterpreterRuntimeException("Invalid string type")
    };
    public static Value String(string value) => new(value);

    public static Value Function(params object[] args) => args.Length switch {
        0 => new Value(new FnClosure()),
        1 => args[0] switch {
            InlineFunctionDeclaration declaration => Function(declaration),
            FnClosure closure                     => Function(closure),
            _                                     => throw new InterpreterRuntimeException("Invalid arguments")
        },
        _ => throw new InterpreterRuntimeException("Invalid arguments")
    };
    public static Value Function(string name, FnClosure.StaticFunction value) {
        if (ReferenceEquals(value, null))
            throw new ArgumentNullException(nameof(value));

        return Function(new FnClosure(value, name));
    }
    public static Value Function(string name, FnClosure.InstanceFunction value) {
        if (ReferenceEquals(value, null))
            throw new ArgumentNullException(nameof(value));

        return Function(new FnClosure(value, name));
    }
    public static Value Function(InlineFunctionDeclaration declaration) {
        if (ReferenceEquals(declaration, null))
            throw new ArgumentNullException(nameof(declaration));

        return Function(new FnClosure(declaration));
    }
    public static Value InstanceGetterFunction(string name, FnClosure.InstanceGetterFunction value) {
        if (ReferenceEquals(value, null))
            throw new ArgumentNullException(nameof(value));

        return Function(new FnClosure(value, name));
    }
    public static Value Function(FnClosure value) => new(value);

    public static Value Object(params object[] args) {
        if (args?.Length == 0 || args is null)
            throw new InterpreterRuntimeException("Invalid arguments");

        var ctx = args.OfType<ExecContext>().FirstOrDefault();

        return args[0] switch {
            IEnumerable<(string, Value)> enumerable               => Object(enumerable, ctx),
            IEnumerable<KeyValuePair<string, Value>> enumerableKv => Object(enumerableKv, ctx),

            _ => throw new InterpreterRuntimeException("Invalid arguments")
        };
    }
    public static Value Object(ExecContext ctx = null) => new(RTVT.Object, ctx);
    public static Value Object(IEnumerable<(string, Value)> entries, ExecContext ctx = null) {
        var value = Object(ctx);
        value.Members.AddRange(entries);
        return value;
    }
    public static Value Object(IEnumerable<KeyValuePair<string, Value>> entries, ExecContext ctx = null) {
        var value = Object(ctx);
        value.Members.AddRange(entries);
        return value;
    }
    public static Value Struct(params object[] args) => args.Length switch {
        1 => args[0] switch {
            ExecContext ctx => Struct(ctx),

            _ => throw new InterpreterRuntimeException("Invalid arguments")
        },
        _ => throw new InterpreterRuntimeException("Invalid arguments"),
    };
    public static Value Struct(ExecContext ctx) => new(RTVT.Struct, ctx);

    public static Value Array(params object[] args) {
        if (args?.Length == 0)
            return Array();
        if (args?.Length == 1 && args[0] is IEnumerable<Value> values)
            return Array(values);
        throw new InterpreterRuntimeException("Invalid arguments");
    }
    public static Value Array(IEnumerable<Value> values = null) => new(values);

    public static Value Signal(params object[] args) {
        if (args?.Length == 0)
            return Signal();
        throw new InterpreterRuntimeException("Invalid arguments");
    }
    public static Value Signal() => new(RTVT.Signal);

    public static Value Reference(ValueReference reference) => new(RTVT.ValueReference) {
        value = reference,
    };



}