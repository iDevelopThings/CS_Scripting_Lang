using CSScriptingLang.RuntimeValues.Types;

namespace CSScriptingLang.RuntimeValues.Values;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ValueTypeAttribute : Attribute
{
    public RTVT Type             { get; set; }
    public Type RuntimeValueType { get; set; }
    public Type ValueType        { get; set; }

    public ValueTypeAttribute(RTVT type, Type runtimeValueType, Type valueType) {
        Type             = type;
        RuntimeValueType = runtimeValueType;
        ValueType        = valueType;
    }
}


[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class NoGeneratedConversionOperatorsAttribute : Attribute { }
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class NoGeneratedMakeFromValueAttribute : Attribute { }



[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ValueTypeAttribute<TClassType, TRuntimeType, TValueType> : ValueTypeAttribute
{
    public ValueTypeAttribute(RTVT type) : base(type, typeof(TRuntimeType), typeof(TValueType)) { }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ValueMathOperator<TConvertTo> : Attribute
{
    public string Operator { get; set; }

    public ValueMathOperator() { }
    public ValueMathOperator(string op) {
        Operator = op;
    }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ValueTypeCastAttribute<TCastToType> : Attribute
{
    public Type     CastToType    { get; set; } = typeof(TCastToType);
    public string[] MathOperators { get; set; }

    public ValueTypeCastAttribute() { }
    public ValueTypeCastAttribute(params string[] mathOperators) {
        MathOperators = mathOperators;
    }
}