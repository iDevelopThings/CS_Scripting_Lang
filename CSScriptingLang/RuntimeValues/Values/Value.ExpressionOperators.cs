using System.Diagnostics;
using System.Runtime.CompilerServices;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Lexing;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.Utils;

namespace CSScriptingLang.RuntimeValues.Values;

public partial class Value
{

    public Value Operator(OperatorType op, Value other) {
        return op switch {
            OperatorType.Plus       => Operator_Add(other),
            OperatorType.PlusEquals => Operator_AddAssign(other),

            OperatorType.Minus       => Operator_Sub(other),
            OperatorType.MinusEquals => Operator_SubAssign(other),

            OperatorType.Multiply           => Operator_Mul(other),
            OperatorType.Divide             => Operator_Div(other),
            OperatorType.Modulus            => Operator_Mod(other),
            OperatorType.Pow                => Operator_Pow(other),
            OperatorType.LessThan           => Operator_LessThan(other),
            OperatorType.LessThanOrEqual    => Operator_LessThanEqual(other),
            OperatorType.GreaterThan        => Operator_GreaterThan(other),
            OperatorType.GreaterThanOrEqual => Operator_GreaterThanEqual(other),

            OperatorType.Equals          => Operator_Equal_Boxed(other, false),
            OperatorType.EqualsStrict    => Operator_Equal_Boxed(other, true),
            OperatorType.NotEquals       => Operator_NotEqual_Boxed(other, false),
            OperatorType.NotEqualsStrict => Operator_NotEqual_Boxed(other, true),

            OperatorType.BitwiseAnd => Operator_BitwiseAnd(other),
            OperatorType.And        => Operator_ConditionalAnd(other),

            OperatorType.Pipe => Operator_ConditionalOr(other),
            OperatorType.Or   => Operator_ConditionalOr(other),

            OperatorType.Not => Operator_ConditionalNot(),

            OperatorType.Increment => Operator_Increment(),
            OperatorType.Decrement => Operator_Decrement(),

            _ => throw new InterpreterRuntimeException($"Invalid/Unhandled operator lhs={Type} op={op} rhs={other?.Type}"),
        };
    }

    #region Operator Equals

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerStepThrough]
    private Value Operator_Equal_Boxed(Value other, bool isStrict) => Operator_Equal(other, isStrict);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Operator_Equal(Value other, bool isStrict) {
        if (other == null) {
            return false;
        }

        if (FullType == other?.FullType || isStrict) {
            var aV = FullValue;
            var bV = other.FullValue;
            var eq = Equals(aV, bV);
            return eq;
        }

        if (TypeCoercion.TryCoerce(this, other, out var commonType, out var a, out var b)) {
            switch (commonType) {
                case RTVT.String:   return ((string) a == (string) b);
                case RTVT.Int32:    return ((int) a == (int) b);
                case RTVT.Int64:    return ((long) a == (long) b);
                case RTVT.Float:    return ((float) a == (float) b);
                case RTVT.Double:   return ((double) a == (double) b);
                case RTVT.Boolean:  return ((bool) a == (bool) b);
                case RTVT.Function: return ReferenceEquals(a.As.Fn(), b.As.Fn());
                case RTVT.Array:    return ReferenceEquals(a.As.List(), b.As.List());
                case RTVT.Object: {
                    if (DispatchCall(OperatorType.Equals, [other], out var objResult)) {
                        return objResult.As.Bool();
                    }

                    return ReferenceEquals(a.Members, b.Members);
                }
                default: {
                    if (DispatchCall(OperatorType.Equals, [other], out var result)) {
                        return result.IsTruthy();
                    }

                    return Type == other.Type;
                }
            }
        }

        /*switch (FullType) {
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
                if (DispatchCall(OperatorType.Equals, [other], out var objResult)) {
                    return objResult.As.Bool();
                }

                return other?.Is.Object == true && ReferenceEquals(Members, other.Members);
            }

            default: {
                if (DispatchCall(OperatorType.Equals, [other], out var result)) {
                    return result.IsTruthy();
                }

                return Type == other.Type;
            }

        }*/

        throw new InterpreterRuntimeException($"Cannot compare {Type} and {other.Type}");
    }

    #endregion

    #region Operator NotEqual

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerStepThrough]
    public Value Operator_NotEqual_Boxed(Value other, bool isStrict) => Operator_NotEqual(other, isStrict);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Operator_NotEqual(Value other, bool isStrict) {
        if (other == null) {
            return true;
        }

        if (Is.Object) {
            if (DispatchCall(OperatorType.NotEquals, [other], out var objResult)) {
                return objResult.As.Bool();
            }
        }

        return !Operator_Equal(other, isStrict);
    }

    #endregion

    #region Operator Assign

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value Operator_Assign(Value other) {
        if (DispatchCall(OperatorType.Assignment, [other], out var result)) {
            return result;
        }

        if (SetValue(other, false)) {
            return this;
        }

        throw new InterpreterRuntimeException($"Cannot Assign {Type} and {other.Type}");
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

        if (DispatchCall(OperatorType.Increment.GetOverloadFnName(), [], out var result)) {
            return result;
        }

        throw new InterpreterRuntimeException($"Cannot Increment {Type}");
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

        if (DispatchCall(OperatorType.Decrement, [], out var result)) {
            return result;
        }
        throw new InterpreterRuntimeException($"Cannot Decrement {Type}");
    }

    #endregion

    #region Operator Add

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Value operator +(Value a, Value b) => a.Operator_Add(b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value Operator_Add(Value other) {
        if (TypeCoercion.TryCoerce(this, other, out var commonType, out var a, out var b)) {
            switch (commonType) {
                case RTVT.String:  return ((string) a + (string) b);
                case RTVT.Int32:   return ((int) a + (int) b);
                case RTVT.Int64:   return ((long) a + (long) b);
                case RTVT.Float:   return ((float) a + (float) b);
                case RTVT.Double:  return ((double) a + (double) b);
                case RTVT.Boolean: return ((bool) a ? 1 : 0) + ((bool) b ? 1 : 0);
            }
        }

        /*var commonType = TypeCoercion.GetCommonType(FullType, other.FullType);
        var a          = TypeCoercion.CastTo(this, commonType);
        var b          = TypeCoercion.CastTo(other, commonType);

        switch (commonType) {
            case RTVT.String:  return ((string) a + (string) b);
            case RTVT.Int32:   return ((int) a + (int) b);
            case RTVT.Int64:   return ((long) a + (long) b);
            case RTVT.Float:   return ((float) a + (float) b);
            case RTVT.Double:  return ((double) a + (double) b);
            case RTVT.Boolean: return ((bool) a ? 1 : 0) + ((bool) b ? 1 : 0);
        }
        */

        if (DispatchCall(OperatorType.Plus, [other], out var result)) {
            return result;
        }

        throw new InterpreterRuntimeException($"Cannot Add {Type} and {other.Type}");
    }

    #endregion

    #region Operator AddAssign

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value Operator_AddAssign(Value other) {
        if (Is.Signal && other.Is.Function) {
            other.ToDebugString();
            As.Signal().AddListener(other);
            return this;
        }

        return Operator_Add(other);
    }

    #endregion

    #region Operator Sub

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Value operator -(Value a, Value b) => a.Operator_Sub(b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value Operator_Sub(Value other) {

        if (TypeCoercion.TryCoerce(this, other, out var commonType, out var a, out var b)) {
            switch (commonType) {
                case RTVT.Int32:   return ((int) a - (int) b);
                case RTVT.Int64:   return ((long) a - (long) b);
                case RTVT.Float:   return ((float) a - (float) b);
                case RTVT.Double:  return ((double) a - (double) b);
                case RTVT.Boolean: return ((bool) a ? 1 : 0) - ((bool) b ? 1 : 0);
            }
        }


        // if (Is.Int32) return As.Int() - other.As.Int();
        // if (Is.Int64) return As.Long() - other.As.Long();
        // if (Is.Float) return As.Float() - other.As.Float();
        // if (Is.Double) return As.Double() - other.As.Double();

        if (DispatchCall(OperatorType.Minus, [other], out var result)) {
            return result;
        }

        throw new InterpreterRuntimeException($"Cannot Sub {Type} and {other.Type}");
    }

    #endregion

    #region Operator SubAssign

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value Operator_SubAssign(Value other) {
        if (Is.Signal && other.Is.Function) {
            As.Signal().RemoveListener(other);
            return this;
        }
        return Operator_Sub(other);
    }

    #endregion

    #region Operator Mul

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Value operator *(Value a, Value b) => a.Operator_Mul(b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value Operator_Mul(Value other) {
        if (TypeCoercion.TryCoerce(this, other, out var commonType, out var a, out var b)) {
            switch (commonType) {
                case RTVT.Int32:   return ((int) a * (int) b);
                case RTVT.Int64:   return ((long) a * (long) b);
                case RTVT.Float:   return ((float) a * (float) b);
                case RTVT.Double:  return ((double) a * (double) b);
                case RTVT.Boolean: return ((bool) a ? 1 : 0) * ((bool) b ? 1 : 0);
            }
        }

        // if (Is.Int32) return As.Int() * other.As.Int();
        // if (Is.Int64) return As.Long() * other.As.Long();
        // if (Is.Float) return As.Float() * other.As.Float();
        // if (Is.Double) return As.Double() * other.As.Double();

        if (DispatchCall(OperatorType.Multiply, [other], out var result)) {
            return result;
        }

        throw new InterpreterRuntimeException($"Cannot Mul {Type} and {other.Type}");
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
        }

        if (TypeCoercion.TryCoerce(this, other, out var commonType, out var a, out var b)) {
            switch (commonType) {
                case RTVT.Int32:   return ((int) a / (int) b);
                case RTVT.Int64:   return ((long) a / (long) b);
                case RTVT.Float:   return ((float) a / (float) b);
                case RTVT.Double:  return ((double) a / (double) b);
                case RTVT.Boolean: return ((bool) a ? 1 : 0) / ((bool) b ? 1 : 0);
            }
        }

        if (DispatchCall(OperatorType.Divide, [other], out var result)) {
            return result;
        }
        throw new InterpreterRuntimeException($"Cannot Div {Type} and {other.Type}");
    }

    #endregion

    #region Operator Mod

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Value operator %(Value a, Value b) => a.Operator_Mod(b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value Operator_Mod(Value other) {

        if (TypeCoercion.TryCoerce(this, other, out var commonType, out var a, out var b)) {
            switch (commonType) {
                case RTVT.Int32:   return ((int) a % (int) b);
                case RTVT.Int64:   return ((long) a % (long) b);
                case RTVT.Float:   return ((float) a % (float) b);
                case RTVT.Double:  return ((double) a % (double) b);
                case RTVT.Boolean: return ((bool) a ? 1 : 0) % ((bool) b ? 1 : 0);
            }
        }

        if (DispatchCall(OperatorType.Modulus, [other], out var result)) {
            return result;
        }

        throw new InterpreterRuntimeException($"Cannot Mod {Type} and {other.Type}");
    }

    #endregion

    #region Operator Pow

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value Operator_Pow(Value other) {
        if (TypeCoercion.TryCoerce(this, other, out var commonType, out var a, out var b)) {
            switch (commonType) {
                case RTVT.Int32:  return (int) Math.Pow((int) a, (int) b);
                case RTVT.Int64:  return (long) Math.Pow((long) a, (long) b);
                case RTVT.Float:  return (float) Math.Pow((float) a, (float) b);
                case RTVT.Double: return Math.Pow((double) a, (double) b);
            }
        }

        if (DispatchCall(OperatorType.Pow, [other], out var result)) {
            return result;
        }

        throw new InterpreterRuntimeException($"Cannot Pow {Type} and {other.Type}");
    }

    #endregion

    #region Operator LessThan

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Value operator <(Value a, Value b) => a.Operator_LessThan(b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value Operator_LessThan(Value other) {
        if (TypeCoercion.TryCoerce(this, other, out var commonType, out var a, out var b)) {
            switch (commonType) {
                case RTVT.Int32:  return ((int) a < (int) b);
                case RTVT.Int64:  return ((long) a < (long) b);
                case RTVT.Float:  return ((float) a < (float) b);
                case RTVT.Double: return ((double) a < (double) b);
                case RTVT.String: return string.Compare((string) a, (string) b, StringComparison.Ordinal) < 0;
            }
        }

        if (DispatchCall(OperatorType.LessThan, [other], out var result)) {
            return result;
        }

        throw new InterpreterRuntimeException($"Cannot LessThan {Type} and {other.Type}");
    }

    #endregion

    #region Operator LessThanEqual

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Value operator <=(Value a, Value b) => a.Operator_LessThanEqual(b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value Operator_LessThanEqual(Value other) {
        if (TypeCoercion.TryCoerce(this, other, out var commonType, out var a, out var b)) {
            switch (commonType) {
                case RTVT.Int32:  return ((int) a <= (int) b);
                case RTVT.Int64:  return ((long) a <= (long) b);
                case RTVT.Float:  return ((float) a <= (float) b);
                case RTVT.Double: return ((double) a <= (double) b);
                case RTVT.String: return string.Compare((string) a, (string) b, StringComparison.Ordinal) <= 0;
            }
        }

        if (DispatchCall(OperatorType.LessThanOrEqual, [other], out var result)) {
            return result;
        }

        throw new InterpreterRuntimeException($"Cannot LessThanEqual {Type} and {other.Type}");
    }

    #endregion

    #region Operator GreaterThan

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Value operator >(Value a, Value b) => a.Operator_GreaterThan(b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value Operator_GreaterThan(Value other) {
        if (TypeCoercion.TryCoerce(this, other, out var commonType, out var a, out var b)) {
            switch (commonType) {
                case RTVT.Int32:  return ((int) a > (int) b);
                case RTVT.Int64:  return ((long) a > (long) b);
                case RTVT.Float:  return ((float) a > (float) b);
                case RTVT.Double: return ((double) a > (double) b);
                case RTVT.String: return string.Compare((string) a, (string) b, StringComparison.Ordinal) > 0;
            }
        }

        if (DispatchCall(OperatorType.GreaterThan, [other], out var result)) {
            return result;
        }

        throw new InterpreterRuntimeException($"Cannot GreaterThan {Type} and {other.Type}");
    }

    #endregion

    #region Operator GreaterThanEqual

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Value operator >=(Value a, Value b) => a.Operator_GreaterThanEqual(b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value Operator_GreaterThanEqual(Value other) {
        if (TypeCoercion.TryCoerce(this, other, out var commonType, out var a, out var b)) {
            switch (commonType) {
                case RTVT.Int32:  return ((int) a >= (int) b);
                case RTVT.Int64:  return ((long) a >= (long) b);
                case RTVT.Float:  return ((float) a >= (float) b);
                case RTVT.Double: return ((double) a >= (double) b);
                case RTVT.String: return string.Compare((string) a, (string) b, StringComparison.Ordinal) >= 0;
            }
        }

        if (DispatchCall(OperatorType.GreaterThanOrEqual, [other], out var result)) {
            return result;
        }
        throw new InterpreterRuntimeException($"Cannot GreaterThanEqual {Type} and {other.Type}");
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
            return !GetUntypedValue<bool>();

        if (TypeCoercion.CastTo(this, RTVT.Boolean, out var casted)) {
            return !casted.GetUntypedValue<bool>();
        }

        if (DispatchCall(OperatorType.Not, [], out var result)) {
            return result;
        }

        return IsTruthy() ? False() : True();
    }

    #endregion

    #region Operator And

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value Operator_ConditionalAnd(Value other) {
        var (commonType, vals) = TypeCoercion.GetCommonType(this, other);

        switch (commonType) {
            case RTVT.Int32:
            case RTVT.Int64:
            case RTVT.Float:
            case RTVT.Double:
            case RTVT.String:
            case RTVT.Boolean: {
                var a = vals.a.CastTo(RTVT.Boolean);
                // if the first operand is false, return false
                if (!a) {
                    return False();
                }

                // Short-circuiting passed; evaluate the second operand
                var b = vals.b.CastTo(RTVT.Boolean);
                return b ? True() : False();
            }
        }

        if (DispatchCall(OperatorType.And, [other], out var result)) {
            return result;
        }

        throw new InterpreterRuntimeException($"Cannot eval ({Type} && {other.Type})");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value Operator_BitwiseAnd(Value other) {
        if (TypeCoercion.TryCoerce(this, other, out var commonType, out var a, out var b)) {
            switch (commonType) {
                case RTVT.Int32:   return ((int) a & (int) b);
                case RTVT.Int64:   return ((long) a & (long) b);
                case RTVT.Boolean: return ((bool) a && (bool) b);
            }
        }

        if (DispatchCall(OperatorType.BitwiseAnd, [other], out var result)) {
            return result;
        }

        throw new InterpreterRuntimeException($"Cannot eval ({Type} & {other.Type})");
    }

    #endregion

    #region Operator Or

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value Operator_ConditionalOr(Value other) {
        if (TypeCoercion.TryCoerce(this, other, out var commonType, out var a, out var b)) {
            switch (commonType) {
                case RTVT.Int32:   return ((int) a | (int) b);
                case RTVT.Int64:   return ((long) a | (long) b);
                case RTVT.String:  return string.Compare((string) a, (string) b, StringComparison.Ordinal) >= 0;
                case RTVT.Boolean: return ((bool) a || (bool) b);
            }
        }

        if (DispatchCall(OperatorType.Or, [other], out var result)) {
            return result;
        }

        return IsTruthy() || other.IsTruthy() ? True() : False();
    }

    #endregion

    private bool _suppressDispatch = false;
    public UsingCallbackHandle SuppressDispatch() {
        _suppressDispatch = true;
        return new UsingCallbackHandle(() => _suppressDispatch = false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerStepThrough]
    public bool DispatchCall(OperatorType op, Value[] args, out Value result)
        => DispatchCall(op.GetOverloadFnName(), args, out result);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value DispatchCall(ExecContext ctx, Value[] args)
        => ctx.Call(this, this, args);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Value DispatchCall(ExecContext ctx, Value instance, Value[] args)
        => ctx.Call(this, instance, args);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool DispatchCall(string name, Value[] args, out Value result) {
        if (_suppressDispatch) {
            result = Null();
            return false;
        }

        // if (_context == null) {
        //     throw new InvalidOperationException("Cannot call a function without a context");
        // }
        if (Get_Field(name, RTVT.Function, out var fn)) {
            // Add `this` as the first argument

            System.Array.Resize(ref args, args.Length + 1);
            System.Array.Copy(args, 0, args, 1, args.Length - 1);
            args[0] = this;

            var fnValue = fn.As.Fn();
            //var fnContext = new FunctionExecContext(_context) {
            //    Function = fnValue.Declaration,
            //    This     = this,
            //};

            result = _context.Call(fn, this, args);
            // result = fnValue.Call(fnContext, this, args);
            return true;
        }

        result = Null();
        return false;
    }

}