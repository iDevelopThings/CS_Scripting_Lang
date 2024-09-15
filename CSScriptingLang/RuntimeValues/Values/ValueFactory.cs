using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.RuntimeValues.Types;
using Force.DeepCloner;

// ReSharper disable MemberHidesStaticFromOuterClass

namespace CSScriptingLang.RuntimeValues.Values;

public static class ValueFactory
{
    public static T Make<T>(object value = null) where T : BaseValue {
        return (T) Make(typeof(T), value);
    }

    public static BaseValue Make(Type type, object value = null) {
        if (type == typeof(RTVT)) return Make(value as RTVT? ?? RTVT.Null);

        if (type == typeof(List<BaseValue>)) return ValueArray.Make(value);
        if (type == typeof(bool)) return Boolean.Make(value);
        if (type == typeof(InlineFunctionDeclaration)) return ValueFunction.Make(value);
        if (type == typeof(object)) return Null.Make();
        if (type == typeof(int)) return Number_Int32.Make(value);
        if (type == typeof(long)) return Number_Int64.Make(value);
        if (type == typeof(float)) return Number_Float.Make(value);
        if (type == typeof(double)) return Number_Double.Make(value);
        if (type == typeof(ObjectDictionary)) return ValueObject.Make(value);
        if (type == typeof(SignalDeclarationNode)) return Signal.Make(value);
        if (type == typeof(string)) return ValueString.Make(value);
        if (type == typeof(StructDictionary)) return Struct.Make(value);

        if (type != null && type.IsAssignableTo(typeof(BaseValue))) {
            if (type == typeof(ValueArray)) return ValueArray.Make(value);
            if (type == typeof(ValueBoolean)) return Boolean.Make(value);
            if (type == typeof(ValueFunction)) return ValueFunction.Make(value);
            if (type == typeof(ValueNull)) return Null.Make();
            if (type == typeof(ValueUnit)) return Unit.Make();
            if (type == typeof(Number_Int32)) return Number_Int32.Make(value);
            if (type == typeof(Number_Int64)) return Number_Int64.Make(value);
            if (type == typeof(Number_Float)) return Number_Float.Make(value);
            if (type == typeof(Number_Double)) return Number_Double.Make(value);
            if (type == typeof(ValueObject)) return ValueObject.Make(value);
            if (type == typeof(ValueSignal)) return Signal.Make(value);
            if (type == typeof(ValueString)) return ValueString.Make(value);
            if (type == typeof(ValueStruct)) return Struct.Make(value);
        }

        if (type != null && type.IsAssignableTo(typeof(RuntimeType))) {
            if (type == typeof(RuntimeTypeInfo_Array)) return ValueArray.Make(value);
            if (type == typeof(RuntimeTypeInfo_Boolean)) return Boolean.Make(value);
            if (type == typeof(RuntimeTypeInfo_Function)) return ValueFunction.Make(value);
            if (type == typeof(RuntimeTypeInfo_Int32)) return Number_Int32.Make(value);
            if (type == typeof(RuntimeTypeInfo_Int64)) return Number_Int64.Make(value);
            if (type == typeof(RuntimeTypeInfo_Float)) return Number_Float.Make(value);
            if (type == typeof(RuntimeTypeInfo_Double)) return Number_Double.Make(value);
            if (type == typeof(RuntimeTypeInfo_Object)) return ValueObject.Make(value);
            if (type == typeof(RuntimeTypeInfo_Signal)) return Signal.Make(value);
            if (type == typeof(RuntimeTypeInfo_String)) return ValueString.Make(value);
            if (type == typeof(RuntimeTypeInfo_Struct)) return Struct.Make(value);
            if (type == typeof(RuntimeTypeInfo_Unit)) return Unit.Make();
            if (type == typeof(RuntimeTypeInfo_Null)) return Null.Make();
        }

        throw new ArgumentException($"Unknown RTVT: {type}", nameof(type));
    }

    public static BaseValue Make(object value) {
        if (value is null)
            return Null.Zero();

        switch (value) {
            case List<BaseValue> vValueArray:             return ValueArray.Make(vValueArray);
            case bool vBoolean:                           return Boolean.Make(vBoolean);
            case InlineFunctionDeclaration vFunction: return ValueFunction.Make(vFunction);
            case int vNumber_Int32:                       return Number_Int32.Make(vNumber_Int32);
            case long vNumber_Int64:                      return Number_Int64.Make(vNumber_Int64);
            case float vNumber_Float:                     return Number_Float.Make(vNumber_Float);
            case double vNumber_Double:                   return Number_Double.Make(vNumber_Double);
            case ObjectDictionary vObject:                return ValueObject.Make(vObject);
            case SignalDeclarationNode vSignal:           return Signal.Make(vSignal);
            case string vString:                          return ValueString.Make(vString);
            case StructDictionary vStruct:                return Struct.Make(vStruct);
        }

        throw new ArgumentException($"Unknown value type: {value.GetType()}", nameof(value));
    }

    public static BaseValue Make(RTVT type, object value = null) {
        switch (type) {
            case RTVT.Array:    return ValueArray.Make(value);
            case RTVT.Boolean:  return Boolean.Make(value);
            case RTVT.Function: return ValueFunction.Make(value);
            case RTVT.Null:     return Null.Make();
            case RTVT.Unit:     return Unit.Make();
            case RTVT.Int32:    return Number_Int32.Make(value);
            case RTVT.Int64:    return Number_Int64.Make(value);
            case RTVT.Float:    return Number_Float.Make(value);
            case RTVT.Double:   return Number_Double.Make(value);
            case RTVT.Object:   return ValueObject.Make(value);
            case RTVT.Signal:   return Signal.Make(value);
            case RTVT.String:   return ValueString.Make(value);
            case RTVT.Struct:   return Struct.Make(value);
        }

        throw new ArgumentException($"Unknown RTVT: {type}", nameof(type));
    }

    public static BaseValue Clone(BaseValue value) {
        var newValue = value.DeepClone();

        return newValue;

        /*return value switch {
            Number_Int32 i32  => i32.GetClone(),
            Number_Int64 i64  => i64.GetClone(),
            Number_Float f32  => f32.GetClone(),
            Number_Double f64 => f64.GetClone(),
            ValueString str   => str.GetClone(),
            Boolean b         => b.GetClone(),
            ValueArray arr    => arr.GetClone(),
            ValueFunction fn  => fn.GetClone(),
            Struct s          => s.GetClone(),
            Signal sig        => sig.GetClone(),
            ValueObject obj   => obj.GetClone(),
            _                 => Make(value.Type, value.GetUntypedValue())
        };*/
    }

    public static T Clone<T>(T value) where T : BaseValue {
        return (T) Clone((BaseValue) value);
    }

    public static object NativeZeroValue(RTVT type) {
        switch (type) {
            case RTVT.Array:    return ValueArray.GetNativeZero();
            case RTVT.Boolean:  return ValueBoolean.GetNativeZero();
            case RTVT.Function: return ValueFunction.GetNativeZero();
            case RTVT.Null:     return null;
            case RTVT.Unit:     return ValueUnit.GetNativeZero();
            case RTVT.Int32:    return Number_Int32.GetNativeZero();
            case RTVT.Int64:    return Number_Int64.GetNativeZero();
            case RTVT.Float:    return Number_Float.GetNativeZero();
            case RTVT.Double:   return Number_Double.GetNativeZero();
            case RTVT.Object:   return ValueObject.GetNativeZero();
            case RTVT.Signal:   return ValueSignal.GetNativeZero();
            case RTVT.String:   return ValueString.GetNativeZero();
            case RTVT.Struct:   return ValueStruct.GetNativeZero();
        }

        throw new ArgumentException($"Unknown RTVT: {type}", nameof(type));
    }

    public class BaseValueFactory<T> where T : BaseValue
    {
        public T Zero() => ValueFactory.Make<T>();

        public object NativeZero() {
            return typeof(T) switch {
                _ when typeof(T) == typeof(ValueArray)    => ValueArray.GetNativeZero(),
                _ when typeof(T) == typeof(ValueBoolean)  => ValueBoolean.GetNativeZero(),
                _ when typeof(T) == typeof(ValueFunction) => ValueFunction.GetNativeZero(),
                _ when typeof(T) == typeof(ValueNull)     => ValueNull.GetNativeZero(),
                _ when typeof(T) == typeof(ValueUnit)     => ValueUnit.GetNativeZero(),
                _ when typeof(T) == typeof(Number_Int32)  => Number_Int32.GetNativeZero(),
                _ when typeof(T) == typeof(Number_Int64)  => Number_Int64.GetNativeZero(),
                _ when typeof(T) == typeof(Number_Float)  => Number_Float.GetNativeZero(),
                _ when typeof(T) == typeof(Number_Double) => Number_Double.GetNativeZero(),
                _ when typeof(T) == typeof(ValueObject)   => ValueObject.GetNativeZero(),
                _ when typeof(T) == typeof(ValueSignal)   => ValueSignal.GetNativeZero(),
                _ when typeof(T) == typeof(ValueString)   => ValueString.GetNativeZero(),
                _ when typeof(T) == typeof(ValueStruct)   => ValueStruct.GetNativeZero(),
                _                                         => throw new ArgumentException($"Unknown RTVT: {typeof(T)}", nameof(T))
            };
        }
    }

    public class MakeObjectFactory : BaseValueFactory<ValueObject>
    {
        public ValueObject Make()                             => ValueObject.Make();
        public ValueObject Make(RuntimeTypeInfo_Object value) => ValueObject.Make(value);
        public ValueObject Make(ObjectDictionary       value) => ValueObject.Make(value);
        public ValueObject Make(object                 value) => ValueObject.Make(value);
    }

    public static MakeObjectFactory Object => new();

    public class MakeStringFactory : BaseValueFactory<ValueString>
    {
        public ValueString Make()                             => new();
        public ValueString Make(RuntimeTypeInfo_String value) => new(value);
        public ValueString Make(string                 value) => new(value);
        public ValueString Make(object                 value) => value == null ? Make() : new ValueString((string) value);
    }

    public static MakeStringFactory String => new();

    public class MakeStructFactory : BaseValueFactory<ValueStruct>
    {
        public ValueStruct Make()                             => new();
        public ValueStruct Make(RuntimeTypeInfo_Struct value) => new(value);
        public ValueStruct Make(StructDictionary       value) => new(value);
        public ValueStruct Make(object                 value) => value == null ? Make() : new ValueStruct((StructDictionary) value);
    }

    public static MakeStructFactory Struct => new();

    public class MakeSignalFactory : BaseValueFactory<ValueSignal>
    {
        public ValueSignal Make()                             => new();
        public ValueSignal Make(RuntimeTypeInfo_Signal value) => new(value);
        public ValueSignal Make(SignalDeclarationNode  value) => new(value);
        public ValueSignal Make(object                 value) => value == null ? Make() : new ValueSignal((SignalDeclarationNode) value);
    }

    public static MakeSignalFactory Signal => new();

    public class MakeNumber_Int32Factory : BaseValueFactory<Number_Int32>
    {
        public Number_Int32 Make()                            => new();
        public Number_Int32 Make(RuntimeTypeInfo_Int32 value) => new(value);
        public Number_Int32 Make(int                   value) => new(value);
        public Number_Int32 Make(object                value) => value == null ? Make() : new Number_Int32((int) value);
    }

    public static MakeNumber_Int32Factory Int32 => new();

    public class MakeNumber_Int64Factory : BaseValueFactory<Number_Int64>
    {
        public Number_Int64 Make()                            => new();
        public Number_Int64 Make(RuntimeTypeInfo_Int64 value) => new(value);
        public Number_Int64 Make(long                  value) => new(value);
        public Number_Int64 Make(object                value) => value == null ? Make() : new Number_Int64((long) value);
    }

    public static MakeNumber_Int64Factory Int64 => new();

    public class MakeNumber_FloatFactory : BaseValueFactory<Number_Float>
    {
        public Number_Float Make()                            => new();
        public Number_Float Make(RuntimeTypeInfo_Float value) => new(value);
        public Number_Float Make(float                 value) => new(value);
        public Number_Float Make(object                value) => value == null ? Make() : new Number_Float((float) value);
    }

    public static MakeNumber_FloatFactory Float => new();

    public class MakeNumber_DoubleFactory : BaseValueFactory<Number_Double>
    {
        public Number_Double Make()                             => new();
        public Number_Double Make(RuntimeTypeInfo_Double value) => new(value);
        public Number_Double Make(double                 value) => new(value);
        public Number_Double Make(object                 value) => value == null ? Make() : new Number_Double((double) value);
    }

    public static MakeNumber_DoubleFactory Double => new();

    public class MakeUnitFactory : BaseValueFactory<ValueUnit>
    {
        public ValueUnit Make()                           => new();
        public ValueUnit Make(RuntimeTypeInfo_Unit value) => new(value);
        public ValueUnit Make(SharpX.Unit          value) => new(value);
    }

    public static MakeUnitFactory Unit => new();

    public class MakeNullFactory : BaseValueFactory<ValueNull>
    {
        public ValueNull Make()                           => new();
        public ValueNull Make(SharpX.Unit          value) => new(value);
        public ValueNull Make(RuntimeTypeInfo_Null value) => new(value);
    }

    public static MakeNullFactory Null => new();

    public class MakeBooleanFactory : BaseValueFactory<ValueBoolean>
    {
        public ValueBoolean True()  => new(true);
        public ValueBoolean False() => new(false);

        public ValueBoolean Make()                              => new();
        public ValueBoolean Make(RuntimeTypeInfo_Boolean value) => new(value);
        public ValueBoolean Make(bool                    value) => new(value);
        public ValueBoolean Make(object                  value) => value == null ? Make() : new ValueBoolean((bool) value);
    }

    public static MakeBooleanFactory Boolean => new();

    public class MakeArrayFactory : BaseValueFactory<ValueArray>
    {
        public ValueArray Make()                            => new();
        public ValueArray Make(RuntimeTypeInfo_Array value) => new(value);
        public ValueArray Make(List<BaseValue>       value) => new(value);
    }

    public static MakeArrayFactory Array => new();

    public class MakeFunctionFactory : BaseValueFactory<ValueFunction>
    {
        public ValueFunction Make()                                               => new();
        public ValueFunction Make(RuntimeTypeInfo_Function                 value) => new(value);
        public ValueFunction Make(InlineFunctionDeclaration            value) => new(value);
        public ValueFunction Make(ValueFunction.ExecutableFunction         value) => new() {Executable         = value};
        public ValueFunction Make(ValueFunction.ExecutableInstanceFunction value) => new() {InstanceExecutable = value};
    }

    public static MakeFunctionFactory Function => new();
}