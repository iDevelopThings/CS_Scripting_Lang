using CSScriptingLang.Interpreter.Bindings;
using CSScriptingLang.RuntimeValues.Prototypes;
using CSScriptingLang.RuntimeValues.Types;

namespace CSScriptingLang.RuntimeValues.Values;

public class ObjectDictionary : Dictionary<string, BaseValue> { }

// [LanguageClassBind("Object")]
public partial class ValueObject : BaseValue<ValueObject, ObjectDictionary>
{
    public override RTVT Type             => RTVT.Object;
    
    public new RuntimeTypeInfo_Object RuntimeType {
        get => (RuntimeTypeInfo_Object) base.RuntimeType;
        set => base.RuntimeType = value;
    }

    public static ValueObject Zero()          => new(new ObjectDictionary());
    public static object      GetNativeZero() => new ObjectDictionary();

    public override bool ToBool()      => true;
    public override bool IsZeroValue() => false;

    public ValueObject() {
        Value = new ObjectDictionary();
    }

    public ValueObject(RuntimeTypeInfo_Object value) : base(value) {
        Value = new ObjectDictionary();

        foreach (var field in value.Fields) {
            SetField(field.Key, field.Value.ValueConstructor());
        }
    }

    public ValueObject(ObjectDictionary value) : base(value) { }

    protected override void OnConstruct() {
        base.OnConstruct();

        // SetPrototype(ObjectPrototype.Proto);
    }
    

    public static explicit operator ValueObject(ObjectDictionary value) => new ValueObject(value);
    public static explicit operator ObjectDictionary(ValueObject value) => value.Value;

    public override ObjectDictionary GetValue() {
        if (GetterProxy != null) {
            return (ObjectDictionary) GetterProxy(this);
        }

        return (ObjectDictionary) Fields;
    }

    public override void SetValue(ObjectDictionary value) {
        if (SetterProxy != null) {
            SetterProxy(this, value);
            return;
        }

        Fields = value;
    }

    // public IIterator GetIterator() => new ObjectIterator(this);

    public static ValueObject Make()                             => new();
    public static ValueObject Make(RuntimeTypeInfo_Object value) => new(value);
    public static ValueObject Make(ObjectDictionary       value) => new(value);
    public static ValueObject Make(object value) {
        return value switch {
            null                      => Make(),
            RuntimeTypeInfo_Object rt => Make(rt),
            ObjectDictionary obj      => Make(obj),
            _                         => throw new ArgumentException($"Unknown value type: {value.GetType()}", nameof(value))
        };
    }

    [LanguageOperator("+")]
    public static int Operator_Add(ValueObject a, ValueObject b) {
        return 0;
    }
}