using CSScriptingLang.Utils;

namespace CSScriptingLang.RuntimeValues.Types;

public abstract class RuntimeTypeInfoNumberBase : RuntimeTypeInfo
{
    public static RuntimeValue Temporary<T>(T value) {
        var rtValue = ObjectPool<RuntimeValue>.Rent();
        rtValue.Set(value, StaticTypes.TypesByValueType[typeof(T)]);
        return rtValue;
    }
}

public abstract class RuntimeTypeInfoNumberBase<TRuntimeType, TNativeType, TRuntimeValueType> : RuntimeTypeInfoNumberBase
    where TRuntimeType : RuntimeTypeInfoNumberBase<TRuntimeType, TNativeType, TRuntimeValueType>, new()
    where TRuntimeValueType : RuntimeValue, new()
{
    public override object ConvertToNative(object value) => (TNativeType) value;

    public override TRuntimeValueType Constructor(params object[] args) {
        var rtValue = ObjectPool<TRuntimeValueType>.Rent();
        rtValue.Set(args.Length > 0 ? args[0] : ZeroValue, this);
        rtValue.OnConstruct(args.Skip(1).ToArray());

        return rtValue;
    }

    public static TRuntimeValueType Temporary(TNativeType value) {
        var rtValue = ObjectPool<TRuntimeValueType>.Rent();
        rtValue.Set(value, StaticTypes.TypesByValueType[typeof(TNativeType)]);
        return rtValue;
    }
}

public class RuntimeTypeInfo_Int32 : RuntimeTypeInfoNumberBase<RuntimeTypeInfo_Int32, int, RuntimeValue>
{
    public RuntimeTypeInfo_Int32() {
        IsPrimitive = true;
        Type        = RTVT.Int32;
        Name        = "Int32";
        ValueType   = typeof(int);
    }

    public override object ZeroValue                     => 0;
    public override object ConvertToNative(object value) => Convert.ToInt32(value);
}

public class RuntimeTypeInfo_Int64 : RuntimeTypeInfoNumberBase<RuntimeTypeInfo_Int64, long, RuntimeValue>
{
    public RuntimeTypeInfo_Int64() {
        IsPrimitive = true;
        Type        = RTVT.Int64;
        Name        = "Int64";
        ValueType   = typeof(long);
    }

    public override object ZeroValue                     => (long) 0;
    public override object ConvertToNative(object value) => Convert.ToInt64(value);
}

public class RuntimeTypeInfo_Float : RuntimeTypeInfoNumberBase<RuntimeTypeInfo_Float, float, RuntimeValue>
{
    public RuntimeTypeInfo_Float() {
        IsPrimitive = true;
        Type        = RTVT.Float;
        Name        = "Float";
        ValueType   = typeof(float);
    }

    public override object ZeroValue                     => 0.0f;
    public override object ConvertToNative(object value) => Convert.ToSingle(value);
}

public class RuntimeTypeInfo_Double : RuntimeTypeInfoNumberBase<RuntimeTypeInfo_Double, double, RuntimeValue>
{
    public RuntimeTypeInfo_Double() {
        IsPrimitive = true;
        Type        = RTVT.Double;
        Name        = "Double";
        ValueType   = typeof(double);
    }

    public override object ZeroValue                     => 0.0;
    public override object ConvertToNative(object value) => Convert.ToDouble(value);
}