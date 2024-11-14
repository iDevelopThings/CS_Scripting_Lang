using System.Diagnostics;
using System.Globalization;
using CSScriptingLang.Interpreter;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Lexing;
using CSScriptingLang.RuntimeValues.Prototypes;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.Core.Logging;
using Force.DeepCloner;
using ValueType = CSScriptingLang.RuntimeValues.Types.ValueType;

namespace CSScriptingLang.RuntimeValues.Values;

public class ValueDebugView
{
    private readonly Value _value;

    public ValueDebugView(Value value) {
        _value = value;
    }

    public (RTVT type, RTVT fullType) Type => new(_value.Type, _value.FullType);

    public Prototype PrototypeObjectInstance => _value.PrototypeType;
    public Symbol    Symbol                  => _value.Symbol;

    public Value Parent => _value.Prototype;

    public object DataObject => _value.DataObject;

    public object Value => _value.GetUntypedValue();

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    [DebuggerDisplay("Count = {Members.Count}")]
    public KeyValuePair<string, Value>[] Members => _value.Members.ToArray();

    public ExecContext Context => _value._context;
}

[DebuggerDisplay($"{{{nameof(ToDebugString)}(),nq}}")]
[DebuggerTypeProxy(typeof(ValueDebugView))]
public partial class Value : IEquatable<Value>, IComparable<Value>, IIterable
{
    protected static Logger Logger = Logs.Get<Value>();

    private bool Locked { get; set; }

    /** The actual prototype object of the value when created */
    private Value _prototype;
    private Prototype _prototypeType;

    public Prototype PrototypeType {
        // get => _prototypeType ?? TypesTable.For(Type);
        get => Type.Prototype(_prototypeType);
        set {
            if (Locked) {
                throw new InvalidOperationException("Value is locked");
            }

            _prototypeType = value;
        }
    }

    public Value Prototype {
        get {
            return Type.PrototypeProto(Type switch {
                RTVT.ValueReference => _value != null ? ((Value) value).Prototype : null,
                _                   => _prototype,
            });
            /*switch (Type) {
                case RTVT.Array:
                    return ArrayPrototype.Instance.Proto;
                case RTVT.Object:
                    return _prototype ?? ObjectPrototype.Instance.Proto;
                case RTVT.Struct:
                    return _prototype ?? StructPrototype.Instance.Proto;
                case RTVT.Enum:
                    return _prototype ?? EnumPrototype.Instance.Proto;
                case RTVT.EnumMember:
                    return _prototype ?? ObjectPrototype.Instance.Proto;
                case RTVT.Function:
                    return FunctionPrototype.Instance.Proto;
                case RTVT.Signal:
                    return SignalPrototype.Instance.Proto;
                case RTVT.String:
                    return StringPrototype.Instance.Proto;
                case RTVT.Boolean:
                    return BooleanPrototype.Instance.Proto;
                case RTVT.Int32:
                    return Int32Prototype.Instance.Proto;
                case RTVT.Int64:
                    return Int64Prototype.Instance.Proto;
                case RTVT.Float:
                    return FloatPrototype.Instance.Proto;
                case RTVT.Double:
                    return DoublePrototype.Instance.Proto;
                case RTVT.ValueReference:
                    return _value != null ? ((Value) value).Prototype : null;
                case RTVT.Null:
                    return NullPrototype.Instance.Proto;
                case RTVT.Unit:
                    return UnitPrototype.Instance.Proto;

                default:
                    throw new InvalidOperationException($"Cannot get prototype for value of type {Type}");

            }*/
        }
        set {
            if (Locked) {
                throw new InvalidOperationException("Value is locked");
            }

            switch (Type) {
                case RTVT.Enum:
                case RTVT.EnumMember:
                case RTVT.Object:
                case RTVT.Struct: {
                    _prototype = value;
                    return;
                }

                case RTVT.Signal: {
                    if (value == null)
                        throw new InvalidOperationException("Cannot set prototype to null for signal");
                    if (value.Type != RTVT.Signal)
                        throw new InvalidOperationException($"Signal prototypes can only be set to other signal prototypes, got {value.Type}");
                    _prototype = value;
                    return;
                }

                default:
                    throw new InterpreterRuntimeException($"Cannot set prototype for value of type {Type}");
            }
        }
    }

    public object DataObject { get; set; }

    public RTVT Type { get; set; } = RTVT.Null;

    public RTVT FullType => Type switch {
        RTVT.ValueReference => As.ValueReference().Value.Type,
        _                   => Type,
    };

    private Dictionary<string, Value> _members = new();
    public Dictionary<string, Value> Members {
        get => Type switch {
            RTVT.ValueReference => ((ValueReference) _value).Value?.Members,
            _                   => _members,
        };
        set {
            if (Locked) {
                throw new InvalidOperationException("Value is locked");
            }

            if (Type == RTVT.ValueReference) {
                ((ValueReference) _value).Value.Members = value;
                return;
            }

            _members = value;
        }
    }

    public Symbol _symbol;
    public Symbol Symbol {
        get => _symbol ?? PrototypeType?.Symbol;
        set {
            if (Locked) {
                throw new InvalidOperationException("Value is locked");
            }
            _symbol = value;
        }
    }

    private object _value;
    private object value {
        get {
            return Type switch {
                RTVT.Object     => Members,
                RTVT.Struct     => Members,
                RTVT.Enum       => Members,
                RTVT.EnumMember => Members,

                RTVT.ValueReference => ((ValueReference) _value).Value,

                _ => _value,
            };
        }
        set {
            if (Locked) {
                throw new InvalidOperationException("Value is locked");
            }

            if (Type == RTVT.Object || Type == RTVT.Struct) {
                Members = (Dictionary<string, Value>) value;
                return;
            }

            if (Type == RTVT.ValueReference) {
                switch (value) {
                    case ValueReference vr: {
                        _value = vr;
                        return;
                    }
                    case Value v: {
                        if (_value is ValueReference vr) {
                            vr.SetValue(v, true);
                            return;
                        }
                        throw new InvalidOperationException($"Cannot set value of type {value.GetType()}");
                    }
                    case null: {
                        _value = null;
                        return;
                    }
                    default:
                        throw new InvalidOperationException($"Cannot set value of type {value.GetType()}");
                }
            }

            _value = value;
        }
    }
    private object FullValue => Type switch {
        RTVT.ValueReference => As.ValueReference().Value.FullValue,
        _                   => value,
    };

    public Value SetValue(object v) {
        SetValue(v, true);
        return this;
    }

    public bool SetValue(object v, bool throwOnFail) {
        if (Locked) {
            throw new InvalidOperationException("Value is locked");
        }

        if (Type == RTVT.ValueReference) {
            value = (Value) v;
            return true;
        }

        var newValue = v is Value val ? val.value : v;

        if (!RTVTUtil.FromValueType(newValue, out var type, throwOnFail))
            return false;

        Type  = type;
        value = newValue;

        return true;
    }

    public object GetUntypedValue() {
        if (Type == RTVT.ValueReference)
            return As.ValueReference().Value.GetUntypedValue();
        return value;
    }
    public     T      GetUntypedValue<T>() => (T) GetUntypedValue();
    public ref object GetUntypedValueRef() => ref _value;

    public Value Lock(bool locked = true) {
        Locked = locked;
        return this;
    }
    public Value Unlock() => Lock(false);
    public Value UnlockedOp(Action<Value> op) {
        var wasLocked = Locked;
        Locked = false;
        op(this);
        Locked = wasLocked;
        return this;
    }

    public Value Clone() => this.DeepClone();

    public Value GetOrClone() {
        if (PrototypeType?.IsPrimitive ?? false)
            return Clone();

        return this;
    }

    public bool Equals(Value other) => Operator_Equal(other, true);

    bool IEquatable<Value>.Equals(Value other) => Equals(other);

    public override bool Equals(object obj) {
        if (ReferenceEquals(obj, null)) {
            return false;
        }

        return obj is Value other && Equals(other);
    }
    public override int GetHashCode() {
        if (Type == RTVT.ValueReference)
            return As.ValueReference().Value.GetHashCode();
        if (Is.Number) return value.GetHashCode();
        if (Is.String) return As.String().GetHashCode();
        if (Is.Boolean) return As.Bool().GetHashCode();
        if (Is.Array) return As.List().GetHashCode();
        if (Is.Null) return int.MinValue;
        if (Is.Unit) return int.MaxValue;
        if (Is.Function) return As.Fn().GetHashCode();


        if (Is.Object) {
            if (DispatchCall("__hash", [], out var hashValue)) {
                if (!hashValue.Is.Number)
                    throw new InterpreterRuntimeException($"__hash method must return a number, got {hashValue.Type}");

                return hashValue.As.Int();
            }

            return Members.GetHashCode();
        }

        throw new InvalidOperationException($"Cannot get hash code for value of type {Type}");
    }

    public int CompareTo(Value other) {
        if (ReferenceEquals(other, null)) {
            return 1;
        }
        if (ReferenceEquals(this, other)) {
            return 0;
        }
        if (Type != other.Type) {
            return Type.CompareTo(other.Type);
        }
        return value switch {
            int i    => i.CompareTo(other.As.Int()),
            long l   => l.CompareTo(other.As.Int64()),
            float f  => f.CompareTo(other.As.Float()),
            double d => d.CompareTo(other.As.Double()),
            string s => string.Compare(s, other.As.String(), StringComparison.Ordinal),
            bool b   => b.CompareTo(other.As.Bool()),
            _        => throw new InvalidOperationException($"Cannot compare values of type {Type}")
        };
    }

    int IComparable<Value>.CompareTo(Value other) => CompareTo(other);

    public static Value ClassInstance(ExecContext ctx, string prototypeName, params Value[] args) {
        if (ctx == null)
            throw new ArgumentNullException(nameof(ctx));

        if (!TypesTable.TypesByFQN.TryGetValue(prototypeName, out var prototype)) {
            throw new InterpreterRuntimeException($"Could not find prototype for bound object: {prototypeName}");
        }

        var inst = Value.Object(ctx, prototype.PrototypeInstance.Proto);

        return inst;
    }

    public static Value ClassInstance<T>(ExecContext ctx, T instance, string prototypeName) where T : class {
        if (ctx == null)
            throw new ArgumentNullException(nameof(ctx));

        if (!TypesTable.TypesByFQN.TryGetValue(prototypeName, out var prototype)) {
            throw new InterpreterRuntimeException($"Could not find prototype for bound object: {prototypeName}");
        }

        var inst = Value.Object(ctx, prototype.PrototypeInstance.Proto);
        inst.DataObject = instance;

        return inst;
    }
    public static Value ClassInstance<T>(ExecContext ctx, T instance, Prototype prototype) where T : class {
        if (ctx == null)
            throw new ArgumentNullException(nameof(ctx));

        var inst = Value.Object(ctx, prototype.ValueType.PrototypeInstance.Proto);
        inst.DataObject = instance;

        return inst;
    }

    public static WrappedValue Wrapped(ExecContext ctx, object instance, string prototypeName = null) {
        if (ctx == null)
            throw new ArgumentNullException(nameof(ctx));

        ValueType proto = null;
        if (prototypeName != null) {
            TypesTable.TypesByFQN.TryGetValue(prototypeName, out proto);
        }

        return WrappedValue.From(ctx, instance, proto);
    }
    public static WrappedValue<T> Wrapped<T>(ExecContext ctx, T instance, string prototypeName = null) where T : class {
        if (ctx == null)
            throw new ArgumentNullException(nameof(ctx));

        ValueType proto = null;
        if (prototypeName != null) {
            TypesTable.TypesByFQN.TryGetValue(prototypeName, out proto);
        }

        return WrappedValue<T>.From(ctx, instance, proto);
    }

    public Symbol SetSymbol(Symbol symbol) {
        SetMember("symbolName", symbol.Name);
        Symbol = symbol;
        return symbol;
    }
    public Symbol SetSymbol(string name) => SetSymbol(Symbol.For(name));

    public IIterator GetIterator(ExecContext ctx) {
        if (IsEnumerable && Is.Object)
            return new ObjectEnumerableIterator(this, ctx);
        if (IsEnumerable)
            return new EnumerableIterator(this, ctx);
        if (Is.Array)
            return new ArrayIterator(this);
        if (Is.Int32)
            return new NumberRangeIterator<int>(this);
        if (Is.Int64)
            return new NumberRangeIterator<long>(this);
        if (Is.Float)
            return new NumberRangeIterator<float>(this);
        if (Is.Double)
            return new NumberRangeIterator<double>(this);
        if (Is.Object)
            return new ObjectIterator(this);
        if (Is.String)
            return new StringIterator(this);

        throw new InvalidOperationException($"Cannot get iterator for value of type {Type}");
    }

    public virtual string ToDebugString() {
        return $"{Type}(Symbol: {(Symbol?.Name ?? "Undefined")}): " + Type switch {
            RTVT.Null           => "null",
            RTVT.Unit           => "unit",
            RTVT.Int32          => As.Int().ToString(),
            RTVT.Int64          => As.Int64().ToString(),
            RTVT.Float          => As.Float().ToString(CultureInfo.InvariantCulture),
            RTVT.Double         => As.Double().ToString(CultureInfo.InvariantCulture),
            RTVT.String         => As.String(),
            RTVT.Boolean        => As.Bool().ToString(),
            RTVT.Function       => As.Fn().ToString(),
            RTVT.Array          => $"[{string.Join(", ", As.List().Select(v => v.ToDebugString()))}]",
            RTVT.Object         => $"{{ {string.Join(", ", Members.Select(kv => $"{kv.Key}: {kv.Value.ToDebugString()}"))} }}",
            RTVT.Struct         => $"{{ {string.Join(", ", Members.Select(kv => $"{kv.Key}: {kv.Value.ToDebugString()}"))} }}",
            RTVT.Signal         => $"Signal({(As.Signal()?.Name ?? "Undefined")})({As.Signal()?.Listeners?.Count ?? 0} listeners)",
            RTVT.ValueReference => $"Reference Value -> {(As.ValueReference().Value?.ToDebugString() ?? "null")}",
            RTVT.Enum           => $"",
            RTVT.EnumMember     => $"Value -> {Members["value"]?.ToDebugString()}",
            _                   => throw new InvalidOperationException($"Cannot convert value of type {Type} to string")
        };
    }
}