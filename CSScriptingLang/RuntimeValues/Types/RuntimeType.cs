using CSScriptingLang.Utils;

namespace CSScriptingLang.RuntimeValues.Types;

public abstract class RuntimeTypeInfo
{
    public TypeTable Table { get; set; }

    public          bool         IsPrimitive { get; set; }
    public          RTVT         Type        { get; set; }
    public          string       Name        { get; set; }
    public          Type         ValueType   { get; set; }
    public abstract RuntimeValue Constructor(params object[] args);
    public abstract object       ZeroValue { get; }


    // Takes any value and converts it to the correct corresponding type
    public virtual object ConvertToNative(object value) {
        throw new NotImplementedException();
    }

    public static RuntimeValue TemporaryFrom<T>(T value) where T : RuntimeValue, new() {
        var rtValue = ObjectPool<T>.Rent();
        rtValue.Set(value.RuntimeType.ZeroValue, value.RuntimeType);
        return rtValue;
    }
}

public abstract class RuntimeTypeInfo<TRuntimeType, TNativeType, TRuntimeValueType> : RuntimeTypeInfo
    where TRuntimeType : RuntimeTypeInfo<TRuntimeType, TNativeType, TRuntimeValueType>, new()
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

public class RuntimeTypeInfo_String : RuntimeTypeInfo<RuntimeTypeInfo_String, string, RuntimeValue>
{
    public RuntimeTypeInfo_String() {
        IsPrimitive = true;
        Type        = RTVT.String;
        Name        = "String";
        ValueType   = typeof(string);
    }

    public override object ZeroValue => "";

    public override object ConvertToNative(object value) => Convert.ToString(value);
}

public class RuntimeTypeInfo_Boolean : RuntimeTypeInfo<RuntimeTypeInfo_Boolean, bool, RuntimeValue>
{
    public RuntimeTypeInfo_Boolean() {
        IsPrimitive = true;
        Type        = RTVT.Boolean;
        Name        = "Boolean";
        ValueType   = typeof(bool);
    }

    public override object ZeroValue => false;

    public override object ConvertToNative(object value) => Convert.ToBoolean(value);
}

public class RuntimeTypeInfo_Null : RuntimeTypeInfo
{
    public RuntimeTypeInfo_Null() {
        IsPrimitive = true;
        Type        = RTVT.Null;
        Name        = "Null";
        ValueType   = null;
    }

    public override RuntimeValue Constructor(params object[] args) => new(this, ZeroValue);
    public override object       ZeroValue                         => null;

    public override object ConvertToNative(object value) => null;
}

public class RuntimeTypeInfo_Object : RuntimeTypeInfo<RuntimeTypeInfo_Object, Dictionary<string, RuntimeValue>, RuntimeValue_Object>
{
    public RuntimeTypeInfo Owner { get; set; }

    public Dictionary<string, RuntimeTypeInfo> Fields { get; } = new();

    public RuntimeTypeInfo_Object() {
        ValueType = typeof(Dictionary<string, RuntimeValue>);
        Type      = RTVT.Object;
        Name      = "Object";
    }

    public RuntimeTypeInfo RegisterField(string name, RuntimeTypeInfo rtType) {
        Fields.TryAdd(name, rtType);

        return rtType;
    }
    public RuntimeTypeInfo RegisterField(string name, RTVT type) {
        var rtType = Table.Get(type);
        if (rtType == null) {
            throw new Exception($"Type {type} not found");
        }

        return RegisterField(name, rtType);
    }

    public override object ZeroValue => new Dictionary<string, RuntimeValue>();

    public override object ConvertToNative(object value) => value;
}

public class RuntimeTypeInfo_Array : RuntimeTypeInfo<RuntimeTypeInfo_Array, List<RuntimeValue>, RuntimeValue_Array>
{
    public RuntimeTypeInfo_Array() {
        ValueType = typeof(List<RuntimeValue>);
        Type      = RTVT.Array;
        Name      = "Array";
    }

    public override object ZeroValue                     => new List<RuntimeValue>();
    public override object ConvertToNative(object value) => value;
}

public class RuntimeTypeInfo_Function : RuntimeTypeInfo<RuntimeTypeInfo_Function, RuntimeValue_Function, RuntimeValue_Function>
{
    public int             Index { get; set; }
    public RuntimeTypeInfo Owner { get; set; }

    public struct Parameter
    {
        public string          Name;
        public RuntimeTypeInfo Type;
    }

    public List<Parameter> Parameters { get; } = new();

    public RuntimeTypeInfo_Function() {
        ValueType = typeof(RuntimeValue_Function);
        Type      = RTVT.Function;
        Name      = "Function";
    }

    public override object ZeroValue => StaticTypes.Null;

    public override object ConvertToNative(object value) => value;
}
