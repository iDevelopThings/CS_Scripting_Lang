using System.Runtime.CompilerServices;
using CSScriptingLang.Core.Async;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Execution.Statements;
using CSScriptingLang.Lexing;
using CSScriptingLang.RuntimeValues.Types;

namespace CSScriptingLang.RuntimeValues.Values;

public struct ValueIs(Value v)
{
    private RTVT Type => v.Type switch {
        RTVT.ValueReference => v.As.ValueReference().Value.Type,
        _                   => v.Type,
    };

    public bool Null => Type == RTVT.Null;
    public bool Unit => Type == RTVT.Unit;

    public bool Int32  => (Type & RTVT.Int32) != 0;
    public bool Int64  => (Type & RTVT.Int64) != 0;
    public bool Float  => (Type & RTVT.Float) != 0;
    public bool Double => (Type & RTVT.Double) != 0;
    public bool Number => (Type & RTVT.Number) != 0;

    public bool String     => Type == RTVT.String;
    public bool Boolean    => Type == RTVT.Boolean;
    public bool Object     => Type == RTVT.Object;
    public bool Function   => Type == RTVT.Function;
    public bool Array      => Type == RTVT.Array;
    public bool Signal     => Type == RTVT.Signal;
    public bool Struct     => Type == RTVT.Struct;
    public bool Enum       => Type == RTVT.Enum;
    public bool EnumMember => Type == RTVT.EnumMember;

    public bool ScriptTask => Type == RTVT.Object && v.DataObject is ScriptTask;

    public bool ZeroValue() => Type switch {
        RTVT.Int32    => v.GetUntypedValue<int>() == 0,
        RTVT.Int64    => v.GetUntypedValue<long>() == 0,
        RTVT.Float    => v.GetUntypedValue<float>() == 0,
        RTVT.Double   => v.GetUntypedValue<double>() == 0,
        RTVT.String   => string.IsNullOrEmpty(v.As.String()),
        RTVT.Boolean  => v.GetUntypedValue<bool>() == false,
        RTVT.Object   => v.As.Object().Count == 0,
        RTVT.Array    => v.As.Array().Count == 0,
        RTVT.Function => v.As.Function() == null,
        _             => false,
    };

    public bool IsInstanceGetterFn() {
        if (!Function)
            return false;
        var fn = v.GetUntypedValue() as FnClosure;
        return fn?.Type == FnClosure.FnClosureType.InstanceGetter;
    }

    public bool A(RTVT type) => (Type & type) != 0;

    public bool ThrowIfNot(RTVT type, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "") {
        if ((Type & type) == 0) {
            throw new InterpreterRuntimeException($"Expected type {type} but got {Type}.").WithCaller(file, line, member);
        }

        return true;
    }
}

public struct ValueProxy<T>(Value v)
{
    public T Value {
        get => v.AsNativeType<T>();
        set => v.SetValue(value);
    }
}

public readonly struct ValueAs(Value value)
{
    public int Int32() => value.AsType<int>(RTVT.Int32);
    public int Int()   => Int32();

    public long Int64() => value.AsType<long>(RTVT.Int64);
    public long Long()  => Int64();

    public float  Float()  => value.AsType<float>(RTVT.Float);
    public double Double() => value.AsType<double>(RTVT.Double);
    public double Number() => value.AsType<double>(RTVT.Number);

    public string                    String()   => value.AsType<string>(RTVT.String);
    public bool                      Bool()     => value.AsType<bool>(RTVT.Boolean);
    public FnClosure                 Function() => value.AsType<FnClosure>(RTVT.Function);
    public FnClosure                 Fn()       => Function();
    public List<Value>               Array()    => value.GetUntypedValue<List<Value>>();
    public List<Value>               List()     => Array();
    public Dictionary<string, Value> Object()   => value.AsType<Dictionary<string, Value>>(RTVT.Object);
    public Dictionary<string, Value> Struct()   => value.AsType<Dictionary<string, Value>>(RTVT.Struct);
    public Signal                    Signal()   => value.AsType<Signal>(RTVT.Signal);

    public ValueReference ValueReference() {
        var val = value.GetUntypedValueRef();
        return (ValueReference) val;
    }
}

public partial class Value
{
    public ValueIs       Is         => new(this);
    public ValueAs       As         => new(this);
    public ValueProxy<T> Proxy<T>() => new(this);

    public Value CastTo(RTVT type) => TypeCoercion.CastTo(this, type);
    public Value CastTo<T>()       => TypeCoercion.CastTo<T>(this);
    public Value CastTo(Type t)    => TypeCoercion.CastTo(this, t);

    public T AsNativeType<T>() => (T) AsNativeType(typeof(T));

    public object AsNativeType(Type type) {
        return type switch {
            not null when type == typeof(int)    => AsType(RTVT.Int32),
            not null when type == typeof(long)   => AsType(RTVT.Int64),
            not null when type == typeof(float)  => AsType(RTVT.Float),
            not null when type == typeof(double) => AsType(RTVT.Double),
            not null when type == typeof(string) => AsType(RTVT.String),
            not null when type == typeof(bool)   => AsType(RTVT.Boolean),

            _ => throw new InvalidOperationException($"Cannot convert value to {type}"),
        };
    }
    public T AsType<T>(RTVT vType) {
        var v = AsType(vType);
        if (v == null)
            return default;

        return (T) AsType(vType);
    }
    public object AsType(RTVT vType) {
        var curVal = Type == RTVT.ValueReference ? As.ValueReference().Value.value : value;
        if (curVal == null)
            return null;

        if (FullType == vType)
            return curVal;

        if (TypeCoercion.CastTo(this, vType, out var casted)) {
            return casted.value;
        } else {
            TypeCoercion.CastTo(this, vType);
            throw new InvalidOperationException($"Cannot convert value to {vType}");
        }

#pragma warning disable CS0162 // Unreachable code detected
        switch (FullType) {
            case RTVT.Int32:
            case RTVT.Int64:
            case RTVT.Float:
            case RTVT.Double:
                return vType switch {
                    RTVT.Int32  => FullType == RTVT.Int32 ? curVal : Convert.ChangeType(curVal, typeof(int)),
                    RTVT.Int64  => FullType == RTVT.Int64 ? curVal : Convert.ChangeType(curVal, typeof(long)),
                    RTVT.Float  => FullType == RTVT.Float ? curVal : Convert.ChangeType(curVal, typeof(float)),
                    RTVT.Double => FullType == RTVT.Double ? curVal : Convert.ChangeType(curVal, typeof(double)),
                    RTVT.String => Convert.ChangeType(curVal, typeof(string)),

                    _ => throw new InvalidOperationException($"Cannot convert number to {vType}"),
                };
            case RTVT.String:
                return vType switch {
                    RTVT.String  => curVal,
                    RTVT.Int32   => Convert.ChangeType(curVal, typeof(int)),
                    RTVT.Int64   => Convert.ChangeType(curVal, typeof(long)),
                    RTVT.Float   => Convert.ChangeType(curVal, typeof(float)),
                    RTVT.Double  => Convert.ChangeType(curVal, typeof(double)),
                    RTVT.Boolean => Convert.ChangeType(curVal, typeof(bool)),
                    _            => throw new InvalidOperationException($"Cannot convert string to {vType}")
                };

            case RTVT.Boolean:
                return vType switch {
                    RTVT.Boolean                                          => curVal,
                    RTVT.Int32 or RTVT.Int64 or RTVT.Float or RTVT.Double => (bool) curVal ? 1 : 0,
                    RTVT.String                                           => Convert.ChangeType(curVal, typeof(string)),
                    _                                                     => throw new InvalidOperationException($"Cannot convert boolean to {vType}")
                };
        }
        if (FullType != vType) {
            throw new InvalidOperationException($"Value is not of type {vType} - Lhs={FullType}, Rhs={vType}");
        }

        return curVal;
#pragma warning restore CS0162 // Unreachable code detected
    }

}