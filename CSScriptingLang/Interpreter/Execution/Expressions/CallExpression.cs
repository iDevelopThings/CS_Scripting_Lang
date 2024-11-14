using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Modules;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.RuntimeValues.Values;

namespace CSScriptingLang.Interpreter.Execution.Expressions;

[ASTNode]
public partial class CallExpression : Expression
{
    public string Name;

    [VisitableNodeProperty]
    public IdentifierExpression Identifier { get; set; }

    [VisitableNodeProperty]
    public ExpressionListNode Arguments { get; set; } = new();

    // Handles `obj.method()` | `obj['method']()` etc
    [VisitableNodeProperty]
    public Expression Variable { get; set; }

    [VisitableNodeProperty]
    public TypeParametersList TypeParameters { get; set; } = new();

    public CallExpression() { }
    public CallExpression(IdentifierExpression name, ExpressionListNode arguments = null) {
        StartToken = name.StartToken;
        Identifier = name;
        Name       = name;

        if (arguments != null)
            Arguments = arguments;
    }

    public CallExpression(Expression variable, ExpressionListNode arguments = null) {
        Variable     = variable;
        // Name      = "Error";
        if (arguments != null)
            Arguments = arguments;

        if (variable is MemberAccessExpression prop) {
            Name = prop.Identifier;
        } else if(variable is IdentifierExpression ident) {
            Name = ident.Name;
        }
        
        StartToken = variable.StartToken;
    }


    public override ValueReference Execute(ExecContext ctx) {
        using var _ = ctx.SetCaller(this);

        var fnInstRef = Variable?.Execute(ctx) ?? Identifier?.Execute(ctx) ?? ctx.ValReference(Value.Null());
        var fn        = fnInstRef.Value;
        var inst      = fnInstRef.Object;

        // var (fn, inst) = TryGetFunctionValue(this, ctx);
        if (fn == null) {
            ctx.LogError(this, $"Function '{Name}' not found");
            return ctx.ValReference(Value.Null());
        }

        if (!fn.Is.Function) {
            if (inst?.DataObject is Script script) {
                if (script.Declarations.IsExport(Name)) {
                    ctx.LogError(this, $"Value '{Name}' is not callable. It doesn't appear to be exported?");
                    return ctx.ValReference(Value.Null());
                }
            }
            
            ctx.LogError(this, $"Value '{Name}' is not callable. It is of type '{fn.Type}'");
            return ctx.ValReference(Value.Null());
        }

        var args = Arguments.Execute(ctx).Select(v => v.Value).ToArray();

        var returnValue = ctx.Call(
            fn,
            inst,
            args
        );

        return ctx.ValReference(returnValue);
    }
}