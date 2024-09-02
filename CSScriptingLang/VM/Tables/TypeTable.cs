using System.Reflection;
using CSScriptingLang.Interpreter;
using CSScriptingLang.RuntimeValues;
using CSScriptingLang.Utils;

namespace CSScriptingLang.VM.Tables;

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

/*public class RuntimeTypeInfo_Number : RuntimeTypeInfoNumberBase<RuntimeTypeInfo_Number, double, RuntimeValue>
{
    public RuntimeTypeInfo_Number() {
        IsPrimitive = true;
        Type        = RTVT.Number;
        Name        = "Number";
        ValueType   = typeof(double);
    }

    public override object ZeroValue => 0.0;

    public override object ConvertToNative(object value) => Convert.ToDouble(value);
}*/

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

public static class StaticTypes
{
    public static RuntimeTypeInfo_Int32    Int32    = new();
    public static RuntimeTypeInfo_Int64    Int64    = new();
    public static RuntimeTypeInfo_Float    Float    = new();
    public static RuntimeTypeInfo_Double   Double   = new();
    public static RuntimeTypeInfo_String   String   = new();
    public static RuntimeTypeInfo_Boolean  Boolean  = new();
    public static RuntimeTypeInfo_Null     Null     = new();
    public static RuntimeTypeInfo_Object   Object   = new();
    public static RuntimeTypeInfo_Function Function = new();
    public static RuntimeTypeInfo_Array    Array    = new();

    public static Dictionary<RTVT, RuntimeTypeInfo> Types { get; } = new() {
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
    };

    public static Dictionary<Type, RuntimeTypeInfo> TypesByValueType { get; } = new() {
        {typeof(int), Int32},
        {typeof(long), Int64},
        {typeof(float), Float},
        {typeof(double), Double},
        {typeof(string), String},
        {typeof(bool), Boolean},
        {typeof(Dictionary<string, RuntimeValue>), Object},
        {typeof(RuntimeValue_Function), Function},
        {typeof(List<RuntimeValue>), Array},
    };
}

public class TypeTable : ScopeAware<TypeTable>
{
    public static TypeTable GlobalTypeTable { get; set; }

    public Dictionary<string, RuntimeTypeInfo> Types { get; } = new();

    public Dictionary<RTVT, List<RuntimeTypeInfo>> TypesByType { get; } = new();

    // This is essentially the raw native value type -> RuntimeTypeInfo mapping
    public Dictionary<Type, RuntimeTypeInfo> TypesByValueType { get; } = new();

    // This is the type info class type -> RuntimeTypeInfo mapping
    // For ex; typeof(RuntimeTypeInfo_Number) -> Number, typeof(RuntimeTypeInfo_Object) -> Object
    public Dictionary<Type, RuntimeTypeInfo> TypesByNativeType { get; } = new();

    public static Dictionary<RTVT, int> UniqueTypeIds { get; } = new();

    public TypeTable() { }
    public TypeTable(InterpreterExecutionContext context, TypeTable parent) : base(context, parent) { }
    public TypeTable(ExecutionContext            context, TypeTable parent) : base(context, parent) { }

    public void RegisterStaticTypes() {
        var staticTypes = typeof(StaticTypes)
           .GetFields(BindingFlags.Public | BindingFlags.Static)
           .Where(p => p.FieldType.IsSubclassOf(typeof(RuntimeTypeInfo)))
           .ToDictionary(p => p.Name, p => p);

        foreach (var pair in staticTypes) {
            var rtType = (RuntimeTypeInfo) pair.Value.GetValue(null)!;
            RegisterType(rtType);
            if (rtType.ValueType != null)
                TypesByValueType[rtType.ValueType] = rtType;

            TypesByNativeType[rtType.GetType()] = rtType;
        }

        /*
        foreach (var typeInfo in TypeInfo.StaticTypes) {
            var rtType = new RuntimeTypeInfo {
                Type      = typeInfo.Type,
                Name      = typeInfo.GetType().GetCustomAttribute<StaticTypeRegistrationAttribute>()!.Name,
                ValueType = typeInfo.ValueType
            };
            RegisterType(rtType);

            if (staticTypes.TryGetValue(rtType.Name, out var prop)) {
                prop.SetValue(null, rtType);

                rtType.IsPrimitive = true;
            }
        }*/
    }

    public RuntimeTypeInfo this[string name] => Get(name);


    public RuntimeTypeInfo Get(Type type) {
        if (type == null)
            return StaticTypes.Null;


        return type switch {
            {IsValueType: true} when type == typeof(int)    => StaticTypes.Int32,
            {IsValueType: true} when type == typeof(long)   => StaticTypes.Int64,
            {IsValueType: true} when type == typeof(double) => StaticTypes.Double,
            {IsValueType: true} when type == typeof(float)  => StaticTypes.Float,
            
            {IsValueType: true} when type == typeof(bool)    => StaticTypes.Boolean,
            {IsValueType: false} when type == typeof(string) => StaticTypes.String,
            // {IsValueType: false} when type == typeof(Dictionary<string, RuntimeValue>) => Object,
            // {IsValueType: false} when type == typeof(RuntimeValue_Function)            => Function,

            _ => throw new ArgumentException($"Cannot convert type {type?.Name} to a runtime value type")
        };
    }
    public RuntimeTypeInfo Get(RTVT type) {
        if (TypesByType.TryGetValue(type, out var types)) {
            return types.First();
        }

        return Parent?.Get(type);
    }
    public RuntimeTypeInfo Get(string name) {
        if (Types.TryGetValue(name, out var type)) {
            return type;
        }

        return Parent?.Get(name);
    }

    public static string GenerateUniqueTypeId(RTVT type) {
        var id = UniqueTypeIds.GetValueOrDefault(type, 0);

        UniqueTypeIds[type] = id + 1;

        return $"{type}_{id}";
    }

    /*public static RuntimeTypeInfo TryGet(string name)
        => GlobalTypeTable?.Get(name);
    public static RuntimeTypeInfo TryGet(RTVT type)
        => TryGet(type.Name());*/

    /*public static Func<RuntimeValue> GetConstructor(string name, params object[] args) {
        var rtType = TryGet(name);
        return rtType != null
            ? () => rtType.Constructor(args)
            : null;
    }
    public static Func<RuntimeValue> GetConstructor(RTVT type, params object[] args)
        => GetConstructor(type.Name(), args);*/

    public RuntimeTypeInfo_Object RegisterObjectType(string name, RuntimeTypeInfo owner) {
        if (string.IsNullOrWhiteSpace(name))
            name = GenerateUniqueTypeId(RTVT.Object);

        var rtType = new RuntimeTypeInfo_Object {
            Name  = name,
            Owner = owner,
        };
        RegisterType(rtType);
        return rtType;
    }
    public RuntimeTypeInfo_Function RegisterFunctionType(string name, int index, RuntimeTypeInfo owner) {
        if (string.IsNullOrWhiteSpace(name))
            name = GenerateUniqueTypeId(RTVT.Function);

        var rtType = new RuntimeTypeInfo_Function {
            Name  = name,
            Index = index,
            Owner = owner
        };
        RegisterType(rtType);
        return rtType;
    }


    private void RegisterType(RuntimeTypeInfo rtType) {
        if (Types.TryAdd(rtType.Name, rtType)) {
            rtType.Table = this;

            TypesByType.GetOrAdd(rtType.Type).Add(rtType);
            return;
        }

        throw new Exception($"Type {rtType.Name} is already registered");
    }

    public RuntimeTypeInfo FromValueType(object value) {
        if (value == null)
            return StaticTypes.Null;

        RuntimeTypeInfo resultType = value switch {
            RuntimeValue rtValue => rtValue.RuntimeType,
            RuntimeTypeInfo info => info,

            Type ty when TypesByValueType.TryGetValue(ty, out var tType)  => tType,
            Type ty when TypesByNativeType.TryGetValue(ty, out var tType) => tType,

            // Type ty when StaticTypes.TypesByValueType.TryGetValue(ty, out var tType) => tType, 

            _ => null
        };


        if (resultType != null)
            return resultType;

        // if (value is Type ty && TypesByValueType.TryGetValue(ty, out var tType))
        // return tType;

        if (TypesByValueType.TryGetValue(value.GetType(), out var rtType))
            return rtType;

        if (Parent?.FromValueType(value) is { } parentType)
            return parentType;

        return Get(value.GetType());
    }

    public static RuntimeTypeInfo GetFromValueType(object value) {
        return GlobalTypeTable.FromValueType(value);
    }


    public static bool AreSameRtType(RuntimeTypeInfo a, RuntimeTypeInfo b) => a.Type == b.Type;
    public static bool AreSameRtType(RuntimeTypeInfo a, object          b) => a.Type == GetFromValueType(b).Type;
}