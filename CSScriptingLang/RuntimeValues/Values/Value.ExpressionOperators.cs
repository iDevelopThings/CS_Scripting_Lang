using System.Runtime.CompilerServices;
using CSScriptingLang.Lexing;
using CSScriptingLang.RuntimeValues.Types;

namespace CSScriptingLang.RuntimeValues.Values;

public partial class Value
{

    public Value Operator(OperatorType op, Value other) {
        return op switch {
            OperatorType.Plus               => Operator_Add(other),
            OperatorType.Minus              => Operator_Sub(other),
            OperatorType.Multiply           => Operator_Mul(other),
            OperatorType.Divide             => Operator_Div(other),
            OperatorType.Modulus            => Operator_Mod(other),
            OperatorType.Pow                => Operator_Pow(other),
            OperatorType.LessThan           => Operator_LessThan(other),
            OperatorType.LessThanOrEqual    => Operator_LessThanEqual(other),
            OperatorType.GreaterThan        => Operator_GreaterThan(other),
            OperatorType.GreaterThanOrEqual => Operator_GreaterThanEqual(other),

            OperatorType.Equals    => Operator_Equal(other),
            OperatorType.NotEquals => Operator_NotEqual(other),

            OperatorType.And => Operator_ConditionalAnd(other),
            OperatorType.Or  => Operator_ConditionalOr(other),
            OperatorType.Not => Operator_ConditionalNot(),

            OperatorType.Increment => Operator_Increment(),
            OperatorType.Decrement => Operator_Decrement(),

            _ => throw new InterpreterRuntimeException($"Invalid/Unhandled operator lhs={Type} op={op} rhs={other?.Type}"),
        };
    }

    #region Operator Equals

    /*[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Value a, Value b) {
        if (b == null) {
            return a == null;
        }

        return a?.Operator_Equal(b) ?? b?.Is.Null == true;
    }*/

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Operator_Equal(Value other) {
        if (other == null) {
            return false;
        }

        switch (Type) {
            case RTVT.Int32:
                return As.Int32() == other.As.Int32();
            case RTVT.Int64:
                return As.Int64() == other.As.Int64();
            case RTVT.Float:
                return As.Float() == other.As.Float();
            case RTVT.Double:
                return As.Double() == other.As.Double();
            case RTVT.String:
                return other?.Is.String == true && As.String() == other.As.String();
            case RTVT.Boolean:
                return other?.Is.Boolean == true && As.Bool() == other.As.Bool();
            case RTVT.Function:
                return other?.Is.Function == true && ReferenceEquals(As.Fn(), other.As.Fn());
            case RTVT.Array:
                return other?.Is.Array == true && ReferenceEquals(As.List(), other.As.List());
            case RTVT.Object: {
                if (DispatchCall("__eq", [other], out var objResult)) {
                    return objResult.As.Bool();
                }

                return other?.Is.Object == true && ReferenceEquals(Members, other.Members);
            }

            default: {
                if (DispatchCall("__eq", [other], out var defaultResult)) {
                    return defaultResult.IsTruthy();
                }

                return Type == other.Type;
            }

        }

    }

    #endregion

    #region Operator NotEqual

    /*[MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Value a, Value b) {
        if (b == null) {
            return a != null;
        }

        return a?.Operator_NotEqual(b) ?? b?.Is.Null == false;
    }*/

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Operator_NotEqual(Value other) {
        if (other == null) {
            return true;
        }

        if (Is.Object) {
            if (DispatchCall("__neq", [other], out var objResult)) {
                return objResult.As.Bool();
            }
        }

        return !Operator_Equal(other);
    }

    #endregion

    #region Operator Assign

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value Operator_Assign(Value other) {
        if (SetValue(other, false)) {
            return this;
        }

        return Dispatch_Operator_Assign(other);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Value Dispatch_Operator_Assign(Value other) {
        if (DispatchCall("__assign", [other], out var result)) {
            return result;
        }

        throw new InvalidCastException($"Cannot Assign {Type} and {other.Type}");
    }

    #endregion

    #region Operator Increment

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Value operator ++(Value a) => a.Operator_Increment();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value Operator_Increment() {
        if (Is.Int32)
            return Operator_Assign(As.Int() + 1);
        if (Is.Int64)
            return Operator_Assign(As.Long() + 1);
        if (Is.Float)
            return Operator_Assign(As.Float() + 1);
        if (Is.Double)
            return Operator_Assign(As.Double() + 1);

        return Dispatch_Operator_Increment();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Value Dispatch_Operator_Increment() {
        if (DispatchCall("__increment", [], out var result)) {
            return result;
        }

        throw new InvalidCastException($"Cannot Increment {Type}");
    }

    #endregion

    #region Operator Decrement

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Value operator --(Value a) => a.Operator_Decrement();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value Operator_Decrement() {
        if (Is.Int32)
            return Operator_Assign(As.Int() - 1);
        if (Is.Int64)
            return Operator_Assign(As.Long() - 1);
        if (Is.Float)
            return Operator_Assign(As.Float() - 1);
        if (Is.Double)
            return Operator_Assign(As.Double() - 1);


        return Dispatch_Operator_Decrement();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Value Dispatch_Operator_Decrement() {
        if (DispatchCall("__decrement", [], out var result)) {
            return result;
        }

        throw new InvalidCastException($"Cannot Decrement {Type}");
    }

    #endregion

    #region Operator Add

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Value operator +(Value a, Value b) => a.Operator_Add(b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value Operator_Add(Value other) {
        if (Is.String || other.Is.String) {
            return As.String() + other.As.String();
        }

        if (Is.Int32) return As.Int32() + other.As.Int32();
        if (Is.Int64) return As.Int64() + other.As.Int64();
        if (Is.Float) return As.Float() + other.As.Float();
        if (Is.Double) return As.Double() + other.As.Double();


        return Dispatch_Operator_Add(other);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Value Dispatch_Operator_Add(Value other) {
        if (DispatchCall("__add", [other], out var result)) {
            return result;
        }

        if (other.Is.Int32) return As.Int32() + other.As.Int32();
        if (other.Is.Int64) return As.Int64() + other.As.Int64();
        if (other.Is.Float) return As.Float() + other.As.Float();
        if (other.Is.Double) return As.Double() + other.As.Double();

        throw new InvalidCastException($"Cannot Add {Type} and {other.Type}");
    }

    #endregion

    #region Operator Sub

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Value operator -(Value a, Value b) => a.Operator_Sub(b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value Operator_Sub(Value other) {
        if (Is.Int32) return As.Int() - other.As.Int();
        if (Is.Int64) return As.Long() - other.As.Long();
        if (Is.Float) return As.Float() - other.As.Float();
        if (Is.Double) return As.Double() - other.As.Double();

        return Dispatch_Operator_Sub(other);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Value Dispatch_Operator_Sub(Value other) {
        if (DispatchCall("__sub", [other], out var result)) {
            return result;
        }

        if (other.Is.Int32) return As.Int32() - other.As.Int32();
        if (other.Is.Int64) return As.Int64() - other.As.Int64();
        if (other.Is.Float) return As.Float() - other.As.Float();
        if (other.Is.Double) return As.Double() - other.As.Double();

        throw new InvalidCastException($"Cannot Sub {Type} and {other.Type}");
    }

    #endregion

    #region Operator Mul

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Value operator *(Value a, Value b) => a.Operator_Mul(b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value Operator_Mul(Value other) {
        if (Is.Int32) return As.Int() * other.As.Int();
        if (Is.Int64) return As.Long() * other.As.Long();
        if (Is.Float) return As.Float() * other.As.Float();
        if (Is.Double) return As.Double() * other.As.Double();

        return Dispatch_Operator_Mul(other);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Value Dispatch_Operator_Mul(Value other) {
        if (DispatchCall("__mul", [other], out var result)) {
            return result;
        }

        if (other.Is.Int32) return As.Int32() * other.As.Int32();
        if (other.Is.Int64) return As.Long() * other.As.Long();
        if (other.Is.Float) return As.Float() * other.As.Float();
        if (other.Is.Double) return As.Double() * other.As.Double();

        throw new InvalidCastException($"Cannot Mul {Type} and {other.Type}");
    }

    #endregion

    #region Operator Div

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Value operator /(Value a, Value b) => a.Operator_Div(b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value Operator_Div(Value other) {
        if (Is.Number) {
            if (other.Is.ZeroValue()) {
                Logger.Error("Division by zero");
                return 0;
            }

            if (Is.Int32) return As.Int() / other.As.Int();
            if (Is.Int64) return As.Long() / other.As.Long();
            if (Is.Float) return As.Float() / other.As.Float();
            if (Is.Double) return As.Double() / other.As.Double();
        }

        return Dispatch_Operator_Div(other);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Value Dispatch_Operator_Div(Value other) {
        if (DispatchCall("__div", [other], out var result)) {
            return result;
        }

        if (other.Is.Number) {
            if (other.Is.ZeroValue()) {
                Logger.Error("Division by zero");
                return 0;
            }

            if (Is.Int32) return As.Int32() / other.As.Int32();
            if (Is.Int64) return As.Long() / other.As.Long();
            if (Is.Float) return As.Float() / other.As.Float();
            if (Is.Double) return As.Double() / other.As.Double();
        }

        throw new InvalidCastException($"Cannot Div {Type} and {other.Type}");
    }

    #endregion

    #region Operator Mod

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Value operator %(Value a, Value b) => a.Operator_Mod(b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value Operator_Mod(Value other) {
        if (Is.Int32) return As.Int() % other.As.Int();
        if (Is.Int64) return As.Long() % other.As.Long();
        if (Is.Float) return As.Float() % other.As.Float();
        if (Is.Double) return As.Double() % other.As.Double();

        return Dispatch_Operator_Mod(other);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Value Dispatch_Operator_Mod(Value other) {
        if (DispatchCall("__mod", [other], out var result)) {
            return result;
        }

        if (other.Is.Int32) return As.Int() % other.As.Int();
        if (other.Is.Int64) return As.Long() % other.As.Long();
        if (other.Is.Float) return As.Float() % other.As.Float();
        if (other.Is.Double) return As.Double() % other.As.Double();

        throw new InvalidCastException($"Cannot Mod {Type} and {other.Type}");
    }

    #endregion

    #region Operator Pow

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value Operator_Pow(Value other) {
        if (Is.Int32) return (int) Math.Pow(As.Int(), other.As.Int());
        if (Is.Int64) return (long) Math.Pow(As.Long(), other.As.Long());
        if (Is.Float) return (float) Math.Pow(As.Float(), other.As.Float());
        if (Is.Double) return Math.Pow(As.Double(), other.As.Double());

        return Dispatch_Operator_Pow(other);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Value Dispatch_Operator_Pow(Value other) {
        if (DispatchCall("__pow", [other], out var result)) {
            return result;
        }

        throw new InvalidCastException($"Cannot Pow {Type} and {other.Type}");
    }

    #endregion

    #region Operator LessThan

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Value operator <(Value a, Value b) => a.Operator_LessThan(b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value Operator_LessThan(Value other) {
        if (Is.Int32 && other.Is.Int32) return As.Int32() < other.As.Int32();
        if (Is.Int64 && other.Is.Int64) return As.Long() < other.As.Long();
        if (Is.Float && other.Is.Float) return As.Float() < other.As.Float();
        if (Is.Double && other.Is.Double) return As.Double() < other.As.Double();

        return Dispatch_Operator_LessThan(other);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Value Dispatch_Operator_LessThan(Value other) {
        if (Is.String && other.Is.String) {
            return string.Compare(As.String(), other.As.String(), StringComparison.Ordinal) < 0;
        }
        if (DispatchCall("__lt", [other], out var result)) {
            return result;
        }
        throw new InvalidCastException($"Cannot LessThan {Type} and {other.Type}");
    }

    #endregion

    #region Operator LessThanEqual

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Value operator <=(Value a, Value b) => a.Operator_LessThanEqual(b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value Operator_LessThanEqual(Value other) {
        if (Is.Int32 && other.Is.Int32) return As.Int32() <= other.As.Int32();
        if (Is.Int64 && other.Is.Int64) return As.Long() <= other.As.Long();
        if (Is.Float && other.Is.Float) return As.Float() <= other.As.Float();
        if (Is.Double && other.Is.Double) return As.Double() <= other.As.Double();

        return Dispatch_Operator_LessThanEqual(other);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Value Dispatch_Operator_LessThanEqual(Value other) {
        if (Is.String && other.Is.String) {
            return string.Compare(As.String(), other.As.String(), StringComparison.Ordinal) <= 0;
        }
        if (DispatchCall("__lte", [other], out var result)) {
            return result;
        }
        throw new InvalidCastException($"Cannot LessThanEqual {Type} and {other.Type}");
    }

    #endregion

    #region Operator GreaterThan

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Value operator >(Value a, Value b) => a.Operator_GreaterThan(b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value Operator_GreaterThan(Value other) {
        if (Is.Int32 && other.Is.Int32) return As.Int32() > other.As.Int32();
        if (Is.Int64 && other.Is.Int64) return As.Long() > other.As.Long();
        if (Is.Float && other.Is.Float) return As.Float() > other.As.Float();
        if (Is.Double && other.Is.Double) return As.Double() > other.As.Double();

        return Dispatch_Operator_GreaterThan(other);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Value Dispatch_Operator_GreaterThan(Value other) {
        if (Is.String && other.Is.String) {
            return string.Compare(As.String(), other.As.String(), StringComparison.Ordinal) > 0;
        }
        if (DispatchCall("__gt", [other], out var result)) {
            return result;
        }
        throw new InvalidCastException($"Cannot GreaterThan {Type} and {other.Type}");
    }

    #endregion

    #region Operator GreaterThanEqual

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Value operator >=(Value a, Value b) => a.Operator_GreaterThanEqual(b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value Operator_GreaterThanEqual(Value other) {
        if (Is.Int32 && other.Is.Int32) return As.Int32() >= other.As.Int32();
        if (Is.Int64 && other.Is.Int64) return As.Long() >= other.As.Long();
        if (Is.Float && other.Is.Float) return As.Float() >= other.As.Float();
        if (Is.Double && other.Is.Double) return As.Double() >= other.As.Double();


        return Dispatch_Operator_GreaterThanEqual(other);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Value Dispatch_Operator_GreaterThanEqual(Value other) {
        if (Is.String && other.Is.String) {
            return string.Compare(As.String(), other.As.String(), StringComparison.Ordinal) >= 0;
        }
        if (DispatchCall("__gte", [other], out var result)) {
            return result;
        }
        throw new InvalidCastException($"Cannot GreaterThanEqual {Type} and {other.Type}");
    }

    #endregion

    #region Operator Not

    /// <summary>
    /// The `!` operator (logical not)
    /// Ex: `var a = true; var b = !a;` // b is false
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value Operator_ConditionalNot() {
        if (Is.Boolean)
            return !As.Bool();

        return Dispatch_Operator_ConditionalNot();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Value Dispatch_Operator_ConditionalNot() {
        if (DispatchCall("__cond_not", [], out var result)) {
            return result;
        }

        return IsTruthy() ? False() : True();
    }

    #endregion

    #region Operator And

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value Operator_ConditionalAnd(Value other) {
        if (Is.Boolean && other.Is.Boolean)
            return As.Bool() && other.As.Bool();

        return Dispatch_Operator_ConditionalAnd(other);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Value Dispatch_Operator_ConditionalAnd(Value other) {
        if (DispatchCall("__cond_and", [other], out var result)) {
            return result;
        }

        if (IsTruthy() && other.IsTruthy()) {
            return True();
        }
        return False();
    }

    #endregion

    #region Operator Or

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value Operator_ConditionalOr(Value other) {
        if (Is.Boolean && other.Is.Boolean)
            return As.Bool() || other.As.Bool();


        return Dispatch_Operator_ConditionalOr(other);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Value Dispatch_Operator_ConditionalOr(Value other) {

        if (DispatchCall("__cond_or", [other], out var result)) {
            return result;
        }

        return IsTruthy() || other.IsTruthy() ? True() : False();
    }

    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool DispatchCall(string name, Value[] args, out Value result) {
        // if (_context == null) {
        //     throw new InvalidOperationException("Cannot call a function without a context");
        // }
        if (Get_Field(name, RTVT.Function, out var fn)) {
            result = fn.As.Fn().Call(_context, this, args);
            return true;
        }

        result = Null();
        return false;
    }

}