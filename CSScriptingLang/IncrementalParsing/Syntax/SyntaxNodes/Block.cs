using CSScriptingLang.Core.Async;
using CSScriptingLang.Core.Logging;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.RuntimeValues.Values;
using CSScriptingLang.Utils;
using MoreLinq.Extensions;
using SharpX;

namespace CSScriptingLang.IncrementalParsing.Syntax.SyntaxNodes;

[SyntaxNode]
public partial class Block(int index, SyntaxTree tree) : SyntaxNode(index, tree), IExecMulti
{
    public IEnumerable<SyntaxNode> Statements => ChildNodes<SyntaxNode>();

    public IEnumerable<Maybe<ValueReference>> Execute(
        ExecContext ctx,
        bool        pushScope            = true,
        Action      onBeforeExecuteBlock = null,
        Action      onAfterExecuteBlock  = null
    ) {
        using var _  = ctx.UsingScope(pushScope);
        using var __ = ctx.UsingBlockCallbacks(onBeforeExecuteBlock, onAfterExecuteBlock);

        foreach (var n in Statements) {
            foreach (var val in n.DoExecute(ctx)) {
                yield return val;
            }
        }
    }

    public void ExecuteVoid(
        ExecContext ctx,
        bool        pushScope            = true,
        Action      onBeforeExecuteBlock = null,
        Action      onAfterExecuteBlock  = null
    ) {
        using var _  = ctx.UsingScope(pushScope);
        using var __ = ctx.UsingBlockCallbacks(onBeforeExecuteBlock, onAfterExecuteBlock);

        foreach (var n in Statements) {
            n.DoExecute(ctx).Consume();
        }
    }

    public IEnumerable<Maybe<ValueReference>> ExecuteMulti(ExecContext ctx) {
        return Execute(ctx);
    }
}

[SyntaxNode]
public partial class ReturnStatement(int index, SyntaxTree tree) : SyntaxNode(index, tree), IExecSingle
{
    public ExprSyntax ReturnValue => ChildNode<ExprSyntax>();

    public override string DebugContent() => DataContentBuilder.Create()
       .Add("return")
       .Add(ReturnValue?.DebugContent())
       .ClearTrailingSpace();

    public Maybe<ValueReference> Execute(ExecContext ctx) {
        if (ReturnValue == null) {
            ctx.Return();
            return ValueReference.Nothing;
        }

        var rtValue = ReturnValue.DoExecuteSingle(ctx).Value();
        ctx.Return(rtValue);

        return ValueReference.Nothing;
    }
}

[SyntaxNode]
public partial class ContinueStatement(int index, SyntaxTree tree) : SyntaxNode(index, tree), IExecSingle
{
    public override string DebugContent() => DataContentBuilder.Create()
       .ClearTrailingSpace();

    public Maybe<ValueReference> Execute(ExecContext ctx) {
        ctx.Continue();

        return ctx.ValReference(Value.Unit());
    }
}

[SyntaxNode]
public partial class BreakStatement(int index, SyntaxTree tree) : SyntaxNode(index, tree), IExecSingle
{
    public ExprSyntax Count => ChildNode<ExprSyntax>();

    public override string DebugContent() => DataContentBuilder.Create()
       .ClearTrailingSpace();

    public Maybe<ValueReference> Execute(ExecContext ctx) {
        var count = Count?.DoExecuteSingle(ctx).Value() ?? ctx.ValReference(Value.Number(1));
        ctx.Break(count);

        return ctx.ValReference(Value.Unit()).ToMaybe();
    }
}

[SyntaxNode]
public partial class DeferStatement(int index, SyntaxTree tree) : SyntaxNode(index, tree), IExecSingle
{
    public ExprSyntax Expr => ChildNode<ExprSyntax>();

    public override string DebugContent() => DataContentBuilder.Create()
       .Add(Expr?.DebugContent())
       .ClearTrailingSpace();

    public Maybe<ValueReference> Execute(ExecContext ctx) {
        // if (ctx is FunctionExecContext) {
        // ctx.PushDefer(Expr);
        // } else {
        // DiagnosticManager.Diagnostic_Error_Fatal().Message("Defer statement outside of function context").Range(this).Report();
        // }

        return Maybe.Nothing<ValueReference>();
    }
}

[SyntaxNode]
public partial class AwaitStatement(int index, SyntaxTree tree) : ExprSyntax(index, tree), IExecSingle
{
    public ExprSyntax Expr => ChildNode<ExprSyntax>();

    public override string DebugContent() => DataContentBuilder.Create()
       .Add(Expr?.DebugContent())
       .ClearTrailingSpace();

    public Maybe<ValueReference> Execute(ExecContext ctx) {
        var rtValue = Expr.DoExecuteSingle(ctx).Value();

        if (rtValue.Value?.DataObject is ScriptTask task) {

            try {
                task.RunAsync().Wait();
            }
            catch (OperationCanceledException) {
                Logs.Get<AwaitStatement>().Debug("Task was cancelled");
            }
            catch (ExecContext.ReturnException e) {
                return ctx.ValReference(e.ReturnValue);
            }
            catch (AggregateException e) {
                var returnException = e.InnerExceptions.OfType<ExecContext.ReturnException>().FirstOrDefault();
                if (returnException != null) {
                    return ctx.ValReference(returnException.ReturnValue);
                }
            }

            try {
                var val = task.CompletionSource.Task.Result;
                return ctx.ValReference(val);
            }
            catch (ExecContext.ReturnException e) {
                var returnValue = e.ReturnValue;
                if (ctx is FunctionExecContext fnCtx)
                    fnCtx.ReturnValues.Add(returnValue);

                return ctx.ValReference(returnValue);
            }
            catch (AggregateException e) {
                var returnException = e.InnerExceptions.OfType<ExecContext.ReturnException>().FirstOrDefault();
                if (returnException != null) {
                    var returnValue = returnException.ReturnValue;
                    if (ctx is FunctionExecContext fnCtx)
                        fnCtx.ReturnValues.Add(returnValue);

                    return ctx.ValReference(returnValue);
                }
            }

        }

        return rtValue;
    }
}

[SyntaxNode]
public partial class YieldStatement(int index, SyntaxTree tree) : SyntaxNode(index, tree), IExecSingle
{
    public ExprSyntax Expr => ChildNode<ExprSyntax>();

    public override string DebugContent() => DataContentBuilder.Create()
       .Add(Expr?.DebugContent())
       .ClearTrailingSpace();

    public Maybe<ValueReference> Execute(ExecContext ctx) {
        var v = Expr.DoExecuteSingle(ctx).Value();

        return v;
    }
}