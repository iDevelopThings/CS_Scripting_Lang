using CSScriptingLang.Interpreter;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Lexing;
using CSScriptingLang.Utils;
using SharpX;

// ReSharper disable InvalidXmlDocComment

namespace CSScriptingLang.IncrementalParsing.Syntax.SyntaxNodes;

/// <summary>
/// Represents a `while` loop
/// ```for { ... }```
/// </summary>
[SyntaxNode]
public partial class ForWhileLoop(int index, SyntaxTree tree) : SyntaxNode(index, tree), IExecSingle
{
    public Block Body => ChildNode<Block>();

    public override string DebugContent() => DataContentBuilder.Create()
        // .Add(Expr?.DebugContent())
       .ClearTrailingSpace();


    public Maybe<ValueReference> Execute(ExecContext ctx) {
        using var _ = ctx.UsingScope();

        try {
            while (true) {
                try {
                    Body.ExecuteVoid(ctx, false);
                }
                catch (ExecContext.ContinueException) { }
            }
        }
        catch (ExecContext.BreakException e) {
            if (e.Count > 1) {
                e.Count--;
                throw;
            }
        }
        
        return ValueReference.Nothing;
    }
}

/// <summary>
/// Represents a `for i` loop
/// ```for(var i = 0; i < 10; i++) { ... }```
/// </summary>
[SyntaxNode]
public partial class ForIndexedLoop(int index, SyntaxTree tree) : SyntaxNode(index, tree), IExecSingle
{
    public IEnumerable<SyntaxNode> Parts => ChildNodesSeparatedByToken(TokenType.Semicolon);
    public int NumParts => Parts.Count();

    public VariableDecl Initializer => ChildNode<VariableDecl>();
    public ExprSyntax   Condition   => Parts.ElementAtOrDefault(NumParts >= 4 ? 1 : 0) as ExprSyntax;
    public ExprSyntax   Increment   => Parts.ElementAtOrDefault(NumParts >= 4 ? 2 : 1) as ExprSyntax;

    public Block Body => ChildNode<Block>();

    public override string DebugContent() => DataContentBuilder.Create()
       .Add(Initializer?.DebugContent())
       .Add(Condition?.DebugContent())
       .Add(Increment?.DebugContent())
       .ClearTrailingSpace();

    public Maybe<ValueReference> Execute(ExecContext ctx) {
        using var _ = ctx.UsingScope();

        try {
            var             initializer =Initializer?.DoExecuteSingle(ctx).Value();
            ValueReference? incrementResult = null;
            while (true) {
                var cond = Condition.DoExecuteSingle(ctx).Value();
                if (!cond.Value.IsTruthy())
                    break;

                try {
                    Body.ExecuteVoid(ctx, false);
                }
                catch (ExecContext.ContinueException) { }

                incrementResult = Increment?.DoExecuteSingle(ctx).Value();
            }

        }
        catch (ExecContext.BreakException e) {
            if (e.Count > 1) {
                e.Count--;
                throw;
            }
        }

        return ValueReference.Nothing;
    }
}

/// <summary>
/// Represents a range loop, ie loop over array/obj
/// ```for(var (a, b) = range item) { ... }```
/// ```for(var idx = range list) { ... }```
/// </summary>
[SyntaxNode]
public partial class ForRangeLoop(int index, SyntaxTree tree) : SyntaxNode(index, tree), IExecSingle
{
    public VariableDecl Initializer => ChildNode<VariableDecl>();
    public Block        Body        => ChildNode<Block>();

    public override string DebugContent() => DataContentBuilder.Create()
       .Add(Initializer?.DebugContent())
       .ClearTrailingSpace();

    public Maybe<ValueReference> Execute(ExecContext ctx) {
        using var _ = ctx.UsingScope();

        var initializerObj = Initializer.DoExecuteSingle(ctx).Value();

        // var rangeMin = initializerObj.Value["min"];
        // var rangeMax = initializerObj.Value["max"];
        // var rangeMin   = initializers[0].Value().Value;
        // var rangeValue = initializers.Length >= 2 ? initializers[1].Value().Value : null;
        // if (rangeMin == null || rangeMax == null) {
            // ctx.LogError(this, "Failed to get range values");
            // return Maybe.Nothing<ValueReference>();
        // }

        if(initializerObj.Value?.IsEnumerable != true) {
            ctx.LogError(this, "Invalid range object");
            return Maybe.Nothing<ValueReference>();
        }
        
        var indexers = Initializer.Vars.ToList();

        IdentifierExpr variableInitializer = indexers[0];
        IdentifierExpr loopElementDecl     = null;
        if (indexers.Count > 1) {
            // if (indexers[1] is not IdentifierExpr elementDecl) {
            // ctx.LogError(this, "Invalid loop element declaration");
            // return Maybe.Nothing<ValueReference>();
            // }
            loopElementDecl = indexers[1];
        }

        var iterator = initializerObj.Value.GetIterator(ctx);

        var            loopIndexVar   = ctx.Variables.Declare(variableInitializer.Name);
        VariableSymbol loopElementVar = null;

        if (loopElementDecl != null) {
            loopElementVar = ctx.Variables.Declare(loopElementDecl.Name);
        }
        try {
            while (iterator.MoveNext()) {
                try {
                    loopIndexVar.Val = iterator.CurrentIndex;

                    if (loopElementVar != null) {
                        loopElementVar.Val = iterator.Current; // The value from the iterator
                    }

                    Body.ExecuteVoid(ctx, false);
                }
                catch (ExecContext.ContinueException) { }
            }
        }
        catch (ExecContext.BreakException e) {
            if (e.Count > 1) {
                e.Count--;
                throw;
            }
        }
        return ValueReference.Nothing;
    }

}