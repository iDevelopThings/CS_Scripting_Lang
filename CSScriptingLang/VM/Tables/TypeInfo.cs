using System.Reflection;
using CSScriptingLang.VM.Tables;

namespace CSScriptingLang.VM;

/*
[AttributeUsage(AttributeTargets.Property)]
public class StaticTypeRegistrationAttribute : Attribute
{
    public StaticTypeRegistrationAttribute(string name) {
        Name = name;
    }
    public string Name { get; set; }
}
*/

/*public struct TypeInfo
{
    public RTVT                 Type      { get; }
    public Type                 ValueType { get; }
    public object               ZeroValue { get; }
    public Func<object, object> Converter { get; }

    public TypeInfo(
        RTVT                 type,
        Type                 valueType,
        object               zeroValue = null,
        Func<object, object> converter = null
    ) {
        Type      = type;
        ValueType = valueType;
        ZeroValue = zeroValue;
        Converter = converter;
    }

    [StaticTypeRegistration(nameof(Number))]
    public static TypeInfo Number => new(
        RTVT.Number,
        typeof(double),
        0.0,
        v => Convert.ToDouble(v)
    );

    [StaticTypeRegistration(nameof(String))]
    public static TypeInfo String => new(
        RTVT.String,
        typeof(string),
        "",
        Convert.ToString
    );

    [StaticTypeRegistration(nameof(Boolean))]
    public static TypeInfo Boolean => new(
        RTVT.Boolean,
        typeof(bool),
        false,
        v => Convert.ToBoolean(v)
    );

    [StaticTypeRegistration(nameof(Object))]
    public static TypeInfo Object => new(
        RTVT.Object,
        typeof(Dictionary<string, RuntimeValue>),
        new Dictionary<string, RuntimeValue>(),
        v => v
    );

    [StaticTypeRegistration(nameof(Function))]
    public static TypeInfo Function => new(
        RTVT.Function,
        typeof(RuntimeValue_Function),
        new RuntimeValue_Function(-1),
        v => v
    );

    [StaticTypeRegistration(nameof(Null))]
    public static TypeInfo Null => new(
        RTVT.Null,
        null,
        null,
        v => null
    );

    public static Dictionary<RTVT, TypeInfo> TypeGetterMap = new();
    public static Dictionary<RTVT, Type>     TypeMap       = new();
    public static Dictionary<Type, RTVT>     RtTypeMap     = new();

    public static IEnumerable<TypeInfo> StaticTypes =>
        typeof(TypeInfo).GetProperties(BindingFlags.Public | BindingFlags.Static)
           .Where(p => p.GetCustomAttribute<StaticTypeRegistrationAttribute>() != null)
           .Select(p => (TypeInfo) p.GetValue(null)!);

    static TypeInfo() {
        foreach (var fieldInfo in typeof(TypeInfo).GetProperties(BindingFlags.Public | BindingFlags.Static)) {
            if (fieldInfo.PropertyType == typeof(TypeInfo)) {
                var value = (TypeInfo) fieldInfo.GetValue(null)!;
                TypeMap[value.Type] = value.ValueType;
                if (value.ValueType != null)
                    RtTypeMap[value.ValueType] = value.Type;
                TypeGetterMap[value.Type] = value;
            }
        }
    }

    public static TypeInfo FromType(Type type) {
        if (type == null)
            return Null;

        return type switch {
            {IsValueType: true} when type == typeof(double)                            => Number,
            {IsValueType: true} when type == typeof(bool)                              => Boolean,
            {IsValueType: false} when type == typeof(string)                           => String,
            {IsValueType: false} when type == typeof(Dictionary<string, RuntimeValue>) => Object,
            {IsValueType: false} when type == typeof(RuntimeValue_Function)            => Function,

            _ => throw new ArgumentException($"Cannot convert type {type} to a runtime value type")
        };
    }

    public bool AreSameRtType(TypeInfo type)  => Type == type.Type;
    public bool AreSameRtType(object   value) => Type == FromValue(value).Type;

    public static TypeInfo FromValue(object value) => FromType(value?.GetType());
    public static TypeInfo FromValue<T>(T   value) => FromType(typeof(T));

    public static implicit operator TypeInfo(RTVT type) {
        return TypeGetterMap[type];
    }
    public static implicit operator TypeInfo(Type type) => FromType(type);

    public static implicit operator Type(TypeInfo type) => type.ValueType;
    public static implicit operator RTVT(TypeInfo type) => type.Type;
}*/