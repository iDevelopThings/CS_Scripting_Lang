using System.Globalization;
using System.Runtime.CompilerServices;
using CSScriptingLang.Interpreter.Context;
using SharpX;

namespace CSScriptingLang.RuntimeValues.Values;

public partial class Value
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Value(ValueReference v) => v.Value;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Value(int v) => Number(v);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Value(long v) => Number(v);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Value(float v) => Number(v);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Value(double v) => Number(v);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Value(string v) => String(v);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Value(bool v) => Boolean(v);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Value(FnClosure.StaticFunction v) => Function(null, v);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Value(FnClosure.InstanceFunction v) => Function(null, v);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Value(FnClosure v) => Function(v);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Value(Dictionary<string, Value> v) => Object(v);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Value(List<Value> v) => Array(v);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Value(Unit v) => Unit();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator bool(Value v) => v.IsTruthy();

    public bool IsTruthy() {
        return Is switch {
            {Null: true} or {Unit: true} => false,

            {Boolean : true} => As.Bool(),
            {Int32   : true} => As.Int32() != 0,
            {Int64   : true} => As.Long() != 0,
            {Float   : true} => As.Float() != 0,
            {Double  : true} => As.Double() != 0,
            {String  : true} => string.IsNullOrEmpty(As.String()) == false,
            {Object  : true} => Members.Count > 0,
            {Array   : true} => As.List().Count > 0,
            {Function: true} => As.Fn() != null,

            _ => throw new InvalidCastException($"Cannot convert {Type} to bool"),
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator int(Value v) {
        return v.Is switch {
            {Number: true} => v.As.Int(),

            _ => throw new InvalidCastException($"Cannot convert {v.Type} to int"),
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator long(Value v) {
        return v.Is switch {
            {Number: true} => v.As.Long(),

            _ => throw new InvalidCastException($"Cannot convert {v.Type} to long"),
        };
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator double(Value v) {
        return v.Is switch {
            {Number: true} => v.As.Double(),

            _ => throw new InvalidCastException($"Cannot convert {v.Type} to double"),
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator float(Value v) {
        return v.Is switch {
            {Number: true} => v.As.Float(),

            _ => throw new InvalidCastException($"Cannot convert {v.Type} to float"),
        };
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator string(Value v) {
        return v.Is switch {
            {String : true} => v.As.String(),
            {Int32  : true} => v.As.Int().ToString(),
            {Int64  : true} => v.As.Long().ToString(),
            {Float  : true} => v.As.Float().ToString(CultureInfo.InvariantCulture),
            {Double : true} => v.As.Double().ToString(CultureInfo.InvariantCulture),
            {Boolean: true} => v.As.Bool().ToString(),

            _ => throw new InvalidCastException($"Cannot convert {v.Type} to string"),
        };
    }

}