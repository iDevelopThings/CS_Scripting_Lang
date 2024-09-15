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

        if (ReturnValue == null)
            return ctx.ValReference(Value.Unit()).ToMaybe();

        var rtValue = ReturnValue.Execute(ctx);

        if (fnCtx != null) {
            fnCtx.ReturnValues.Add(rtValue);
        }

        return rtValue.ToMaybe();
    }
}