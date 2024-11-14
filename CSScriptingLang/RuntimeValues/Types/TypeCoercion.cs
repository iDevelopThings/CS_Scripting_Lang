using CSScriptingLang.RuntimeValues.Values;

namespace CSScriptingLang.RuntimeValues.Types;

public class TypeCoercion
{
    public static (RTVT commonType, (Value a, Value b)) GetCommonType(Value valueA, Value valueB, bool testPrototype = true) {
        var typeA = valueA.FullType;
        var typeB = valueB.FullType;

        // Highest precedence
        if (typeA == RTVT.String || typeB == RTVT.String)
            return (RTVT.String, (valueA, valueB));
        // Next highest
        if (typeA == RTVT.Double || typeB == RTVT.Double)
            return (RTVT.Double, (valueA, valueB));
        if (typeA == RTVT.Float || typeB == RTVT.Float)
            return (RTVT.Float, (valueA, valueB));
        if (typeA == RTVT.Int64 || typeB == RTVT.Int64)
            return (RTVT.Int64, (valueA, valueB));
        if (typeA == RTVT.Int32 || typeB == RTVT.Int32)
            return (RTVT.Int32, (valueA, valueB));
        // Lowest precedence
        if (typeA == RTVT.Boolean || typeB == RTVT.Boolean)
            return (RTVT.Boolean, (valueA, valueB));

        if (testPrototype) {
            return GetCommonType(valueA.Prototype, valueB.Prototype, false);
        }

        throw new InvalidOperationException($"Cannot determine common type for {typeA} and {typeB}");
    }

    public static (bool CanCast, Func<Value, Value> Cast) GetCaster(Value a, RTVT type) {
        var proto = a.FullType.Prototype();
        if (proto != null) {
            return proto.GetCaster(type);
        }
        return (false, null);
    }

    public static bool CanCastTo<T>(Value a) => CanCastTo(a, typeof(T));

    public static bool CanCastTo(Value a, Type t) => CanCastTo(a, RTVTUtil.FromType(t));
    public static bool CanCastTo(Value a, RTVT type) {
        var (canCast, _) = GetCaster(a, type);
        return canCast;
    }


    public static bool CastTo<T>(Value a, out Value result) => CastTo(a, typeof(T), out result);

    public static bool CastTo(Value a, Type t, out Value result) => CastTo(a, RTVTUtil.FromType(t), out result);

    public static bool CastTo(Value a, RTVT type, out Value result) {
        var (canCast, cast) = GetCaster(a, type);
        if (!canCast) {
            result = null;
            return false;
        }

        result = cast(a);
        return true;
    }

    public static Value CastTo<T>(Value value)            => CastTo<T>(value, out var result) ? result : null;
    public static Value CastTo(Value    value, Type t)    => CastTo(value, t, out var result) ? result : null;
    public static Value CastTo(Value    value, RTVT type) => CastTo(value, type, out var result) ? result : null;

    public static bool TryCoerce(Value inA, Value inB, out RTVT commonType, out Value a, out Value b) {
        var (ct, vals) = GetCommonType(inA, inB);
        commonType = ct;
        inA = vals.a;
        inB = vals.b;
        
        var (canCast, castA)  = GetCaster(inA, commonType);
        var (canCastB, castB) = GetCaster(inB, commonType);

        // var (canCast, castA)  = GetCaster((inA.Type == RTVT.ValueReference ? inA.GetUntypedValue<Value>() : inA), commonType);
        // var (canCastB, castB) = GetCaster((inB.Type == RTVT.ValueReference ? inB.GetUntypedValue<Value>() : inB), commonType);

        if (!canCast || !canCastB) {
            a = null;
            b = null;
            return false;
        }

        a = castA(inA);
        b = castB(inB);

        return true;
    }
}