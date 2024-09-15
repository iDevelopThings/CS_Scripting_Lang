using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.Utils;
using SharpX;

namespace CSScriptingLang.Interpreter.Execution.Statements;

[ASTNode]
public partial class ForLoopStatement : Statement
{
    // let i = 0
    [VisitableNodeProperty]
    public BaseNode Initialization { get; }

    // i < 10
    [VisitableNodeProperty]
    public Expression Condition { get; }

    // i++
    [VisitableNodeProperty]
    public Expression Increment { get; }

    [VisitableNodeProperty]
    public BlockExpression Body { get; }


    public ForLoopStatement(BaseNode initialization, Expression condition, Expression increment, BlockExpression body) {
        Initialization = initialization;
        Condition      = condition;
        Increment      = increment;
        Body           = body;
    }

    public override Maybe<ValueReference> Execute(ExecContext ctx) {
        using var _ = ctx.UsingScope();

        if (Initialization != null) {
            switch (Initialization) {
                case Expression init:
                    init.Execute(ctx);
                    break;
                case VariableDeclarationNode varDecl:
                    varDecl.Execute(ctx);
                    break;
                default:
                    ctx.LogError(Initialization, $"Invalid initialization expression: {Initialization?.GetType().ToFullLinkedName()}");
                    break;
            }
        }

        while (true) {
            var cond = Condition.Execute(ctx);
            if (!cond.Value.IsTruthy())
                break;
            Body.Execute(ctx, false);

            Increment?.Execute(ctx);
        }

        return Maybe.Nothing<ValueReference>();
    }

}

[ASTNode]
public partial class ForRangeStatement : Statement
{
    [VisitableNodeProperty]
    public TupleListDeclarationNode Indexers { get; set; }

    public VariableDeclarationNode Initializer { get; set; }

    [VisitableNodeProperty]
    public RangeExpression Range { get; set; }

    [VisitableNodeProperty]
    public BlockExpression Body { get; set; }

    public ForRangeStatement() {
        Indexers = new();
        Body     = new();
    }
    public ForRangeStatement(TupleListDeclarationNode indexers, RangeExpression range, BlockExpression body) {
        Indexers = indexers;
        Range    = range;
        Body     = body;
    }

    public override Maybe<ValueReference> Execute(ExecContext ctx) {

        using var _ = ctx.UsingScope();

        var rangeResult = Range.ExecuteMulti(ctx).ToArray();

        var rangeMin   = rangeResult[0].Value;
        var rangeValue = rangeResult[1].Value;

        if (rangeMin == null || rangeValue == null) {
            ctx.LogError(this, "Failed to get range values");
            return Maybe.Nothing<ValueReference>();
        }

        if (Indexers?[0] is not VariableInitializerNode variableInitializerNode) {
            ctx.LogError(this, "Invalid loop indexer declaration");
            return Maybe.Nothing<ValueReference>();
        }

        if (variableInitializerNode.Variable is null) {
            ctx.LogError(this, "Invalid loop indexer declaration");
            return Maybe.Nothing<ValueReference>();
        }

        VariableInitializerNode loopElementDecl = null;
        if (Indexers.Nodes.Count > 1) {
            if (Indexers[1] is not VariableInitializerNode elementDecl) {
                ctx.LogError(this, "Invalid loop element declaration");
                return Maybe.Nothing<ValueReference>();
            }

            loopElementDecl = elementDecl;
        }

        var iterator = rangeValue.GetIterator();

        var            loopIndexVar   = ctx.Variables.Declare(variableInitializerNode.Name);
        VariableSymbol loopElementVar = null;

        if (loopElementDecl != null) {
            loopElementVar = ctx.Variables.Declare(loopElementDecl.Name);
        }

        while (iterator.MoveNext()) {
            loopIndexVar.Val = iterator.CurrentIndex;

            if (loopElementVar != null) {
                loopElementVar.Val = iterator.Current; // The value from the iterator
            }

            Body.Execute(ctx);
        }
        
        return Maybe.Nothing<ValueReference>();
    }
}