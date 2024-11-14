using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.Parsing.AST;
using SharpX;

namespace CSScriptingLang.Interpreter.Execution.Statements;

[ASTNode]
public partial class YieldStatement : Statement
{
    [VisitableNodeProperty]
    public Expression Value { get; set; }

    public YieldStatement() { }
    public YieldStatement(Expression value) {
        Value = value;
    }

    public override IEnumerable<Maybe<ValueReference>> ExecuteEnumerable(ExecContext ctx) {
        yield return Execute(ctx);
    }
    public override Maybe<ValueReference> Execute(ExecContext ctx) {
        var v = Value.Execute(ctx);

        return v.ToMaybe();
    }
}