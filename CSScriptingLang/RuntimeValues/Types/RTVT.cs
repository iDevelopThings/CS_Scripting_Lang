using System.Globalization;
using CSScriptingLang.RuntimeValues.Prototypes;
using CSScriptingLang.RuntimeValues.Values;
using CSScriptingLang.Utils;
using SharpX;

namespace CSScriptingLang.RuntimeValues.Types;

[Flags]
public enum RTVT
{
    None = 0,

    // Number = 1 << 0,
    Int32  = 1 << 0,
    Int64  = 1 << 1,
    Float  = 1 << 2,
    Double = 1 << 3,
    Number = Int32 | Int64 | Float | Double,

    String  = 1 << 4,
    Boolean = 1 << 5,

    Object     = 1 << 6,
    Function   = 1 << 7,
    Array      = 1 << 8,
    Signal     = 1 << 9,
    Struct     = 1 << 10,
    Enum       = 1 << 11,
    EnumMember = 1 << 12,

    Null = 1 << 13,
    Unit = 1 << 14, // void

    // Holds a struct instance of ValueReference
    ValueReference = 1 << 15,
}

public static class RTVTUtil
{
    public static bool FromValueType(object value, out RTVT val, bool throwOnFail = true) {
        val = FromValueType(value, throwOnFail);
        return val != RTVT.None;
    }

    public static RTVT FromValueType(object value, bool throwOnFail = true) {
        if (value == null)
            return RTVT.Null;

        var t = value.GetType();

        if (value is Value v) {
            return v.FullType;
        }

        return FromType(t, throwOnFail);
    }

    public static RTVT FromType<T>(bool throwOnFail = true) {
        return FromType(typeof(T), throwOnFail);
    }

    public static Prototype Prototype(this RTVT type, Prototype ProvidedPrototype = null) {
        if (TypesTable.IsBindingPrototypes) {
            return null;
        }

        return type switch {
            RTVT.Array          => StaticType.Array(),
            RTVT.Object         => ProvidedPrototype ?? StaticType.Object(),
            RTVT.Struct         => ProvidedPrototype ?? StaticType.Struct(),
            RTVT.Enum           => ProvidedPrototype ?? StaticType.Enum(),
            RTVT.EnumMember     => ProvidedPrototype ?? StaticType.Null(),
            RTVT.Function       => StaticType.Function(),
            RTVT.Signal         => StaticType.Signal(),
            RTVT.String         => StaticType.String(),
            RTVT.Boolean        => StaticType.Boolean(),
            RTVT.Int32          => StaticType.Int32(),
            RTVT.Int64          => StaticType.Int64(),
            RTVT.Float          => StaticType.Float(),
            RTVT.Double         => StaticType.Double(),
            RTVT.ValueReference => ProvidedPrototype,
            RTVT.Unit           => StaticType.Unit(),
            RTVT.Null           => StaticType.Null(),
            _                   => throw new NotImplementedException($"Prototype::For({type}) not implemented"),
        };
    }
    
    public static Value PrototypeProto(this RTVT type, Value ProvidedValue = null) {
        if (TypesTable.IsBindingPrototypes) {
            return null;
        }

        return type switch {
            RTVT.Array          => StaticType.Array().Proto,
            RTVT.Object         => ProvidedValue ?? StaticType.Object().Proto,
            RTVT.Struct         => ProvidedValue ?? StaticType.Struct().Proto,
            RTVT.Enum           => ProvidedValue ?? StaticType.Enum().Proto,
            RTVT.EnumMember     => ProvidedValue ?? StaticType.Null().Proto,
            RTVT.Function       => StaticType.Function().Proto,
            RTVT.Signal         => StaticType.Signal().Proto,
            RTVT.String         => StaticType.String().Proto,
            RTVT.Boolean        => StaticType.Boolean().Proto,
            RTVT.Int32          => StaticType.Int32().Proto,
            RTVT.Int64          => StaticType.Int64().Proto,
            RTVT.Float          => StaticType.Float().Proto,
            RTVT.Double         => StaticType.Double().Proto,
            RTVT.ValueReference => ProvidedValue,
            RTVT.Unit           => StaticType.Unit().Proto,
            RTVT.Null           => StaticType.Null().Proto,
            _                   => throw new NotImplementedException($"Prototype::For({type}) not implemented"),
        };
    }
    
    // return a RTVT value from a c# type, ie `T` = `int` => `RTVT.Int32`
    public static RTVT FromType(Type type, bool throwOnFail = true) {
        var result = type switch {
            not null when type == typeof(int)    => RTVT.Int32,
            not null when type == typeof(long)   => RTVT.Int64,
            not null when type == typeof(float)  => RTVT.Float,
            not null when type == typeof(double) => RTVT.Double,
            not null when type == typeof(string) => RTVT.String,
            not null when type == typeof(bool)   => RTVT.Boolean,
            not null when type == typeof(Unit)   => RTVT.Unit,

            not null when type == typeof(Dictionary<string, Value>) => RTVT.Object,

            not null when type == typeof(Value[])            => RTVT.Array,
            not null when type == typeof(List<Value>)        => RTVT.Array,
            not null when type == typeof(IEnumerable<Value>) => RTVT.Array,

            not null when type == typeof(Interpreter.Execution.Statements.Signal) => RTVT.Signal,
            not null when type == typeof(FnClosure)                               => RTVT.Function,
            not null when type == typeof(FnClosure.StaticFunction)                => RTVT.Function,
            not null when type == typeof(FnClosure.InstanceFunction)              => RTVT.Function,
            not null when type == typeof(FnClosure.InstanceGetterFunction)        => RTVT.Function,
            not null when type == typeof(FnClosure.InterpretedFunction)           => RTVT.Function,

            _ => RTVT.None,
        };

        if (result == RTVT.None && throwOnFail)
            throw new ArgumentException($"Invalid type {type} for RTVT conversion");

        return result;
    }

    public static bool FromType(Type type, out RTVT resultValue, bool throwOnFail = true) {
        resultValue = FromType(type, throwOnFail);
        return resultValue != RTVT.None;
    }

}

public static class RuntimeValueTypeExtensions
{
    public static string Name(this RTVT type) => type.ToString();

    public static string FormattedStringNumber(this RTVT type, object value) => type switch {
        RTVT.Int32  => $"i32({((int) value).ToString(CultureInfo.InvariantCulture)})",
        RTVT.Int64  => $"i64({((long) value).ToString(CultureInfo.InvariantCulture)})",
        RTVT.Float  => $"f32({((float) value).ToString(CultureInfo.InvariantCulture)})",
        RTVT.Double => $"f64({((double) value).ToString(CultureInfo.InvariantCulture)})",
        _           => throw new ArgumentException("Invalid RTVT value for FormattedStringNumber")
    };

    public static string ValueTag(this RTVT type) => type switch {
        RTVT.Int32    => "i32",
        RTVT.Int64    => "i64",
        RTVT.Float    => "f32",
        RTVT.Double   => "f64",
        RTVT.String   => "str",
        RTVT.Boolean  => "bool",
        RTVT.Object   => "obj",
        RTVT.Function => "fn",
        RTVT.Array    => "arr",
        RTVT.Signal   => "sig",
        RTVT.Struct   => "struct",
        RTVT.Unit     => "unit",
        RTVT.Null     => "null",
        _             => "",
    };
    public static string ValueTagWrapped(this RTVT type, bool colored = false, string value = "") {
        var tag = type.ValueTag();
        if (value.Length == 0)
            return tag.ColorIf(colored, AnsiColorCodes.BrightGray);

        tag =  tag.ColorIf(colored, AnsiColorCodes.BrightGray);
        tag += "(".ColorIf(colored, AnsiColorCodes.BrightGray);

        value =  value.ColorIf(colored, AnsiColorCodes.BrightWhite);
        tag   += value;

        tag += ")".ColorIf(colored, AnsiColorCodes.BrightGray);

        return tag;
    }

    public static string FormattedValueString(this RTVT type, object value, bool colored = false) {
        var tag = type.ValueTag();
        if (tag.Length == 0)
            return value.ToString();

        return type switch {
            RTVT.String   => ValueTagWrapped(type, colored, value.ToString()),
            RTVT.Int32    => ValueTagWrapped(type, colored, ((int) value).ToString(CultureInfo.InvariantCulture)),
            RTVT.Int64    => ValueTagWrapped(type, colored, ((long) value).ToString(CultureInfo.InvariantCulture)),
            RTVT.Float    => ValueTagWrapped(type, colored, ((float) value).ToString(CultureInfo.InvariantCulture)),
            RTVT.Double   => ValueTagWrapped(type, colored, ((double) value).ToString(CultureInfo.InvariantCulture)),
            RTVT.Boolean  => ValueTagWrapped(type, colored, ((bool) value).ToString()),
            RTVT.Unit     => ValueTagWrapped(type, colored, "unit"),
            RTVT.Null     => ValueTagWrapped(type, colored, "null"),
            RTVT.Function => ValueTagWrapped(type, colored, value.ToString()),
            RTVT.Signal   => ValueTagWrapped(type, colored, value.ToString()),
            _             => throw new ArgumentException("Invalid RTVT value for FormattedStringNumber")
        };
    }

    public static Prototype Prototype(this RTVT type) {
        var pt = TypesTable.For(type);
        return pt;
    }

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

    /// <summary>
    /// Returns the C# type for the given RTVT, for ex
    /// RTVT.Int32 => typeof(int), RTVT.String => typeof(string)
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static Type GetCSType(this RTVT type) => type.ZeroValue().GetType();

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
    public static bool IsBool(this     RTVT type) => IsBoolean(type);
    public static bool IsBoolean(this  RTVT type) => ((type & RTVT.Boolean) == RTVT.Boolean);
    public static bool IsObject(this   RTVT type) => ((type & RTVT.Object) == RTVT.Object);
    public static bool IsFunction(this RTVT type) => ((type & RTVT.Function) == RTVT.Function);
    public static bool IsArray(this    RTVT type) => ((type & RTVT.Array) == RTVT.Array);
    public static bool IsSignal(this   RTVT type) => ((type & RTVT.Signal) == RTVT.Signal);
    public static bool IsStruct(this   RTVT type) => ((type & RTVT.Struct) == RTVT.Struct);
    public static bool IsUnit(this     RTVT type) => ((type & RTVT.Unit) == RTVT.Unit);
}