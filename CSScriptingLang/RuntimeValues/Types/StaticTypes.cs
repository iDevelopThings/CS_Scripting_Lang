using CSScriptingLang.RuntimeValues.Prototypes;
using CSScriptingLang.RuntimeValues.Values;

namespace CSScriptingLang.RuntimeValues.Types;

[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
public class StaticTypeAliasAttribute : Attribute
{
    public string[] Aliases { get; set; }
    public StaticTypeAliasAttribute(params string[] aliases) {
        Aliases = aliases;
    }
}


public static class StaticTypes
{
    [StaticTypeAlias("int", "i32")]
    public static RuntimeTypeInfo_Int32 Int32 = new();

    [StaticTypeAlias("long", "i64")]
    public static RuntimeTypeInfo_Int64 Int64 = new();

    [StaticTypeAlias("float", "f32")]
    public static RuntimeTypeInfo_Float Float = new();

    [StaticTypeAlias("double", "f64")]
    public static RuntimeTypeInfo_Double Double = new();

    [StaticTypeAlias("string", "str")]
    public static RuntimeTypeInfo_String String = new();

    [StaticTypeAlias("bool", "boolean")]
    public static RuntimeTypeInfo_Boolean Boolean = new();

    [StaticTypeAlias("void", "unit")]
    public static RuntimeTypeInfo_Unit Unit = new();

    public static RuntimeTypeInfo_Null     Null     = new();
    public static RuntimeTypeInfo_Object   Object   = new();
    public static RuntimeTypeInfo_Function Function = new();
    public static RuntimeTypeInfo_Array    Array    = new();
    public static RuntimeTypeInfo_Signal   Signal   = new();

    public static Dictionary<RTVT, RuntimeType> Types { get; } = new() {
        {RTVT.Int32, Int32},
        {RTVT.Int64, Int64},
        {RTVT.Float, Float},
        {RTVT.Double, Double},
        {RTVT.String, String},
        {RTVT.Boolean, Boolean},
        {RTVT.Null, Null},
        {RTVT.Object, Object},
        {RTVT.Function, Function},
        {RTVT.Array, Array},
        {RTVT.Signal, Signal},
        {RTVT.Unit, Unit},
    };

    public static Dictionary<Type, RuntimeType> TypesByValueType { get; } = new() {
        {typeof(int), Int32},
        {typeof(long), Int64},
        {typeof(float), Float},
        {typeof(double), Double},
        {typeof(string), String},
        {typeof(bool), Boolean},
        {typeof(Dictionary<string, BaseValue>), Object},
        {typeof(ValueFunction), Function},
        {typeof(List<BaseValue>), Array},
        {typeof(ValueSignal), Signal},
    };
}