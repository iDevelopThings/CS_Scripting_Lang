using System.Runtime.CompilerServices;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Execution.Statements;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.RuntimeValues.Values;
using CSScriptingLang.Utils;
using SharpX;

namespace CSScriptingLang.Interpreter.Execution.Expressions;

[ASTNode]
public partial class BlockExpression : NodeList<BaseNode>
{
    // public (IEnumerable<Expression>, IEnumerable<Statement>) PartitionedNodes => Nodes.Partition();
    // public List<Statement>  Statements  => PartitionedNodes.Item2.ToList();
    // public List<Expression> Expressions => PartitionedNodes.Item1.ToList();

    [VisitableNodeProperty]
    public ReturnStatement ReturnNode { get; set; }

    public BlockExpression() { }
    public BlockExpression(IEnumerable<BaseNode> statements) : base(statements) { }

    /*public override NodeList<Either<Expression, Statement>> Add(BaseNode node, [CallerMemberName] string caller = "") {
        if (node is Expression expr) {
            Nodes.Add(Either.Left<Expression, Statement>(expr));
            return this;
        }

        if (node is Statement stmt) {
            Nodes.Add(Either.Right<Expression, Statement>(stmt));
        }

        throw new Exception($"Invalid node type: {node.GetType().ToFullLinkedName()}");
    }*/

    public void Execute(
        ExecContext ctx,
        bool        pushScope            = true,
        Action      onBeforeExecuteBlock = null,
        Action      onAfterExecuteBlock  = null
    ) {
        using var _  = ctx.UsingScope(pushScope);
        using var __ = ctx.UsingBlockCallbacks(onBeforeExecuteBlock, onAfterExecuteBlock);

        foreach (var n in Nodes) {
            if (n is Statement stmt) {
                stmt.Execute(ctx);
            } else if (n is Expression expr) {
                expr.Execute(ctx).ToMaybe();
            } else if (n is BlockExpression block) {
                block.Execute(ctx);
            } else {
                ctx.LogError(n, $"Invalid statement: {n.GetType().ToFullLinkedName()}");
            }
        }

    }

    public IEnumerable<Maybe<ValueReference>> ExecuteEnumerable(
        ExecContext ctx,
        bool        pushScope            = true,
        Action      onBeforeExecuteBlock = null,
        Action      onAfterExecuteBlock  = null
    ) {
        using var _  = ctx.UsingScope(pushScope);
        using var __ = ctx.UsingBlockCallbacks(onBeforeExecuteBlock, onAfterExecuteBlock);

        foreach (var n in Nodes) {
            switch (n) {
                case Statement stmt: {
                    foreach (var val in stmt.ExecuteEnumerable(ctx))
                        yield return val;

                    break;
                }
                case Expression expr:
                    foreach (var val in expr.ExecuteEnumerable(ctx))
                        yield return val;

                    break;
                case BlockExpression block:
                    foreach (var val in block.ExecuteEnumerable(ctx))
                        yield return val;

                    break;
                default:
                    ctx.LogError(n, $"Invalid statement: {n.GetType().ToFullLinkedName()}");
                    break;
            }
        }

    }
}

[ASTNode]
public partial class BlockExpressionWrapper : Expression
{
    [VisitableNodeProperty]
    public BlockExpression Block { get; set; }

    public BlockExpressionWrapper(BlockExpression block) {
        Block      = block;
        StartToken = block.StartToken;
        EndToken   = block.EndToken;
    }

    public override ValueReference Execute(ExecContext ctx) {
        Block.Execute(ctx);
        return new ValueReference(ctx, Value.Null());
    }

}