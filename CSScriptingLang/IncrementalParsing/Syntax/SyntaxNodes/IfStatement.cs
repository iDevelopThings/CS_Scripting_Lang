using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Utils;
using SharpX;

namespace CSScriptingLang.IncrementalParsing.Syntax.SyntaxNodes;

[SyntaxNode]
public partial class IfStatement(int index, SyntaxTree tree) : SyntaxNode(index, tree), IExecMulti
{
    public IEnumerable<IfClause> Clauses => ChildNodes<IfClause>();

    public IEnumerable<Maybe<ValueReference>> ExecuteMulti(ExecContext ctx) {
        foreach (var clause in Clauses) {
            bool conditionPasses;
            if (clause.Condition is not null) {
                var condition = clause.Condition.DoExecuteSingle(ctx).Value();
                conditionPasses = condition.Value.IsTruthy();
            } else {
                conditionPasses = true;
            }

            if (conditionPasses) {
                foreach (var val in clause.Body.ExecuteMulti(ctx)) {
                    yield return val;
                }
                yield break;
            }
        }
    }
}

[SyntaxNode]
public partial class IfClause(int index, SyntaxTree tree) : SyntaxNode(index, tree) /*, IExecMulti*/
{
    public Block Body => ChildNode<Block>();

    public ExprSyntax Condition => ChildNode<ExprSyntax>();

    /*public override IEnumerable<Maybe<ValueReference>> ExecuteMulti(ExecContext ctx) {
        var condition = Condition.DoExecuteSingle(ctx).Value();
        if (condition.Value.IsTruthy()) {
            foreach (var val in Body.ExecuteMulti(ctx)) {
                yield return val;
            }
        } else {
            yield return Maybe.Nothing<ValueReference>();
        }
    }*/
}