using System.Diagnostics;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.Utils;

namespace CSScriptingLang.RuntimeValues.Values;

[ValueTypeCast<Number>("+", "-", "*", "/", "%")]
[ValueTypeCast<ValueString>("+")]
[ValueMathOperator<int>("+")]
[ValueMathOperator<int>("-")]
[ValueMathOperator<int>("*")]
[ValueMathOperator<int>("/")]
[ValueMathOperator<int>("%")]
[DebuggerDisplay($"{{{nameof(ToDebugString)}(),nq}}")]
public partial class ValueBoolean : BaseValue<ValueBoolean, bool>
{
    public override RTVT Type             => RTVT.Boolean;
    public new RuntimeTypeInfo_Boolean RuntimeType {
        get => (RuntimeTypeInfo_Boolean) base.RuntimeType;
        set => base.RuntimeType = value;
    }
    public ValueBoolean() { }

    public ValueBoolean(bool value) : base(value) { }

    public ValueBoolean(RuntimeTypeInfo_Boolean value) : base(value) { }

    public static explicit operator ValueBoolean(bool value) => new(value);
    public static explicit operator bool(ValueBoolean value) => value.Value;
    
    public override                 bool ToBool()       => Value;
    public override                 bool IsZeroValue()  => Value == false;
    public ValueBoolean(object value) {
        _value = Convert.ToBoolean(value);
    }

    public override string ToDebugString() {
        return $"{GetType().ToShortName()} {(Value ? "true" : "false")}";
    }
    
    public static object GetNativeZero() => false;
    
    public static ValueBoolean Zero()  => new(false);
    public static ValueBoolean True()  => new(true);
    public static ValueBoolean False() => new(false);

    public static ValueBoolean Make()                              => new();
    public static ValueBoolean Make(RuntimeTypeInfo_Boolean value) => new(value);
    public static ValueBoolean Make(bool                    value) => new(value);
    public static ValueBoolean Make(object                  value) => value == null ? Make() : new ValueBoolean((bool) value);
}