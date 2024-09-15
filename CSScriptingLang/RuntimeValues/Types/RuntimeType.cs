using CSScriptingLang.Parsing.AST;
using CSScriptingLang.RuntimeValues.Values;

namespace CSScriptingLang.RuntimeValues.Types;

public abstract partial class RuntimeType
{
    /// <summary>
    /// Whether the type is a primitive type or not(like `int`, `string`, `bool`, etc)
    /// </summary>
    public bool IsPrimitive { get; set; }

    /// <summary>
    /// Enum value of the type, for ex `RTVT.String`
    /// </summary>
    public RTVT Type { get; set; }

    /// <summary>
    /// The name of the type, for ex `string`
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The type of the native/c# type, for ex `typeof(string)`
    /// </summary>
    public Type ValueType { get; set; }

    /// <summary>
    /// C# type of the value type, for ex,
    /// - Object is `typeof(Object)`
    /// - Array is `typeof(Array)`
    /// </summary>
    public abstract Type RuntimeValueType { get; }

    /// <summary>
    /// C# "zero" value of the type, for ex,
    /// - Object is new dict,
    /// - Array is new list
    /// </summary>
    public abstract object ZeroValue { get; }

    public BaseNode           LinkedNode         { get; set; }
    public DeclarationContext DeclarationContext => LinkedNode is ITopLevelDeclarationNode node ? node.DeclarationContext : null;

    // Takes any value and converts it to the correct corresponding type
    public virtual object ConvertToNative(object value) {
        throw new NotImplementedException();
    }
}

public class RuntimeTypeInfo_String : RuntimeType
{
    public RuntimeTypeInfo_String() {
        IsPrimitive = true;
        Type        = RTVT.String;
        Name        = "String";
        ValueType   = typeof(string);
    }
    public override Type RuntimeValueType => typeof(ValueString);

    public override object ZeroValue                     => "";
    public override object ConvertToNative(object value) => Convert.ToString(value);
}

public class RuntimeTypeInfo_Boolean : RuntimeType
{
    public RuntimeTypeInfo_Boolean() {
        IsPrimitive = true;
        Type        = RTVT.Boolean;
        Name        = "Boolean";
        ValueType   = typeof(bool);
    }
    public override Type RuntimeValueType => typeof(ValueBoolean);

    public override object ZeroValue => false;

    public override object ConvertToNative(object value) => Convert.ToBoolean(value);
}

public class RuntimeTypeInfo_Null : RuntimeType
{
    public RuntimeTypeInfo_Null() {
        IsPrimitive = true;
        Type        = RTVT.Null;
        Name        = "Null";
        ValueType   = null;
    }

    public override object ZeroValue        => null;
    public override Type   RuntimeValueType => typeof(ValueNull);

    public override object ConvertToNative(object value) => null;
}

public class RuntimeTypeInfo_Object : RuntimeType
{
    public RuntimeType Owner { get; set; }

    public RuntimeTypeInfo_Object() {
        ValueType = typeof(Dictionary<string, BaseValue>);
        Type      = RTVT.Object;
        Name      = "Object";
    }

    // public new RuntimeValue_Object Constructor(params object[] args)
    // => base.Constructor(args) as RuntimeValue_Object;

    public override Type   RuntimeValueType => typeof(ValueObject);
    public override object ZeroValue        => new Dictionary<string, BaseValue>();

    private string _fqn;
    public string FQN {
        get => string.IsNullOrEmpty(_fqn) ? Name : _fqn;
        set => _fqn = value;
    }

    public override object ConvertToNative(object value) => value;
}

public class RuntimeTypeInfo_Signal : RuntimeTypeInfo_Object
{
    public RuntimeTypeInfo_Signal() {
        ValueType = typeof(ValueSignal);
        Type      = RTVT.Signal;
        Name      = "Signal";
    }

    public struct Parameter
    {
        public string      Name;
        public RuntimeType Type;
    }

    public List<Parameter> Parameters { get; } = new();

    public override object ZeroValue                     => new ValueSignal();
    public override Type   RuntimeValueType              => typeof(ValueSignal);
    public override object ConvertToNative(object value) => value;
}

public class RuntimeTypeInfo_Struct : RuntimeTypeInfo_Object
{
    public RuntimeTypeInfo_Struct() {
        ValueType = typeof(ValueStruct);
        Type      = RTVT.Struct;
        Name      = "Struct";
    }

    public override object ZeroValue                     => null;
    public override Type   RuntimeValueType              => typeof(ValueStruct);
    public override object ConvertToNative(object value) => value;
}

public class RuntimeTypeInfo_Array : RuntimeType
{
    public RuntimeTypeInfo_Array() {
        ValueType = typeof(List<BaseValue>);
        Type      = RTVT.Array;
        Name      = "Array";
    }
    public override Type RuntimeValueType => typeof(ValueArray);

    public override object ZeroValue                     => new List<BaseValue>();
    public override object ConvertToNative(object value) => value;
}

public class RuntimeTypeInfo_Function : RuntimeType
{
    public RuntimeType Owner { get; set; }

    public struct Parameter
    {
        public string      Name;
        public RuntimeType Type;
        public bool        IsVariadic { get; set; }
    }

    public List<Parameter> Parameters { get; } = new();

    public RuntimeTypeInfo_Function() {
        ValueType = typeof(ValueFunction);
        Type      = RTVT.Function;
        Name      = "Function";
    }
    public override Type RuntimeValueType => typeof(ValueFunction);

    public override object ZeroValue => StaticTypes.Null;

    public override object ConvertToNative(object value) => value;
}

public class RuntimeTypeInfo_Unit : RuntimeType
{
    public RuntimeTypeInfo_Unit() {
        IsPrimitive = true;
        Type        = RTVT.Unit;
        Name        = "Unit";
        ValueType   = null;
    }

    public override object ZeroValue        => null;
    public override Type   RuntimeValueType => typeof(ValueUnit);

    public override object ConvertToNative(object value) => null;
}