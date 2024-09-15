using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.RuntimeValues.Values;


namespace CSScriptingLang.Interpreter.Execution.Expressions;

[ASTNode]
public partial class RangeExpression : Expression
{
    [VisitableNodeProperty]
    public Expression Expression { get; }

    public RangeExpression(Expression expression) {
        Expression = expression;
    }

    public override IEnumerable<ValueReference> ExecuteMulti(ExecContext ctx) {
        yield return ctx.ValReference(Value.Number(0));
        yield return Expression.Execute(ctx);
    }
}