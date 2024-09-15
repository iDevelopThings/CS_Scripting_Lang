using CSScriptingLang.Interpreter;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Lexing;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.Utils;
using Engine.Engine.Logging;

namespace CSScriptingLang.RuntimeValues.Values;

public struct TypeName : IEquatable<TypeName>
{
    public string Name { get; set; }

    private string _fqn;
    public string FQN {
        get => string.IsNullOrEmpty(_fqn) ? Name : _fqn;
        set => _fqn = value;
    }

    public static implicit operator TypeName(string name) => new() {Name = name};

    public bool Equals(TypeName other) {
        return _fqn == other._fqn && Name == other.Name;
    }
    public override bool Equals(object obj) {
        return obj is TypeName other && Equals(other);
    }
    public override int GetHashCode() {
        return HashCode.Combine(_fqn, Name);
    }
}

public abstract partial class BaseValue
{
    protected static Logger Logger = Logs.Get<BaseValue>();

    public ValueObject Prototype { get; set; }

    public abstract RTVT Type { get; }

    public RuntimeType RuntimeType { get; set; }

    public BaseValue Outer { get; set; }

    public virtual  bool HasValue => GetUntypedValue() != null;
    public abstract bool IsZeroValue();

    public Func<BaseValue, object>   GetterProxy { get; set; }
    public Action<BaseValue, object> SetterProxy { get; set; }

    public abstract void   SetUntypedValue(object value);
    public abstract object GetUntypedValue();

    static BaseValue() {
        // LoadNativeBindings();
    }
    protected BaseValue() {
        RuntimeType = Type.RuntimeType();
        InitNativeBindings();
        OnConstruct();
    }
    protected BaseValue(object value) : this() {
        SetUntypedValue(value);
        OnConstruct();
    }
    protected BaseValue(RuntimeType runtimeType) {
        RuntimeType = runtimeType;
        InitNativeBindings();
        OnConstruct();
    }

    protected virtual void OnConstruct() { }

    public virtual void SetPrototype(ValueObject value) {
        ArgumentNullException.ThrowIfNull(value, nameof(value));
        Prototype = value;
    }

    public virtual T Value<T>() => (T) GetUntypedValue();

    public virtual bool ToBool() => throw new NotImplementedException();

    public virtual bool IsPrimitive() => this switch {
        Number       => true,
        ValueBoolean => true,
        ValueString  => true,
        _            => false
    };

    public BaseValue Clone<T>() where T : BaseValue => ValueFactory.Clone<T>(this as T);
    public BaseValue Clone()                        => ValueFactory.Clone(this);

    public BaseValue GetOrClone<T>() where T : BaseValue => IsPrimitive() ? ValueFactory.Clone<T>(this as T) : this;
    public BaseValue GetOrClone()                        => IsPrimitive() ? ValueFactory.Clone(this) : this;

    public VariableSymbol Symbol { get; set; }

    public ValueObject GetOuterObject() {
        if (Outer == null)
            return null;

        var obj = Outer;
        while (obj != null && obj is not ValueObject) {
            obj = obj.Outer;
        }

        return obj as ValueObject;
    }

    public static BaseValue ClassInstance(ExecContext ctx, string prototypeName, params BaseValue[] args) {
        if (ctx == null)
            throw new ArgumentNullException(nameof(ctx));


        if (!TypeTable.TryGet(prototypeName, out var prototype))
            throw new InterpreterRuntimeException($"Could not find prototype for bound object: {prototypeName}");

        var inst = ValueFactory.Make(prototype.GetType(), prototype);
        if (inst != null) {
            return inst;
        }

        throw new InterpreterRuntimeException($"Failed to create instance of {prototypeName}");
    }

    public virtual string ToDebugString() {
        return $"{GetType().ToShortName()} RTVT={Type} Value={GetUntypedValue()}";
    }
}

public abstract class BaseValue<T, TNativeType> : BaseValue where T : BaseValue<T, TNativeType>
{
    protected TNativeType _value = default;
    public TNativeType Value {
        get => GetValue();
        set => SetValue(value);
    }

    public virtual TNativeType GetValue() {
        if (GetterProxy != null) {
            return (TNativeType) GetterProxy(this);
        }

        return _value;
    }
    public virtual void SetValue(TNativeType value) {
        if (SetterProxy != null) {
            SetterProxy(this, value);
            return;
        }

        _value = value;
    }

    public override void   SetUntypedValue(object value) => SetValue((TNativeType) value);
    public override object GetUntypedValue()             => GetValue();

    protected BaseValue() : base() {
        Value = default;
    }
    protected BaseValue(TNativeType value) : base(value) {
        Value = value;
    }
    protected BaseValue(RuntimeType runtimeType) : base(runtimeType) {
        Value = default;
    }
}