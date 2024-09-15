using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.Lexing;
using CSScriptingLang.Parsing.AST;
using SharpX;

namespace CSScriptingLang.Interpreter.Execution.Statements;

[ASTNode]
public partial class DeferStatement : Statement
{
    [VisitableNodeProperty]
    public Expression Expression { get; }

    public DeferStatement(Expression expression) {
        Expression = expression;
    }

    public override Maybe<ValueReference> Execute(ExecContext ctx) {
        if (ctx is FunctionExecContext) {
            ctx.PushDefer(Expression);
        } else {
            throw new FatalInterpreterException("Defer statement outside of function context", this);
        }

        return Maybe.Nothing<ValueReference>();
    }
}