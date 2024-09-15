using System.Diagnostics;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.Utils;

namespace CSScriptingLang.RuntimeValues.Values;

public partial class ValueNull : BaseValue<ValueNull, SharpX.Unit>
{
    public override RTVT Type             => RTVT.Null;
    public new RuntimeTypeInfo_Null RuntimeType {
        get => (RuntimeTypeInfo_Null) base.RuntimeType;
        set => base.RuntimeType = value;
    }
    public ValueNull() { }
    public ValueNull(SharpX.Unit          value) : base(value) { }
    public ValueNull(object               value) : base(SharpX.Unit.Default) { }
    public ValueNull(RuntimeTypeInfo_Null value) : base(value) { }

    public override bool   ToBool()        => false;
    public override bool   IsZeroValue()   => true;
    public static   object GetNativeZero() => SharpX.Unit.Default;

    public static ValueNull Make()             => new();
    public static ValueNull Make(object value) => new(value);
}

[ValueType<ValueUnit, RuntimeTypeInfo_Unit, SharpX.Unit>(RTVT.Unit)]
[NoGeneratedConversionOperators]
[NoGeneratedMakeFromValue]
[DebuggerDisplay($"{{{nameof(ToDebugString)}(),nq}}")]
public partial class ValueUnit : BaseValue<ValueUnit, object>
{
    public override RTVT Type             => RTVT.Unit;
    public new RuntimeTypeInfo_Unit RuntimeType {
        get => (RuntimeTypeInfo_Unit) base.RuntimeType;
        set => base.RuntimeType = value;
    }
    public ValueUnit() { }
    public ValueUnit(SharpX.Unit          value) : base(value) { }
    public ValueUnit(object               value) : base(SharpX.Unit.Default) { }
    public ValueUnit(RuntimeTypeInfo_Unit value) : base(value) { }

    public override bool ToBool()      => false;
    public override bool IsZeroValue() => true;

    public static object GetNativeZero() => SharpX.Unit.Default;

    public override string ToDebugString() {
        return $"{GetType().ToShortName()} (null/void)";
    }

    public static ValueUnit Make()             => new();
    public static ValueUnit Make(object value) => new(value);
}