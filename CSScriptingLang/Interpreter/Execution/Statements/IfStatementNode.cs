using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.Parsing.AST;
using SharpX;

namespace CSScriptingLang.Interpreter.Execution.Statements;

[ASTNode]
public partial class IfStatementNode : Statement
{
    [VisitableNodeProperty]
    public Expression Condition { get; }

    [VisitableNodeProperty]
    public BlockExpression ThenBranch { get; }

    [VisitableNodeProperty]
    public BlockExpression ElseBranch { get; }

    public IfStatementNode(Expression condition, BlockExpression thenBranch, BlockExpression elseBranch = null) {
        Condition  = condition;
        ThenBranch = thenBranch;
        ElseBranch = elseBranch;
    }

    public override Maybe<ValueReference> Execute(ExecContext ctx) {
        var condition = Condition.Execute(ctx).Value;

        if (condition.IsTruthy()) {
            ThenBranch.Execute(ctx);
        } else {
            ElseBranch?.Execute(ctx);
        }
        
        return Maybe.Nothing<ValueReference>();
    }
}