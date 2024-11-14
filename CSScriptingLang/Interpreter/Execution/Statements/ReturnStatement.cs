using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.RuntimeValues.Values;
using SharpX;

namespace CSScriptingLang.Interpreter.Execution.Statements;

[ASTNode]
public partial class ReturnStatement : Statement, IExecutableVoid
{
    [VisitableNodeProperty]
    public Expression ReturnValue { get; }

    public ReturnStatement(Expression returnValue) {
        ReturnValue = returnValue;
    }

    public override Maybe<ValueReference> Execute(ExecContext ctx) {
        var fnCtx = ctx as FunctionExecContext;


        if (ReturnValue == null) {
            ctx.Return();
            return Maybe.Nothing<ValueReference>();
        }
        
        var rtValue = ReturnValue.Execute(ctx);
        ctx.Return(rtValue);
        
        return Maybe.Nothing<ValueReference>();
        
        /*
        var caller = fnCtx?.Caller;

        if (ReturnValue == null) {
            var v = ctx.ValReference(Value.Unit());
            if (fnCtx != null) {
                fnCtx.ReturnValues.Add(v);
            }
            return v.ToMaybe();
        }

        if (caller != null && ReturnValue is CallExpression ce) {
            if (caller.Name == ce.Name) {
                fnCtx.TailCallExpression = ce;

                var v = ctx.ValReference(Value.Unit());
                fnCtx.ReturnValues.Add(v);
                return v.ToMaybe();
            }
        }

        var rtValue = ReturnValue.Execute(ctx);


        if (fnCtx != null) {
            fnCtx.ReturnValues.Add(rtValue);
        }

        return rtValue.ToMaybe();*/
    }
}

[ASTNode]
public partial class BreakStatement : Statement, IExecutableVoid
{
    [VisitableNodeProperty]
    public Int32Expression Count { get; }

    public BreakStatement(Int32Expression count) {
        Count = count;
    }

    public override Maybe<ValueReference> Execute(ExecContext ctx) {
        var count = Count?.Execute(ctx) ?? ctx.ValReference(Value.Number(1));
        ctx.Break(count);

        return ctx.ValReference(Value.Unit()).ToMaybe();
    }
}

[ASTNode]
public partial class ContinueStatement : Statement, IExecutableVoid
{
    public ContinueStatement() { }

    public override Maybe<ValueReference> Execute(ExecContext ctx) {
        ctx.Continue();
        return ctx.ValReference(Value.Unit()).ToMaybe();
    }
}