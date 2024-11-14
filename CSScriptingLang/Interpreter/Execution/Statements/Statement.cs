using CSScriptingLang.Core.Diagnostics;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.Lexing;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.RuntimeValues.Values;
using CSScriptingLang.Utils;
using SharpX;

namespace CSScriptingLang.Interpreter.Execution.Statements;

[ASTNode]
public partial class Statement : BaseNode, IExecutableVoid
{
    public virtual async Task<Maybe<ValueReference>> ExecuteAsync(ExecContext ctx) {
        return await Task.FromResult(Execute(ctx));
    }

    public virtual async IAsyncEnumerable<Maybe<ValueReference>> ExecuteMultiAsync(ExecContext ctx) {
        yield return await Task.FromResult(await ExecuteAsync(ctx));
    }

    public virtual IEnumerable<Maybe<ValueReference>> ExecuteEnumerable(ExecContext ctx) {
        yield return Execute(ctx);
    }
    
    public virtual Maybe<ValueReference> Execute(ExecContext ctx) {
        DiagnosticManager.Diagnostic_Error_Fatal().Message($"Statement.Execute not implemented for {GetType().ToFullLinkedName()}").Range(this).Report();
        return Maybe.Nothing<ValueReference>();
    }

    public virtual IEnumerable<Maybe<ValueReference>> ExecuteMulti(ExecContext ctx) {
        yield return Execute(ctx);
    }
}

[ASTNode]
public partial class StatementExpression : Expression
{
    [VisitableNodeProperty]
    public Statement Statement { get; set; }

    public StatementExpression(Statement statement) {
        Statement  = statement;
        StartToken = statement.StartToken;
        EndToken   = statement.EndToken;
    }

    public static implicit operator StatementExpression(Statement statement)           => new(statement);
    public static implicit operator Statement(StatementExpression statementExpression) => statementExpression.Statement;

    public override ValueReference Execute(ExecContext ctx) {
        var stmtResult = Statement.Execute(ctx);

        if (stmtResult.MatchJust(out var value)) {
            return value;
        }

        return new ValueReference(ctx, Value.Null());
    }

}


[ASTNode]
public partial class ExpressionStatement : Statement
{
    [VisitableNodeProperty]
    public Expression Expression { get; set; }

    public ExpressionStatement(Expression expression) {
        Expression = expression;
        StartToken = expression.StartToken;
        EndToken   = expression.EndToken;
    }

    public static implicit operator ExpressionStatement(Expression expression)          => new(expression);
    public static implicit operator Expression(ExpressionStatement expressionStatement) => expressionStatement.Expression;

    public override Maybe<ValueReference> Execute(ExecContext ctx) {
        return Expression.Execute(ctx).ToMaybe();
    }

}