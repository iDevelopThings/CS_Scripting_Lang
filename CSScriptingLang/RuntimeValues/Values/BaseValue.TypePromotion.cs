using System.Runtime.CompilerServices;
using CSScriptingLang.Lexing;
using CSScriptingLang.RuntimeValues.Types;

namespace CSScriptingLang.RuntimeValues.Values;

public static class TypeCoercionRegistry
{
    private static readonly Dictionary<Type, int> TypePrecedence = new() {
        {typeof(Number_Int32), 1},
        {typeof(Number_Int64), 2},
        {typeof(Number_Float), 3},
        {typeof(Number_Double), 4},
        {typeof(ValueBoolean), 5}, // Boolean gets precedence for truth tests
        {typeof(ValueString), 6},
        {typeof(ValueFunction), 7},
        {typeof(ValueNull), 8}
    };

    public static RuntimeType GetHigherPrecedence(RuntimeType type1, RuntimeType type2) {
        return GetHigherPrecedence(type1.RuntimeValueType, type2.RuntimeValueType);
    }
    public static RuntimeType GetHigherPrecedence(Type type1, Type type2) {
        var type = GetHigherPrecedenceType(type1, type2);

        var rtType = TypeTable.GetFromValueType(type);
        if (rtType == null) {
            throw new ArgumentException($"Cannot determine precedence for unknown types(left={type1.Name}, right={type2.Name}).");
        }

        return rtType;
    }

    public static Type GetHigherPrecedenceType(RuntimeType type1, RuntimeType type2) {
        return GetHigherPrecedenceType(type1.RuntimeValueType, type2.RuntimeValueType);
    }

    // Get the type with the highest precedence between two types
    public static Type GetHigherPrecedenceType(Type type1, Type type2) {
        if (TypePrecedence.TryGetValue(type1, out var typePrec1)) {
            if (TypePrecedence.TryGetValue(type2, out var typePrec2)) {
                return typePrec1 > typePrec2 ? type1 : type2;
            }
        }

        return type1;
    }

    // Coerce one value into the target type
    public static BaseValue Coerce(BaseValue value, Type targetType) {
        if (value.GetType() == targetType) {
            return value;
        }

        return targetType.Name switch {
            nameof(Number_Int32)  => new Number_Int32(Convert.ToInt32(value.GetUntypedValue())),
            nameof(Number_Int64)  => new Number_Int64(Convert.ToInt64(value.GetUntypedValue())),
            nameof(Number_Float)  => new Number_Float(Convert.ToSingle(value.GetUntypedValue())),
            nameof(Number_Double) => new Number_Double(Convert.ToDouble(value.GetUntypedValue())),
            nameof(ValueBoolean)  => value.ToBool() ? ValueFactory.Boolean.True() : ValueFactory.Boolean.False(),
            _                     => throw new ArgumentException($"Cannot cast {value.GetType().Name} to {targetType.Name}.")
        };
    }
}

public struct BaseValueTypeIs
{
    public BaseValueTypeIs(RTVT type) {
        Type = type;
    }
    private RTVT Type { get; set; }


    public bool Null => Type == RTVT.Null;
    public bool Unit => Type == RTVT.Unit;

    public bool Int32  => (Type & RTVT.Int32) != 0;
    public bool Int64  => (Type & RTVT.Int64) != 0;
    public bool Float  => (Type & RTVT.Float) != 0;
    public bool Double => (Type & RTVT.Double) != 0;
    public bool Number => (Type & RTVT.Number) != 0;

    public bool String   => Type == RTVT.String;
    public bool Boolean  => Type == RTVT.Boolean;
    public bool Object   => Type == RTVT.Object;
    public bool Function => Type == RTVT.Function;
    public bool Array    => Type == RTVT.Array;
    public bool Signal   => Type == RTVT.Signal;
    
    public bool A(RTVT type) => (Type & type) != 0;
    
    public bool ThrowIfNot(RTVT type, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "") { 
        if ((Type & type) == 0) {
            throw new InterpreterRuntimeException($"Expected type {type} but got {Type}.").WithCaller(file, line, member);
        }

        return true;
    }
}

public abstract partial class BaseValue
{
    public BaseValueTypeIs Is => new(Type);

    public virtual T CastTo<T>() where T : BaseValue {
        return (T) CastTo(typeof(T));
    }

    public virtual BaseValue CastTo(Type t) {
        return null;
    }

    public virtual bool TryCastTo(Type t, out BaseValue result) {
        if (!CanCastTo(t)) {
            result = null;
            return false;
        }

        result = CastTo(t);

        return true;
    }
    public virtual bool TryCastTo<T>(out T result) where T : BaseValue {
        if (!TryCastTo(typeof(T), out var baseResult)) {
            result = null;
            return false;
        }

        result = (T) baseResult;

        return true;
    }


    public virtual bool CanCastTo(Type t) {
        throw new NotImplementedException();
    }
    public virtual bool CanCastTo<T>() where T : BaseValue
        => CanCastTo(typeof(T));


    public BaseValue Operator(OperatorType op, BaseValue other) {
        return op switch {
            OperatorType.Plus      => Operator_Add(other),
            OperatorType.Minus     => Operator_Subtract(other),
            OperatorType.Multiply  => Operator_Multiply(other),
            OperatorType.Divide    => Operator_Divide(other),
            OperatorType.Modulus   => Operator_Modulo(other),
            OperatorType.Equals    => Operator_Equals(other),
            OperatorType.NotEquals => Operator_NotEquals(other),

            OperatorType.LessThan           => Operator_LessThan(other),
            OperatorType.GreaterThan        => Operator_GreaterThan(other),
            OperatorType.LessThanOrEqual    => Operator_LessThanOrEqual(other),
            OperatorType.GreaterThanOrEqual => Operator_GreaterThanOrEqual(other),

            OperatorType.Or => Operator_Or(other),

            OperatorType.PlusEquals => Apply(() => {
                BaseValue opResult = null;
                try {
                    opResult = Operator_PlusEquals(other);
                }
                catch (InvalidOperationException) {
                    opResult = Operator_Add(other);
                    var casted = opResult.CastTo(GetType());
                    SetUntypedValue(casted.GetUntypedValue());
                }

                return this;
            }),
            OperatorType.MinusEquals => Apply(() => {
                BaseValue opResult = null;
                try {
                    opResult = Operator_MinusEquals(other);
                }
                catch (InvalidOperationException) {
                    opResult = Operator_Subtract(other);
                    var casted = opResult.CastTo(GetType());
                    SetUntypedValue(casted.GetUntypedValue());
                }

                return this;
            }),

            OperatorType.Increment => Operator(OperatorType.PlusEquals, other),
            OperatorType.Decrement => Operator(OperatorType.MinusEquals, other),

            _ => throw new InvalidOperationException("Unsupported operator."),
        };
    }

    private BaseValue Apply(Func<BaseValue> action) {
        return action();
    }

    public virtual BaseValue Operator_Equals(BaseValue right) {
        return Type switch {
            RTVT.Int32    => ValueFactory.Boolean.Make(((Number_Int32) this).Value == right.CastTo<Number_Int32>().Value),
            RTVT.Int64    => ValueFactory.Boolean.Make(((Number_Int64) this).Value == right.CastTo<Number_Int64>().Value),
            RTVT.Float    => ValueFactory.Boolean.Make(((Number_Float) this).Value == right.CastTo<Number_Float>().Value),
            RTVT.Double   => ValueFactory.Boolean.Make(((Number_Double) this).Value == right.CastTo<Number_Double>().Value),
            RTVT.Boolean  => ValueFactory.Boolean.Make(((ValueBoolean) this).Value == right.CastTo<ValueBoolean>().Value),
            RTVT.String   => ValueFactory.Boolean.Make(((ValueString) this).Value == right.CastTo<ValueString>().Value),
            RTVT.Function => ValueFactory.Boolean.Make(((ValueFunction) this).Value == right.CastTo<ValueFunction>().Value),
            RTVT.Null     => ValueFactory.Boolean.Make(right.Type == RTVT.Null),

            _ => throw new InvalidOperationException($"Unsupported op(==) for left={Type} and right={right.Type}.")
        };
    }

    public virtual BaseValue Operator_NotEquals(BaseValue right) {
        return Type switch {
            RTVT.Int32   => ValueFactory.Boolean.Make(((Number_Int32) this).Value != ((Number_Int32) right).Value),
            RTVT.Int64   => ValueFactory.Boolean.Make(((Number_Int64) this).Value != ((Number_Int64) right).Value),
            RTVT.Float   => ValueFactory.Boolean.Make(((Number_Float) this).Value != ((Number_Float) right).Value),
            RTVT.Double  => ValueFactory.Boolean.Make(((Number_Double) this).Value != ((Number_Double) right).Value),
            RTVT.Boolean => ValueFactory.Boolean.Make(((ValueBoolean) this).Value != ((ValueBoolean) right).Value),
            RTVT.String  => ValueFactory.Boolean.Make(((ValueString) this).Value != ((ValueString) right).Value),
            RTVT.Null    => ValueFactory.Boolean.Make(right.Type != RTVT.Null),

            _ => throw new InvalidOperationException($"Unsupported op(!=) for left={Type} and right={right.Type}.")
        };
    }

    public virtual BaseValue Operator_LessThan(BaseValue right) {
        return Type switch {
            RTVT.Int32  => ValueFactory.Boolean.Make(((Number_Int32) this).Value < ((Number_Int32) right).Value),
            RTVT.Int64  => ValueFactory.Boolean.Make(((Number_Int64) this).Value < ((Number_Int64) right).Value),
            RTVT.Float  => ValueFactory.Boolean.Make(((Number_Float) this).Value < ((Number_Float) right).Value),
            RTVT.Double => ValueFactory.Boolean.Make(((Number_Double) this).Value < ((Number_Double) right).Value),
            RTVT.String => ValueFactory.Boolean.Make(string.Compare(((ValueString) this).Value, ((ValueString) right).Value, StringComparison.Ordinal) < 0),
            RTVT.Null   => ValueFactory.Boolean.False(),

            _ => throw new InvalidOperationException($"Unsupported op(<) for left={Type} and right={right.Type}.")
        };
    }

    public virtual BaseValue Operator_GreaterThan(BaseValue right) {
        return Type switch {
            RTVT.Int32  => ValueFactory.Boolean.Make(((Number_Int32) this).Value > ((Number_Int32) right).Value),
            RTVT.Int64  => ValueFactory.Boolean.Make(((Number_Int64) this).Value > ((Number_Int64) right).Value),
            RTVT.Float  => ValueFactory.Boolean.Make(((Number_Float) this).Value > ((Number_Float) right).Value),
            RTVT.Double => ValueFactory.Boolean.Make(((Number_Double) this).Value > ((Number_Double) right).Value),
            RTVT.String => ValueFactory.Boolean.Make(string.Compare(((ValueString) this).Value, ((ValueString) right).Value, StringComparison.Ordinal) > 0),
            RTVT.Null   => ValueFactory.Boolean.False(),

            _ => throw new InvalidOperationException($"Unsupported op(>) for left={Type} and right={right.Type}.")
        };
    }

    public virtual BaseValue Operator_LessThanOrEqual(BaseValue right) {
        var lessThan = Operator_LessThan(right);
        var equals   = Operator_Equals(right);

        return lessThan.Operator(OperatorType.Or, equals);
    }

    public virtual BaseValue Operator_GreaterThanOrEqual(BaseValue right) {
        var greaterThan = Operator_GreaterThan(right);
        var equals      = Operator_Equals(right);

        return greaterThan.Operator(OperatorType.Or, equals);
    }

    public virtual BaseValue Operator_Or(BaseValue right) {
        return Type switch {
            RTVT.Boolean => ValueFactory.Boolean.Make(((ValueBoolean) this).Value || ((ValueBoolean) right).Value),
            RTVT.Null    => ValueFactory.Boolean.False(),

            _ => throw new InvalidOperationException($"Unsupported op(||) for left={Type} and right={right.Type}.")
        };
    }

    public virtual BaseValue Operator_Add(BaseValue right) {
        if (right == null)
            throw new ArgumentNullException(nameof(right));

        // if (Fields.TryGetValue("operator_add", out var addFunc) && addFunc is ValueFunction func) {
        //     if (func.TryCall(null, this, out var result, right)) {
        //         return result;
        //     }
        // }

        var resultType = TypeCoercionRegistry.GetHigherPrecedenceType(GetType(), right.GetType());

        // Coerce both values to the highest precedence type
        var leftCoerced  = CastTo(resultType);
        var rightCoerced = right.CastTo(resultType);

        // Perform addition based on the resulting type
        return resultType.Name switch {
            nameof(Number_Int32)  => new Number_Int32(leftCoerced.Value<int>() + rightCoerced.Value<int>()),
            nameof(Number_Int64)  => new Number_Int64(leftCoerced.Value<long>() + rightCoerced.Value<long>()),
            nameof(Number_Float)  => new Number_Float(leftCoerced.Value<float>() + rightCoerced.Value<float>()),
            nameof(Number_Double) => new Number_Double(leftCoerced.Value<double>() + rightCoerced.Value<double>()),
            nameof(ValueString)   => new ValueString(leftCoerced.Value<string>() + rightCoerced.Value<string>()),
            _                     => throw new InvalidOperationException($"Unsupported operation lhs={leftCoerced?.Type} op=+ rhs={rightCoerced}")
        };
    }

    public virtual BaseValue Operator_Subtract(BaseValue right) {
        if (right == null)
            throw new ArgumentNullException(nameof(right));

        var resultType = TypeCoercionRegistry.GetHigherPrecedenceType(GetType(), right.GetType());

        // Coerce both values to the highest precedence type
        var leftCoerced  = CastTo(resultType);
        var rightCoerced = right.CastTo(resultType);

        // Perform subtraction based on the resulting type
        return resultType.Name switch {
            nameof(Number_Int32)  => new Number_Int32(leftCoerced.Value<int>() - rightCoerced.Value<int>()),
            nameof(Number_Int64)  => new Number_Int64(leftCoerced.Value<long>() - rightCoerced.Value<long>()),
            nameof(Number_Float)  => new Number_Float(leftCoerced.Value<float>() - rightCoerced.Value<float>()),
            nameof(Number_Double) => new Number_Double(leftCoerced.Value<double>() - rightCoerced.Value<double>()),
            _                     => throw new InvalidOperationException($"Unsupported operation lhs={leftCoerced?.Type} op=- rhs={rightCoerced}")
        };
    }

    public virtual BaseValue Operator_Multiply(BaseValue right) {
        if (right == null)
            throw new ArgumentNullException(nameof(right));

        var resultType = TypeCoercionRegistry.GetHigherPrecedenceType(GetType(), right.GetType());

        // Coerce both values to the highest precedence type
        var leftCoerced  = CastTo(resultType);
        var rightCoerced = right.CastTo(resultType);

        // Perform multiplication based on the resulting type
        return resultType.Name switch {
            nameof(Number_Int32)  => new Number_Int32(leftCoerced.Value<int>() * rightCoerced.Value<int>()),
            nameof(Number_Int64)  => new Number_Int64(leftCoerced.Value<long>() * rightCoerced.Value<long>()),
            nameof(Number_Float)  => new Number_Float(leftCoerced.Value<float>() * rightCoerced.Value<float>()),
            nameof(Number_Double) => new Number_Double(leftCoerced.Value<double>() * rightCoerced.Value<double>()),
            _                     => throw new InvalidOperationException($"Unsupported operation lhs={leftCoerced?.Type} op=* rhs={rightCoerced}")
        };
    }

    public virtual BaseValue Operator_Divide(BaseValue right) {
        if (right == null)
            throw new ArgumentNullException(nameof(right));

        var resultType = TypeCoercionRegistry.GetHigherPrecedenceType(GetType(), right.GetType());

        // Coerce both values to the highest precedence type
        var leftCoerced  = CastTo(resultType);
        var rightCoerced = right.CastTo(resultType);

        if (rightCoerced.IsZeroValue()) {
            Logger.Warning($"Division by zero -> ({leftCoerced.GetUntypedValue()} / {rightCoerced.GetUntypedValue()}). Returning zero value.");
            return ValueFactory.Make(resultType);
        }

        // Perform division based on the resulting type
        return resultType.Name switch {
            nameof(Number_Int32)  => new Number_Int32(leftCoerced.Value<int>() / rightCoerced.Value<int>()),
            nameof(Number_Int64)  => new Number_Int64(leftCoerced.Value<long>() / rightCoerced.Value<long>()),
            nameof(Number_Float)  => new Number_Float(leftCoerced.Value<float>() / rightCoerced.Value<float>()),
            nameof(Number_Double) => new Number_Double(leftCoerced.Value<double>() / rightCoerced.Value<double>()),
            _                     => throw new InvalidOperationException($"Unsupported operation lhs={leftCoerced?.Type} op=/ rhs={rightCoerced}")
        };
    }

    public virtual BaseValue Operator_Modulo(BaseValue right) {
        if (right == null)
            throw new ArgumentNullException(nameof(right));

        var resultType = TypeCoercionRegistry.GetHigherPrecedenceType(GetType(), right.GetType());

        // Coerce both values to the highest precedence type
        var leftCoerced  = CastTo(resultType);
        var rightCoerced = right.CastTo(resultType);

        // Perform modulo based on the resulting type
        return resultType.Name switch {
            nameof(Number_Int32)  => new Number_Int32(leftCoerced.Value<int>() % rightCoerced.Value<int>()),
            nameof(Number_Int64)  => new Number_Int64(leftCoerced.Value<long>() % rightCoerced.Value<long>()),
            nameof(Number_Float)  => new Number_Float(leftCoerced.Value<float>() % rightCoerced.Value<float>()),
            nameof(Number_Double) => new Number_Double(leftCoerced.Value<double>() % rightCoerced.Value<double>()),
            _                     => throw new InvalidOperationException($"Unsupported operation lhs={leftCoerced?.Type} op=% rhs={rightCoerced}")
        };
    }

    public virtual BaseValue Operator_PlusEquals(BaseValue right) {
        throw new InvalidOperationException($"Unsupported operation lhs={Type} op=+= rhs={right.Type}");
    }
    public virtual BaseValue Operator_MinusEquals(BaseValue right) {
        throw new InvalidOperationException($"Unsupported operation lhs={Type} op=-= rhs={right.Type}");
    }
}