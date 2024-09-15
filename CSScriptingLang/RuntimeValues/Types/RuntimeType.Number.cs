using CSScriptingLang.RuntimeValues.Values;
using CSScriptingLang.Utils;

namespace CSScriptingLang.RuntimeValues.Types;

public abstract class RuntimeTypeInfoNumberBase : RuntimeType
{
}

public class RuntimeTypeInfo_Int32 : RuntimeTypeInfoNumberBase
{
    public RuntimeTypeInfo_Int32() {
        IsPrimitive = true;
        Type        = RTVT.Int32;
        Name        = "Int32";
        ValueType   = typeof(int);
    }

    public override Type RuntimeValueType => typeof(Number_Int32);

    public override object ZeroValue                     => 0;
    public override object ConvertToNative(object value) => Convert.ToInt32(value);
}

public class RuntimeTypeInfo_Int64 : RuntimeTypeInfoNumberBase
{
    public RuntimeTypeInfo_Int64() {
        IsPrimitive = true;
        Type        = RTVT.Int64;
        Name        = "Int64";
        ValueType   = typeof(long);
    }
    public override Type RuntimeValueType => typeof(Number_Int64);

    public override object ZeroValue                     => (long) 0;
    public override object ConvertToNative(object value) => Convert.ToInt64(value);
}

public class RuntimeTypeInfo_Float : RuntimeTypeInfoNumberBase
{
    public RuntimeTypeInfo_Float() {
        IsPrimitive = true;
        Type        = RTVT.Float;
        Name        = "Float";
        ValueType   = typeof(float);
    }

    public override Type RuntimeValueType => typeof(Number_Float);

    public override object ZeroValue                     => 0.0f;
    public override object ConvertToNative(object value) => Convert.ToSingle(value);
}

public class RuntimeTypeInfo_Double : RuntimeTypeInfoNumberBase
{
    public RuntimeTypeInfo_Double() {
        IsPrimitive = true;
        Type        = RTVT.Double;
        Name        = "Double";
        ValueType   = typeof(double);
    }
    public override Type RuntimeValueType => typeof(Number_Double);

    public override object ZeroValue                     => 0.0;
    public override object ConvertToNative(object value) => Convert.ToDouble(value);
}