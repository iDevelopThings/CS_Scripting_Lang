using System.Diagnostics;
using System.Globalization;
using CSScriptingLang.Interpreter;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Lexing;
using CSScriptingLang.RuntimeValues.Prototypes;
using CSScriptingLang.RuntimeValues.Types;
using Engine.Engine.Logging;
using Force.DeepCloner;

namespace CSScriptingLang.RuntimeValues.Values;

[DebuggerDisplay($"{{{nameof(ToDebugString)}(),nq}}")]
public partial class Value : IEquatable<Value>, IComparable<Value>, IIterable
{
    protected static Logger Logger = Logs.Get<Value>();

    private bool Locked { get; set; } = false;

    /** The actual prototype object of the value when created */
    private Value _prototype;

    public Prototype PrototypeType {
        get => TypesTable.For(Type);
    }

    public Value Prototype {
        get {
            switch (Type) {
                case RTVT.Array:
                    return ArrayPrototype.Instance.Proto;
                case RTVT.Object:
                    return _prototype ?? ObjectPrototype.Instance.Proto;
                case RTVT.Struct:
                    return _prototype ?? StructPrototype.Instance.Proto;
                case RTVT.Function:
                    return FunctionPrototype.Instance.Proto;
                case RTVT.String:
                    return StringPrototype.Instance.Proto;
                case RTVT.Boolean:
                    return BooleanPrototype.Instance.Proto;
                case RTVT.Int32:
                case RTVT.Int64:
                case RTVT.Float:
                case RTVT.Double:
                    return NumberPrototype.Instance.Proto;
                case RTVT.ValueReference:
                    return _value != null ? ((Value) value).Prototype : null;
                case RTVT.Null:
                    return NullPrototype.Instance.Proto;
                case RTVT.Unit:
                    return UnitPrototype.Instance.Proto;
                default:
                    throw new InvalidOperationException($"Cannot get prototype for value of type {Type}");

            }
            /*if (_prototype == null)
                return TypesTable.For(Type)?.Proto;

            return _prototype;*/
        }
        set {
            if (Locked) {
                throw new InvalidOperationException("Value is locked");
            }

            if (Type == RTVT.Object) {
                _prototype = value;
                return;
            }

            throw new InvalidOperationException($"Cannot set prototype for value of type {Type}");
        }
    }

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

    public Symbol Symbol => PrototypeType?.Symbol;

    private object _value = null;
    private object value {
        get {
            return Type switch {
                RTVT.Object         => Members,
                RTVT.ValueReference => ((ValueReference) _value).Value,

                _ => _value,
            };
        }
        set {
            if (Locked) {
                throw new InvalidOperationException("Value is locked");
            }

            if (Type == RTVT.Object) {
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
                        if (_value != null && _value is ValueReference vr) {
                            vr.SetValue(v);
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
        switch (newValue) {
            case int i:
                Type = RTVT.Int32;
                break;
            case long l:
                Type = RTVT.Int64;
                break;
            case float f:
                Type = RTVT.Float;
                break;
            case double d:
                Type = RTVT.Double;
                break;
            case string s:
                Type = RTVT.String;
                break;
            case bool b:
                Type = RTVT.Boolean;
                break;
            case null:
                Type = RTVT.Null;
                break;
            case FnClosure fn:
                Type = RTVT.Function;
                break;
            case IEnumerable<Value> vv:
                Type = RTVT.Array;
                break;
            case Dictionary<string, Value> obj:
                Type = RTVT.Object;
                break;
            case Value vv:
                Type = vv.Type;
                break;
            default: {
                if (throwOnFail) {
                    throw new InvalidOperationException($"Cannot set value of type {v.GetType()}");
                }

                return false;
            }
        }


        value = newValue;

        return true;
    }

    public     object GetUntypedValue()    => value;
    public ref object GetUntypedValueRef() => ref _value;

    public Value Lock(bool locked = true) {
        Locked = locked;
        return this;
    }

    public Value GetOrClone() {
        if (PrototypeType?.IsPrimitive ?? false)
            return this.DeepClone();

        return this;
    }

    public bool Equals(Value other) => Operator_Equal(other);

    bool IEquatable<Value>.Equals(Value other) => Equals(other);

    public override bool Equals(object obj) {
        if (ReferenceEquals(obj, null)) {
            return false;
        }

        return obj is Value other && Equals(other);
    }
    public override int GetHashCode() {
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

        var inst = Value.Object(ctx);
        inst.Prototype = prototype;

        return inst;
    }

    public Symbol SetSymbol(string name) {
        var symbol = Symbol.For(name);
        SetMember("symbolName", String(name));
        return symbol;
    }

    public IIterator GetIterator() {
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

    public string ToDebugString() {
        return $"{Type}: " + Type switch {
            RTVT.Null     => "null",
            RTVT.Unit     => "unit",
            RTVT.Int32    => As.Int().ToString(),
            RTVT.Int64    => As.Int64().ToString(),
            RTVT.Float    => As.Float().ToString(CultureInfo.InvariantCulture),
            RTVT.Double   => As.Double().ToString(CultureInfo.InvariantCulture),
            RTVT.String   => As.String(),
            RTVT.Boolean  => As.Bool().ToString(),
            RTVT.Function => As.Fn().ToString(),
            RTVT.Array    => $"[{string.Join(", ", As.List().Select(v => v.ToDebugString()))}]",
            RTVT.Object   => $"{{ {string.Join(", ", Members.Select(kv => $"{kv.Key}: {kv.Value.ToDebugString()}"))} }}",
            _             => throw new InvalidOperationException($"Cannot convert value of type {Type} to string")
        };
    }
}