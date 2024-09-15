using System.Runtime.CompilerServices;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Execution.Statements;
using CSScriptingLang.Parsing.AST;
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
        var fnCtx = ctx as FunctionExecContext;

        if (pushScope)
            ctx.PushScope();

        onBeforeExecuteBlock?.Invoke();

        foreach (var n in Nodes) {
            if(n is Statement stmt) {
                stmt.Execute(ctx);
                continue;
            }
            if(n is Expression expr) {
                expr.Execute(ctx);
                continue;
            }
            
            ctx.LogError(n, $"Invalid statement: {n.GetType().ToFullLinkedName()}");
        }

        onAfterExecuteBlock?.Invoke();

        if (pushScope)
            ctx.PopScope();
    }
}