using System.Diagnostics;
using System.Globalization;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.Utils;

namespace CSScriptingLang.RuntimeValues.Values;

[ValueTypeCast<ValueBoolean>()]
[ValueTypeCast<Number>()]
[DebuggerDisplay($"{{{nameof(ToDebugString)}(),nq}}")]
public partial class ValueString : BaseValue<ValueString, string>, IIterable
{
    public override RTVT Type             => RTVT.String;
    
    public new RuntimeTypeInfo_String RuntimeType {
        get => (RuntimeTypeInfo_String) base.RuntimeType;
        set => base.RuntimeType = value;
    }
    public override bool ToBool()      => !string.IsNullOrEmpty(Value);
    public override bool IsZeroValue() => string.IsNullOrEmpty(Value);
    public ValueString() {
        Value = string.Empty;
    }
    public ValueString(string                 value) : base(value ?? string.Empty) { }
    public ValueString(RuntimeTypeInfo_String value) : base(value) { }
    public ValueString(object                 value) : base(Convert.ToString(value)) { }
    
    public static explicit operator ValueString(string value) => new ValueString(value);
    public static explicit operator string(ValueString value) => value.Value;
    
    public override string ToDebugString() => $"{GetType().ToShortName()} \"{Value}\"";

    public IIterator GetIterator() => new StringIterator(Value);
    
    public static object GetNativeZero() => string.Empty;
    
    public static ValueString Make()                             => new();
    public static ValueString Make(RuntimeTypeInfo_String value) => new(value);
    public static ValueString Make(string                 value) => new(value);
    public static ValueString Make(object                 value) => value == null ? Make() : new ValueString((string) value);
}