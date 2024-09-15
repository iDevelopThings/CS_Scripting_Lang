using System.Diagnostics;
using System.Globalization;
using CSScriptingLang.RuntimeValues.Prototypes;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.Utils;

namespace CSScriptingLang.RuntimeValues.Values;

[ValueTypeCastAttribute<Number>("+", "-", "*", "/", "%", "++", "--")]
[ValueTypeCastAttribute<ValueBoolean>("+", "-", "*", "/", "%", "++", "--")]
[ValueTypeCastAttribute<ValueString>("+")]
[DebuggerDisplay($"{{{nameof(ToDebugString)}(),nq}}")]
public abstract class Number : BaseValue, IIterable
{
    public override RTVT Type => RTVT.Number;

    public static object GetNativeZero() => 0;

    public abstract string ValueAsString();

    public virtual IIterator GetIterator() {
        throw new NotImplementedException();
    }
}

public abstract class Number<T> : Number where T : struct
{

    public new static object GetNativeZero() => default(T);
    public override   bool   IsZeroValue()   => Value.Equals(default(T));

    protected T _value;
    public T Value {
        get => GetValue();
        set => SetValue(value);
    }

    public override void   SetUntypedValue(object value) => SetValue((T) value);
    public override object GetUntypedValue()             => GetValue();

    public virtual T GetValue() {
        if (GetterProxy != null) {
            return (T) GetterProxy(this);
        }

        return _value;
    }
    public virtual void SetValue(T value) {
        if (SetterProxy != null) {
            SetterProxy(this, value);
            return;
        }

        _value = value;
    }

    protected Number() { }
    protected Number(T value) {
        _value = value;
    }
    protected Number(object value) {
        _value = (T) Convert.ChangeType(value, typeof(T));
    }

    protected override void OnConstruct() {
        base.OnConstruct();
        // SetPrototype(NumberPrototype.Proto);
    }

    public override bool ToBool() => Convert.ToBoolean(_value);

    public override string ValueAsString() {
        return _value.ToString();
    }

    public void Increment() {
        dynamic val = _value;
        _value = val + 1;
    }
    public void Decrement() {
        dynamic val = _value;
        _value = val - 1;
    }
}

[ValueType<Number_Int32, RuntimeTypeInfo_Int32, int>(RTVT.Int32)]
public partial class Number_Int32 : Number<int>
{
    public override RTVT Type             => RTVT.Int32;

    public new RuntimeTypeInfo_Int32 RuntimeType {
        get => (RuntimeTypeInfo_Int32) base.RuntimeType;
        set => base.RuntimeType = value;
    }

    public static Number_Int32 Zero() => new(0);

    public Number_Int32() { }
    public Number_Int32(int                   value) : base(value) { }
    public Number_Int32(RuntimeTypeInfo_Int32 value) : base(value) { }
    public Number_Int32(object value) {
        if (value is string str) {
            _value = int.Parse(str, CultureInfo.InvariantCulture);
            return;
        }

        _value = Convert.ToInt32(value);
    }

    protected override void OnConstruct() {
        base.OnConstruct();
    }

    public static explicit operator Number_Int32(int value) => new Number_Int32(value);
    public static explicit operator int(Number_Int32 value) => value.Value;


    public override string ToDebugString() => $"{GetType().ToShortName()} i32({Value})";

    // public override IIterator GetIterator() => new NumberRangeIterator<int>(this);


    public static Number_Int32 Make()                            => new();
    public static Number_Int32 Make(RuntimeTypeInfo_Int32 value) => new(value);
    public static Number_Int32 Make(int                   value) => new(value);
    public static Number_Int32 Make(object                value) => value == null ? Make() : new Number_Int32((int) value);
}

[ValueType<Number_Int64, RuntimeTypeInfo_Int64, long>(RTVT.Int64)]
public partial class Number_Int64 : Number<long>
{
    public override RTVT Type             => RTVT.Int64;
    public new RuntimeTypeInfo_Int64 RuntimeType {
        get => (RuntimeTypeInfo_Int64) base.RuntimeType;
        set => base.RuntimeType = value;
    }

    public Number_Int64() { }
    public Number_Int64(object value) {
        _value = Convert.ToInt64(value);
    }
    public Number_Int64(long                          value) : base(value) { }
    public Number_Int64(RuntimeTypeInfo_Int64         value) : base(value) { }
    public static explicit operator Number_Int64(long value) => new Number_Int64(value);
    public static explicit operator long(Number_Int64 value) => value.Value;

    public override string ToDebugString() => $"{GetType().ToShortName()} i64({Value})";

    // public override IIterator GetIterator() => new NumberRangeIterator<long>(this);


    public static Number_Int64 Make()                            => new();
    public static Number_Int64 Make(RuntimeTypeInfo_Int64 value) => new(value);
    public static Number_Int64 Make(long                  value) => new(value);
    public static Number_Int64 Make(object                value) => value == null ? Make() : new Number_Int64((long) value);
}

[ValueType<Number_Float, RuntimeTypeInfo_Float, float>(RTVT.Float)]
public partial class Number_Float : Number<float>
{
    public override RTVT Type             => RTVT.Float;
    public new RuntimeTypeInfo_Float RuntimeType {
        get => (RuntimeTypeInfo_Float) base.RuntimeType;
        set => base.RuntimeType = value;
    }

    public Number_Float() { }
    public Number_Float(object value) {
        _value = Convert.ToSingle(value);
    }
    public Number_Float(float                 value) : base(value) { }
    public Number_Float(RuntimeTypeInfo_Float value) : base(value) { }

    protected override void OnConstruct() {
        base.OnConstruct();
    }

    public static explicit operator Number_Float(float value) => new Number_Float(value);
    public static explicit operator float(Number_Float value) => value.Value;

    public override string    ToDebugString() => $"{GetType().ToShortName()} f32({Value.ToString(CultureInfo.InvariantCulture)})";
    // public override IIterator GetIterator()   => new NumberRangeIterator<float>(this);


    public static Number_Float Make()                            => new();
    public static Number_Float Make(RuntimeTypeInfo_Float value) => new(value);
    public static Number_Float Make(float                 value) => new(value);
    public static Number_Float Make(object                value) => value == null ? Make() : new Number_Float((float) value);
}

[ValueType<Number_Double, RuntimeTypeInfo_Double, double>(RTVT.Double)]
public partial class Number_Double : Number<double>
{
    public override RTVT Type             => RTVT.Double;
    public new RuntimeTypeInfo_Double RuntimeType {
        get => (RuntimeTypeInfo_Double) base.RuntimeType;
        set => base.RuntimeType = value;
    }
    public Number_Double() { }
    public Number_Double(double value) : base(value) { }
    public Number_Double(object value) {
        _value = Convert.ToDouble(value);
    }
    public Number_Double(RuntimeTypeInfo_Double value) : base(value) { }

    public static explicit operator Number_Double(double value) => new Number_Double(value);
    public static explicit operator double(Number_Double value) => value.Value;

    public override string    ToDebugString() => $"{GetType().ToShortName()} f64({Value.ToString(CultureInfo.InvariantCulture)})";
    // public override IIterator GetIterator()   => new NumberRangeIterator<double>(this);


    public static Number_Double Make()                             => new();
    public static Number_Double Make(RuntimeTypeInfo_Double value) => new(value);
    public static Number_Double Make(double                 value) => new(value);
    public static Number_Double Make(object                 value) => value == null ? Make() : new Number_Double((double) value);
}