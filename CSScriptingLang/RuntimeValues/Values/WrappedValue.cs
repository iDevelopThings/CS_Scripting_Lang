using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.RuntimeValues.Types;

namespace CSScriptingLang.RuntimeValues.Values;

public class WrappedValue : Value
{
    protected WrappedValue(ExecContext ctx, Value prototype = null) : base(RTVT.Object, ctx, prototype) { }

    public static WrappedValue From(ExecContext ctx, object value, Value prototype = null) {
        var wrappedType = typeof(WrappedValue<>).MakeGenericType(value.GetType());
        var obj = (WrappedValue)Activator.CreateInstance(wrappedType, ctx, prototype)!;
        obj.DataObject = value;
        return obj;
    }
}

public class WrappedValue<T> : WrappedValue where T : class
{
    protected WrappedValue(ExecContext ctx, Value prototype = null) : base(ctx, prototype) { }

    public T Value {
        get => DataObject as T;
        set => DataObject = value;
    }

    public static WrappedValue<T> From(ExecContext ctx, T value, Value prototype = null) {
        var obj = new WrappedValue<T>(ctx, prototype) {
            DataObject = value,
        };
        return obj;
    }
    
    public static implicit operator T(WrappedValue<T> value) => value.Value;
}