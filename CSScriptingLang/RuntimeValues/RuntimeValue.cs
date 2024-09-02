using System.Globalization;
using CSScriptingLang.Interpreter;
using CSScriptingLang.Lexing;
using CSScriptingLang.Utils;
using CSScriptingLang.Utils.CodeWriter;
using CSScriptingLang.VM.Tables;
using Engine.Engine.Logging;

namespace CSScriptingLang.RuntimeValues;

public class RuntimeValue : PooledObject<RuntimeValue>
{
    protected static Logger Logger = Logs.Get<RuntimeValue>();

    public RuntimeTypeInfo RuntimeType { get; set; }
    public RTVT            Type        => RuntimeType.Type;
    public Type            ValueType   => RuntimeType.ValueType;

    public HashSet<object> References     { get; set; } = new();
    public int             ReferenceCount => References.Count;

    public bool HasValue => _value != null;

    protected object _value;
    public object Value {
        get => _value;
        set {
            var valToSet = value;
            if (valToSet is RuntimeValue rtValue) {
                valToSet = rtValue.Value;
            }

            if ((HasValue && TypeTable.AreSameRtType(RuntimeType, valToSet)) || !HasValue) {
                if (!HasValue && RuntimeType == null) {
                    RuntimeType = TypeTable.GetFromValueType(valToSet);
                }

                _value = valToSet;
            } else {
                Console.WriteLine($"Cannot assign value of type {valToSet.GetType()} to a variable of type {Type}");
            }
        }
    }

    public RuntimeValue() { }
    public RuntimeValue(RuntimeTypeInfo type, object value) {
        RuntimeType = type;
        _value      = value;
    }
    public RuntimeValue(object value) {
        Value = value;
    }

    public override void OnReturn() {
        base.OnReturn();
        Reset();
    }

    public RuntimeValue Set(object value) {
        Value = value;
        return this;
    }
    public RuntimeValue Set(object value, RuntimeTypeInfo type) {
        RuntimeType = type;
        Value       = value;
        return this;
    }

    public virtual void OnConstruct(params object[] args) {
        // Console.WriteLine($"Cannot construct value of type {Type} with {args.Length} arguments");
    }

    public void Reset() {
        _value      = null;
        RuntimeType = StaticTypes.Null;
    }

    public static T Rent<T>(params object[] args) where T : RuntimeValue, new() {
        var type = TypeTable.GetFromValueType(typeof(T));
        var ctor = type?.Constructor(args);
        if (ctor != null) {
            return (T) ctor;
        }

        var rtValue = Rent().Set(args[0]);
        rtValue.OnConstruct(args);
        return (T) rtValue;
    }
    public static TRuntimeType Rent<TValueType, TRuntimeType>(params object[] args) where TRuntimeType : RuntimeValue, new() {
        var type = TypeTable.GetFromValueType(typeof(TValueType));
        var ctor = type?.Constructor(args);
        if (ctor != null) {
            if (ctor is TRuntimeType typedCtor)
                return typedCtor;
            throw new InvalidCastException($"Cannot cast {ctor.GetType()} to {typeof(TRuntimeType)}");
        }

        var rtValue = Rent().Set(args[0]);
        rtValue.OnConstruct(args);
        if (rtValue is TRuntimeType typedValue)
            return typedValue;

        throw new InvalidCastException($"Cannot cast {rtValue.GetType()} to {typeof(TRuntimeType)}");
    }

    public static RuntimeValue RentNull() {
        var obj = ObjectPool<RuntimeValue>.Rent();
        obj.Set(null);
        obj.OnConstruct([null]);
        return obj;
    }
    public static RuntimeValue Rent(params object[] args) {
        var type = TypeTable.GetFromValueType(args[0]);
        var ctor = type?.Constructor(args);
        if (ctor != null) {
            return ctor;
        }

        var rtValue = ObjectPool<RuntimeValue>.Rent().Set(args[0]);
        rtValue.OnConstruct(args);
        return rtValue;
    }

    // public static implicit operator RuntimeValue(int                                   value) => Rent(value);
    // public static implicit operator RuntimeValue(double                                value) => Rent(value);
    // public static implicit operator RuntimeValue(string                                value) => Rent(value);
    // public static implicit operator RuntimeValue(bool                                  value) => Rent(value);
    // public static implicit operator RuntimeValue(Dictionary<string, RuntimeValue>      value) => Rent(value);
    // public static implicit operator RuntimeValue(FunctionTable.InlineFunctionTemporary value) => Rent(value);

    public bool AsBool() {
        return Type switch {
            RTVT.Boolean => (bool) Value,
            RTVT.Number  => (double) Value != 0,
            RTVT.String  => !string.IsNullOrEmpty((string) Value),
            RTVT.Object  => Value != null,
            RTVT.Null    => false,
            _            => false
        };
    }
    
    public bool IsNumber => (Type & RTVT.Number) != 0;
    public bool IsString => Type == RTVT.String;

    public T As<T>() {
        if (ValueType == typeof(T))
            return (T) Value;

        if (Value is double d && typeof(T) == typeof(int))
            return (T) (object) (int) d;

        return (T) RuntimeType.ConvertToNative(HasValue ? Value : RuntimeType.ZeroValue);
    }

    public bool Is<T>() {
        if (Value is double && typeof(T) == typeof(int))
            return ValueType == typeof(double) || ValueType == typeof(int);

        return ValueType == typeof(T);
    }

    public RuntimeValue ImplicitCast(RTVT type) {
        var toRtType = type.RuntimeTypeInfo();

        if (Type == type)
            return this;

        var nativeValue = toRtType.ConvertToNative(HasValue ? Value : RuntimeType.ZeroValue);

        return Rent(nativeValue);

        /*var converter = type.RuntimeTypeInfo().Converter;

        return type switch {
            RTVT.Number  => HasValue && Type == RTVT.Number ? this : Rent(converter(HasValue ? Value : TypeInfo.ZeroValue)),
            RTVT.String  => HasValue && Type == RTVT.String ? this : Rent(converter(HasValue ? Value : TypeInfo.ZeroValue)),
            RTVT.Boolean => HasValue && Type == RTVT.Boolean ? this : Rent(converter(HasValue ? Value : TypeInfo.ZeroValue)),
            RTVT.Object  => HasValue && Type == RTVT.Object ? this : Rent(converter(HasValue ? Value : TypeInfo.ZeroValue)),
            _            => Rent(null)
        };*/
    }


    // Helper method to get the highest priority type for conversion
    public static RTVT GetHighestPriorityType(RuntimeTypeInfo leftType, RuntimeTypeInfo rightType) {
        if (leftType.Type == RTVT.String || rightType.Type == RTVT.String)
            return RTVT.String;

        if (leftType.Type == RTVT.Double || rightType.Type == RTVT.Double)
            return RTVT.Double;
        if (leftType.Type == RTVT.Float || rightType.Type == RTVT.Float)
            return RTVT.Float;
        if (leftType.Type == RTVT.Int64 || rightType.Type == RTVT.Int64)
            return RTVT.Int64;
        if (leftType.Type == RTVT.Int32 || rightType.Type == RTVT.Int32)
            return RTVT.Int32;
        if (leftType.Type == RTVT.Boolean || rightType.Type == RTVT.Boolean)
            return RTVT.Boolean;

        throw new ArgumentException($"Cannot find highest priority type for {leftType.Type} and {rightType.Type}");
    }

    public static (RTVT, RuntimeValue, RuntimeValue) CastToHighestPriorityType(RuntimeValue left, RuntimeValue right) {
        var highestPriorityType = GetHighestPriorityType(left.RuntimeType, right.RuntimeType);

        if (highestPriorityType == RTVT.Null)
            throw new ArgumentException($"Cannot find highest priority type for {left.Type} and {right.Type}");

        var leftV = left;
        if (left.RuntimeType.Type != highestPriorityType) {
            leftV = left.ImplicitCast(highestPriorityType);
        }

        var rightV = right;
        if (right.RuntimeType.Type != highestPriorityType) {
            rightV = right.ImplicitCast(highestPriorityType);
        }

        return (highestPriorityType, leftV, rightV);
    }

    public RuntimeValue Apply(Func<RuntimeValue, RuntimeValue> applyFn) {
        var res = applyFn(this);
        return res;
    }
    public T Apply<T>(Func<T, T> applyFn) where T : RuntimeValue {
        var res = applyFn((T) this);
        return res;
    }

    public static RuntimeValue Operation(RuntimeValue a, RuntimeValue b, OperatorType op) {
        // if (!op.IsBinaryArithmetic() && !op.IsComparison() && !op.IsLogical()) {
        //     throw new ArgumentException($"Cannot perform operation {op} on values of type {a.Type} and {b.Type}");
        // }

        RuntimeValue ResultValue = null;

        var (highestType, castedA, castedB) = CastToHighestPriorityType(a, b);

        dynamic left  = castedA.Value;
        dynamic right = castedB.Value;

        ResultValue = op switch {
            OperatorType.Plus     => Rent(left + right),
            OperatorType.Minus    => Rent(left - right),
            OperatorType.Multiply => Rent(left * right),
            OperatorType.Divide => right switch {
                0 => Rent(double.PositiveInfinity),
                _ => Rent(left / right)
            },
            OperatorType.Modulus => Rent(left % right),

            OperatorType.Equals => highestType switch {
                RTVT.String => Rent(string.Compare(left, right, StringComparison.Ordinal) == 0),
                _           => Rent(left == right),
            },

            OperatorType.NotEquals => highestType switch {
                RTVT.String => Rent(string.Compare(left, right, StringComparison.Ordinal) != 0),
                _           => Rent(left != right),
            },

            OperatorType.GreaterThan => highestType switch {
                RTVT.String => Rent(string.Compare(left, right, StringComparison.Ordinal) > 0),
                _           => Rent(left > right),
            },
            OperatorType.GreaterThanOrEqual => highestType switch {
                RTVT.String => Rent(string.Compare(left, right, StringComparison.Ordinal) >= 0),
                _           => Rent(left >= right),
            },

            OperatorType.LessThan => highestType switch {
                RTVT.String => Rent(string.Compare(left, right, StringComparison.Ordinal) < 0),
                _           => Rent(left < right),
            },
            OperatorType.LessThanOrEqual => highestType switch {
                RTVT.String => Rent(string.Compare(left, right, StringComparison.Ordinal) <= 0),
                _           => Rent(left <= right),
            },

            OperatorType.And => Rent(left && right),
            OperatorType.Or  => Rent(left || right),

            OperatorType.Increment => castedA.Apply(value => {
                if (castedA == a) {
                    castedA.Value = left + 1;
                } else {
                    throw new ArgumentException($"Cannot increment value of type {a.Type}");
                }

                return value;
            }),
            OperatorType.Decrement => castedA.Apply(value => {
                if (castedA == a) {
                    castedA.Value = left - 1;
                } else {
                    throw new ArgumentException($"Cannot decrement value of type {a.Type}");
                }

                return value;
            }),

            _ => Rent(null)
        };

        /*if ((a.Type & RTVT.Number) != 0 && (b.Type & RTVT.Number) != 0) {
            ResultValue = op switch {
                OperatorType.Plus     => Rent(left + right),
                OperatorType.Minus    => Rent(left - right),
                OperatorType.Multiply => Rent(left * right),
                OperatorType.Divide   => Rent(left / right),
                OperatorType.Modulus  => Rent(left % right),

                OperatorType.Equals             => Rent(left == right),
                OperatorType.NotEquals          => Rent(left != right),
                OperatorType.GreaterThan        => Rent(left > right),
                OperatorType.LessThan           => Rent(left < right),
                OperatorType.GreaterThanOrEqual => Rent(left >= right),
                OperatorType.LessThanOrEqual    => Rent(left <= right),

                OperatorType.And => Rent(left && right),
                OperatorType.Or  => Rent(left || right),

                _ => Rent(null)
            };
        } else {
            ResultValue = highestType switch {
                RTVT.String => op switch {
                    OperatorType.Plus => Rent((string) left + (string) right),

                    _ => throw new ArgumentException($"Cannot perform operation {op} on values of type {a.Type} and {b.Type}")
                },

                _ => throw new ArgumentException($"Cannot perform operation {op} on values of type {a.Type} and {b.Type}")
            };
        }*/

        if (ResultValue == null) {
            throw new ArgumentException($"Cannot perform operation {op} on values of type {a.Type} and {b.Type}");
        }

        if (op.IsComparison() || op.IsLogical()) {
            ResultValue = Rent(ResultValue.AsBool());
        }

        return ResultValue;
    }

    public static RuntimeValue operator +(RuntimeValue  a, RuntimeValue b) => Operation(a, b, OperatorType.Plus);
    public static RuntimeValue operator -(RuntimeValue  a, RuntimeValue b) => Operation(a, b, OperatorType.Minus);
    public static RuntimeValue operator *(RuntimeValue  a, RuntimeValue b) => Operation(a, b, OperatorType.Multiply);
    public static RuntimeValue operator /(RuntimeValue  a, RuntimeValue b) => Operation(a, b, OperatorType.Divide);
    public static RuntimeValue operator %(RuntimeValue  a, RuntimeValue b) => Operation(a, b, OperatorType.Modulus);
    public static RuntimeValue operator >(RuntimeValue  a, RuntimeValue b) => Operation(a, b, OperatorType.GreaterThan);
    public static RuntimeValue operator <(RuntimeValue  a, RuntimeValue b) => Operation(a, b, OperatorType.LessThan);
    public static RuntimeValue operator >=(RuntimeValue a, RuntimeValue b) => Operation(a, b, OperatorType.GreaterThanOrEqual);
    public static RuntimeValue operator <=(RuntimeValue a, RuntimeValue b) => Operation(a, b, OperatorType.LessThanOrEqual);

    public RuntimeValue GreaterThan(RuntimeValue b) {
        return Type switch {
            RTVT.Number when b.Type == RTVT.Number => Rent((double) Value > (double) b.Value),
            _                                      => NullValueAndWarning($"Cannot compare values of type {Type} and {b.Type}")
        };
    }
    public RuntimeValue LessThan(RuntimeValue b) {
        return Type switch {
            RTVT.Number when b.Type == RTVT.Number => Rent((double) Value < (double) b.Value),
            _                                      => NullValueAndWarning($"Cannot compare values of type {Type} and {b.Type}")
        };
    }

    public RuntimeValue GreaterThanOrEqual(RuntimeValue b) {
        return Type switch {
            RTVT.Number when b.Type == RTVT.Number => Rent((double) Value >= (double) b.Value),
            _                                      => NullValueAndWarning($"Cannot compare values of type {Type} and {b.Type}")
        };
    }
    public RuntimeValue LessThanOrEqual(RuntimeValue b) {
        return Type switch {
            RTVT.Number when b.Type == RTVT.Number => Rent((double) Value <= (double) b.Value),
            _                                      => NullValueAndWarning($"Cannot compare values of type {Type} and {b.Type}")
        };
    }

    public RuntimeValue And(RuntimeValue b) {

        return Type switch {
            RTVT.Boolean => Rent((bool) Value && b.ImplicitCast(RTVT.Boolean).AsBool()),
            RTVT.Number  => Rent((double) Value != 0 && b.ImplicitCast(RTVT.Number).AsBool()),
            RTVT.String  => Rent(!string.IsNullOrEmpty((string) Value) && b.ImplicitCast(RTVT.String).AsBool()),
            RTVT.Null    => Rent(AsBool() && b.ImplicitCast(RTVT.Boolean).AsBool()),

            _ => NullValueAndWarning($"Cannot perform logical AND on values of type {Type} and {b.Type}")
        };
    }

    private static RuntimeValue NullValueAndWarning(string message) {
        Console.WriteLine(message);
        return Rent([null]);
    }

    public RuntimeValue AreEqual(RuntimeValue b) {
        return Type switch {
            RTVT.Number when b.Type == RTVT.Number   => Rent((double) Value == (double) b.Value),
            RTVT.String when b.Type == RTVT.String   => Rent((string) Value == (string) b.Value),
            RTVT.Boolean when b.Type == RTVT.Boolean => Rent((bool) Value == (bool) b.Value),
            _                                        => NullValueAndWarning($"Cannot compare values of type {Type} and {b.Type}")
        };
    }

    public RuntimeValue Not() {
        return Type switch {
            RTVT.Boolean => Rent(!(bool) Value),
            _            => NullValueAndWarning($"Cannot negate value of type {Type}")
        };
    }

    public RuntimeValue Or(RuntimeValue b) {
        return Type switch {
            RTVT.Boolean => Rent((bool) Value || b.ImplicitCast(RTVT.Boolean).AsBool()),
            RTVT.Number  => Rent((double) Value != 0 || b.ImplicitCast(RTVT.Number).AsBool()),
            RTVT.String  => Rent(!string.IsNullOrEmpty((string) Value) || b.ImplicitCast(RTVT.String).AsBool()),
            RTVT.Null    => Rent(AsBool() || b.ImplicitCast(RTVT.Boolean).AsBool()),

            _ => NullValueAndWarning($"Cannot perform logical OR on values of type {Type} and {b.Type}")
        };
    }

    public virtual string Inspect(Writer parentWriter = null) {
        var w = new Writer(parentWriter);

        switch (Type) {
            case RTVT.String:
                w.WriteInline(((string) Value).ApplyColorTags());
                break;
            case RTVT.Number:
                w.WriteInline(((double) Value).ToString(CultureInfo.InvariantCulture));
                break;
            case RTVT.Boolean:
                w.WriteInline(((bool) Value).ToString());
                break;
            case RTVT.Null:
                w.WriteInline("null");
                break;
            default:

                if ((Type & RTVT.Number) != 0) {
                    if ((Type & RTVT.Int32) != 0)
                        w.WriteInline($"{"i32".BoldBrightGray()}({Value.ToString().BoldBrightWhite()})");
                    else if ((Type & RTVT.Int64) != 0)
                        w.WriteInline($"{"i64".BoldBrightGray()}({Value.ToString().BoldBrightWhite()})");
                    else if ((Type & RTVT.Float) != 0)
                        w.WriteInline($"{"f32".BoldBrightGray()}({Value.ToString().BoldBrightWhite()})");
                    else if ((Type & RTVT.Double) != 0)
                        w.WriteInline($"{"f64".BoldBrightGray()}({Value.ToString().BoldBrightWhite()})");
                } else {
                    w.WriteInline($"UNKNOWN VALUE OF TYPE {Type} -> {Value}");
                }

                break;
        }

        return w.ToString();
    }

    public override string ToString() {
        var str = Inspect(null);
        return str;
    }

    public RuntimeValue GetFieldByPath(string path) {
        var parts = path.Split('.');
        var value = this;
        foreach (var part in parts) {
            value = value.GetField(part);
        }

        return value;
    }

    public virtual RuntimeValue GetField(RuntimeValue index) {
        throw new NotImplementedException();

        /*return Type switch {
            RTVT.Object => ((Dictionary<string, RuntimeValue>) Value).TryGetValue(index.As<string>(), out var value) ? value : Rent(null),
            _           => Rent(null)
        };*/
    }

    public virtual RuntimeValue GetField(string name) {
        using var fname = Rent(name);
        return GetField(fname);
    }

    public virtual void SetField(string name, RuntimeValue value) {
        using var fname = Rent(name);
        SetField(fname, value);
    }
    public virtual void SetField(RuntimeValue index, RuntimeValue value) {
        throw new NotImplementedException();

        /*if (Type != RTVT.Object) {
            Console.WriteLine($"Cannot set field {index} on a value of type {Type}");
            return;
        }

        var name = index.As<string>();

        ((Dictionary<string, RuntimeValue>) Value)[name] = value;*/
    }

    public void SetSymbol(Symbol symbol) {
        Symbol = symbol;
    }
    public Symbol Symbol { get; set; }

    public void AddReference(IEnumerable<object> references) {
        if (references == null)
            return;
        
        foreach (var reference in references) {
            AddReference(reference);
        }
    }
    
    public void AddReference(object reference) {
        if (reference == null)
            return;

        /*if (reference is List<object> list) {
            foreach (var item in list) {
                if (item != null)
                    References.Add(item);
            }

            return;
        }*/

        References.Add(reference);
    }
    public void RemoveReference(IEnumerable<object> references) {
        if (references == null)
            return;

        foreach (var reference in references) {
            RemoveReference(reference);
        }
    }
    public void RemoveReference(object reference) {
        if (reference == null)
            return;

        /*if (reference is List<object> list) {
            foreach (var item in list) {
                References.Remove(item);
            }

            return;
        }*/

        References.Remove(reference);
    }
}

public static class RuntimeValueTypeExtensions
{
    public static RuntimeTypeInfo RuntimeTypeInfo(this RTVT type) => TypeTable.GlobalTypeTable.Get(type);
    public static string          Name(this            RTVT type) => type.ToString();
}

[Flags]
public enum RTVT
{
    Null = 0,

    // Number = 1 << 0,
    Int32  = 1 << 0,
    Int64  = 1 << 1,
    Float  = 1 << 2,
    Double = 1 << 3,
    Number = Int32 | Int64 | Float | Double,

    String  = 1 << 4,
    Boolean = 1 << 5,

    Object   = 1 << 6,
    Function = 1 << 7,
    Array    = 1 << 8,
}