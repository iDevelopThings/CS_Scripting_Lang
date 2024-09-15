using CSScriptingLang.Parsing.AST;
using CSScriptingLang.RuntimeValues.Values;

namespace CSScriptingLang.Interpreter.Context;

public struct ValueReference
{

    public enum RefKind
    {
        Value,

        MemberAccess,
        IndexAccess,
        Variable,
    }

    private ExecContext Ctx { get; set; }

    public ValueReference(ExecContext ctx) {
        Ctx      = ctx;
        Variable = null;
        Object   = null;
        Key      = null;
    }
    public ValueReference(ExecContext ctx, Value val) {
        Ctx      = ctx;
        Variable = null;
        Object   = val;
        Key      = null;
    }

    public VariableSymbol Variable { get; set; }

    public Value Object { get; set; }

    // Used for member access and index access
    public Value Key { get; set; }

    public Value Value {
        get {
            switch (Kind) {
                case RefKind.Value:
                    return Object;
                case RefKind.Variable:
                    return Variable.Val;
                case RefKind.MemberAccess:
                case RefKind.IndexAccess: {
                    var member = Object.GetMember(Key);
                    if (member?.Is.IsInstanceGetterFn() == true) {
                        return member.As.Fn().Call(Ctx, Object);
                    }
                    return member;
                }
                default:
                    throw new InvalidOperationException("Cannot get value from non-variable reference");
            }
        }
    }

    public RefKind Kind { get; set; }


    public static ValueReference MemberAccess(VariableSymbol variable, Value key, ExecContext ctx)
        => new(ctx) {
            Variable = variable,
            Object   = variable.Val,
            Key      = key,
            Kind     = RefKind.MemberAccess,
        };
    public static ValueReference MemberAccess(Value obj, Value key, ExecContext ctx)
        => new(ctx) {
            Object = obj,
            Key    = key,
            Kind   = RefKind.MemberAccess,
        };

    public static ValueReference IndexAccess(Value obj, Value index, ExecContext ctx)
        => new(ctx) {
            Object = obj,
            Key    = index,
            Kind   = RefKind.IndexAccess,
        };

    public static ValueReference VariableAccess(VariableSymbol variable, ExecContext ctx)
        => new(ctx) {
            Variable = variable,
            Kind     = RefKind.Variable,
        };

    public (VariableSymbol variable, Value val) Get() => Kind switch {
        RefKind.Variable => (Variable, Variable.Val),
        _                => throw new InvalidOperationException("Cannot get value from non-variable reference")
    };

    public void SetValue(Value val) {
        switch (Kind) {
            case RefKind.Value:
                Object.SetValue(val);
                break;
            case RefKind.Variable:
                Variable.Val = val;
                break;
            case RefKind.MemberAccess:
            case RefKind.IndexAccess:
                Object.SetMember(Key, val);
                break;
        }
    }
}