using CSScriptingLang.Interpreter.Bindings;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Lexing;
using CSScriptingLang.RuntimeValues.Types;

namespace CSScriptingLang.RuntimeValues.Values;

// [LanguageClassBind(Name = "array")]
public partial class ValueArray : BaseValue<ValueArray, List<BaseValue>>
{
    public override RTVT Type             => RTVT.Array;
    public new RuntimeTypeInfo_Array RuntimeType {
        get => (RuntimeTypeInfo_Array) base.RuntimeType;
        set => base.RuntimeType = value;
    }

    public override bool ToBool() => Value.Count > 0;

    public bool CanAutoExpand { get; set; } = true;

    public Action<BaseValue> OnElementAdded   { get; set; }
    public Action<BaseValue> OnElementRemoved { get; set; }

    [NativeFieldBind]
    [LanguageFunction]
    public int Length => Value?.Count ?? 0;

    [LanguageValueConstructor]
    public ValueArray() {
        Value = [];
    }
    [LanguageValueConstructor]
    public ValueArray(List<BaseValue> value) {
        Value = value ?? [];
    }
    public ValueArray(RuntimeTypeInfo_Array value) : base(value) {
        Value = [];
    }

    public static explicit operator ValueArray(List<BaseValue> value) => new ValueArray(value);
    public static explicit operator List<BaseValue>(ValueArray value) => value.Value;

    [LanguageFunction]
    public string TestMethod(bool value) {
        return "..";
    }

    public static   object GetNativeZero() => new List<BaseValue>();
    public override bool   IsZeroValue()   => Value.Count == 0;

    public override void SetValueAtIndex(int index, BaseValue value)
        => Set(index, value);
    public override BaseValue GetValueAtIndex(int index)
        => Get(index);


    [NativeFunctionBind]
    public BaseValue Push(ref NativeFunctionExecutionContext ctx, ValueArray @this, params BaseValue[] args) {
        foreach (var arg in args) {
            Add(arg);
        }

        return ValueFactory.Make(Length);
    }
    [NativeFunctionBind]
    public BaseValue Append(ref NativeFunctionExecutionContext ctx, ValueArray @this, params BaseValue[] args) {
        return Push(ref ctx, @this, args);
    }
    [NativeFunctionBind]
    public BaseValue RemoveAt(ref NativeFunctionExecutionContext ctx, ValueArray @this, BaseValue index) {
        if (!index.Is.Number) {
            Logger.Warning($"Index must be a number, got {index.Type}");
            return null;
        }

        Remove(index.Value<int>());

        return ValueFactory.Make(Length);
    }

    [NativeFunctionBind]
    public BaseValue RemoveRange(ref NativeFunctionExecutionContext ctx, ValueArray @this, BaseValue start, BaseValue end) {
        if (!start.Is.Number || !end.Is.Number) {
            Logger.Warning($"Start and end must be numbers, got {start.Type} and {end.Type}");
            return null;
        }

        var startIdx = start.Value<int>();
        var endIdx   = end.Value<int>();

        Remove(startIdx, endIdx);

        return ValueFactory.Make(Length);
    }

    public BaseValue this[int index] {
        get => Get(index);
        set => Set(index, value);
    }

    public BaseValue Get(int index) {
        if (index < 0 || index >= Value.Count) {
            Logger.Warning($"Index out of range: {index}");
            return null;
        }

        return Value[index];
    }

    public void Remove(int index) {
        if (index < 0 || index >= Value.Count) {
            Logger.Warning($"Index out of range: {index}");
            return;
        }

        var value = Value[index];
        value.Outer = null;

        Value.RemoveAt(index);

        OnElementRemoved?.Invoke(value);
    }
    public void Remove(int start, int end) {
        if (start < 0 || start >= Value.Count) {
            Logger.Warning($"Start index out of range: {start}");
            return;
        }

        if (end < 0 || end >= Value.Count) {
            Logger.Warning($"End index out of range: {end}");
            return;
        }

        var elementsRemoved = Value.GetRange(start, end - start).ToList();

        Value.RemoveRange(start, end - start);

        foreach (var element in elementsRemoved) {
            element.Outer = null;

            OnElementRemoved?.Invoke(element);
        }
    }

    public int Add(BaseValue value) {
        Value.Add(value);

        if (value.Outer != null && value.Outer != this) {
            Logger.Warning($"Value is already owned by another object: {value.Outer}");
        }

        value.Outer = this;

        OnElementAdded?.Invoke(value);
        return Length;
    }

    public void Set(int index, BaseValue value) {
        if (index < 0)
            throw new IndexOutOfRangeException("Index cannot be negative");

        if (index >= Value.Count) {
            if (!CanAutoExpand) {
                Logger.Warning($"Index out of range: {index}");
                return;
            }

            Value.AddRange(Enumerable.Repeat<BaseValue>(null, index - Value.Count + 1));
        }

        if (value.Outer != null && value.Outer != this) {
            Logger.Warning($"Value is already owned by another object: {value.Outer}");
        }

        value.Outer = this;

        Value[index] = value;

        OnElementAdded?.Invoke(value);
    }

    // public IIterator GetIterator() => new ArrayIterator(this);
    
    public static ValueArray Make()                            => new();
    public static ValueArray Make(RuntimeTypeInfo_Array value) => new(value);
    public static ValueArray Make(List<BaseValue>       value) => new(value);
    public static ValueArray Make(object                value) => value == null ? Make() : new ValueArray((List<BaseValue>) value);
}
