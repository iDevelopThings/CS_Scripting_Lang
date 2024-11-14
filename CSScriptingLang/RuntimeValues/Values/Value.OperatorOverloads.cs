using System.Globalization;
using System.Runtime.CompilerServices;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Execution.Statements;
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
    public static implicit operator Signal(Value v) => v.value as Signal;

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
    public static implicit operator int(Value v) => v.Is.Number ? v.GetUntypedValue<int>() : throw new InvalidCastException($"Cannot convert {v.Type} to int");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator long(Value v) => v.Is.Number ? v.GetUntypedValue<long>() : throw new InvalidCastException($"Cannot convert {v.Type} to long");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator double(Value v) => v.Is.Number ? v.GetUntypedValue<double>() : throw new InvalidCastException($"Cannot convert {v.Type} to double");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator float(Value v) => v.Is.Number ? v.GetUntypedValue<float>() : throw new InvalidCastException($"Cannot convert {v.Type} to float");


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator string(Value v) {
        return v.Is switch {
            {String : true}              => v.GetUntypedValue<string>(),
            {Int32  : true}              => v.GetUntypedValue<int>().ToString(),
            {Int64  : true}              => v.GetUntypedValue<long>().ToString(),
            {Float  : true}              => v.GetUntypedValue<float>().ToString(CultureInfo.InvariantCulture),
            {Double : true}              => v.GetUntypedValue<double>().ToString(CultureInfo.InvariantCulture),
            {Boolean: true}              => v.GetUntypedValue<bool>().ToString(),
            {Null: true} or {Unit: true} => null,

            _ => throw new InvalidCastException($"Cannot convert {v.Type} to string"),
        };
    }

}