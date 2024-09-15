using System.Globalization;
using CSScriptingLang.RuntimeValues.Values;
using SharpX;

namespace CSScriptingLang.RuntimeValues.Types;

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
    Signal   = 1 << 9,
    Struct   = 1 << 10,

    Unit = 1 << 11, // void
    
    // Holds a struct instance of ValueReference
    ValueReference = 1 << 12,
}

public static class RTVTUtil
{
    // return a RTVT value from a c# type, ie `T` = `int` => `RTVT.Int32`
    public static RTVT FromType<T>() => typeof(T) switch {
        { } t when t == typeof(int)                       => RTVT.Int32,
        { } t when t == typeof(long)                      => RTVT.Int64,
        { } t when t == typeof(float)                     => RTVT.Float,
        { } t when t == typeof(double)                    => RTVT.Double,
        { } t when t == typeof(string)                    => RTVT.String,
        { } t when t == typeof(bool)                      => RTVT.Boolean,
        { } t when t == typeof(Dictionary<string, Value>) => RTVT.Object,
        { } t when t == typeof(List<Value>)               => RTVT.Array,
        { } t when t == typeof(Unit)                      => RTVT.Unit,

        _ => RTVT.Null,
    };
}

public static class RuntimeValueTypeExtensions
{
    
    public static RuntimeType RuntimeType(this RTVT   type) => TypeTable.TryGet(type);
    public static RuntimeType RuntimeType(this object type) => TypeTable.GetFromValueType(type);
    public static string      Name(this        RTVT   type) => type.ToString();

    public static string FormattedStringNumber(this RTVT type, object value) => type switch {
        RTVT.Int32  => $"i32({((int) value).ToString(CultureInfo.InvariantCulture)})",
        RTVT.Int64  => $"i64({((long) value).ToString(CultureInfo.InvariantCulture)})",
        RTVT.Float  => $"f32({((float) value).ToString(CultureInfo.InvariantCulture)})",
        RTVT.Double => $"f64({((double) value).ToString(CultureInfo.InvariantCulture)})",
        _           => throw new ArgumentException("Invalid RTVT value for FormattedStringNumber")
    };

    public static object ZeroValue(this RTVT type) => type switch {
        RTVT.Int32    => 0,
        RTVT.Int64    => 0L,
        RTVT.Float    => 0.0f,
        RTVT.Double   => 0.0,
        RTVT.String   => "",
        RTVT.Boolean  => false,
        RTVT.Object   => new Dictionary<string, Value>(),
        RTVT.Function => null,
        RTVT.Array    => new List<Value>(),
        RTVT.Signal   => null,
        RTVT.Struct   => null,
        RTVT.Unit     => Unit.Default,
        _             => throw new ArgumentException("Invalid RTVT value for ZeroValue")
    };

    public static bool IsNumber(this RTVT type) =>
        ((type & RTVT.Int32) == RTVT.Int32) ||
        ((type & RTVT.Int64) == RTVT.Int64) ||
        ((type & RTVT.Float) == RTVT.Float) ||
        ((type & RTVT.Double) == RTVT.Double);

    public static bool IsInt32(this    RTVT type) => ((type & RTVT.Int32) == RTVT.Int32);
    public static bool IsInt64(this    RTVT type) => ((type & RTVT.Int64) == RTVT.Int64);
    public static bool IsFloat(this    RTVT type) => ((type & RTVT.Float) == RTVT.Float);
    public static bool IsDouble(this   RTVT type) => ((type & RTVT.Double) == RTVT.Double);
    public static bool IsString(this   RTVT type) => ((type & RTVT.String) == RTVT.String);
    public static bool IsBoolean(this  RTVT type) => ((type & RTVT.Boolean) == RTVT.Boolean);
    public static bool IsObject(this   RTVT type) => ((type & RTVT.Object) == RTVT.Object);
    public static bool IsFunction(this RTVT type) => ((type & RTVT.Function) == RTVT.Function);
    public static bool IsArray(this    RTVT type) => ((type & RTVT.Array) == RTVT.Array);
    public static bool IsSignal(this   RTVT type) => ((type & RTVT.Signal) == RTVT.Signal);
    public static bool IsStruct(this   RTVT type) => ((type & RTVT.Struct) == RTVT.Struct);
    public static bool IsUnit(this     RTVT type) => ((type & RTVT.Unit) == RTVT.Unit);
}