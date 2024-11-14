using CSScriptingLang.Core.Async;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.Core.Logging;
using SharpX;

namespace CSScriptingLang.Interpreter.Execution.Statements;

[ASTNode]
public partial class AwaitStatement : Statement
{
    [VisitableNodeProperty]
    public Expression Value { get; set; }

    public AwaitStatement() { }
    public AwaitStatement(Expression value) {
        Value = value;
    }
    public override Maybe<ValueReference> Execute(ExecContext ctx) {
        var fnCtx = ctx as FunctionExecContext;

        var rtValue = Value.Execute(ctx);

        if (rtValue.Value?.DataObject is ScriptTask task) {
            try
            {
                task.RunAsync().Wait();
            }
            catch (OperationCanceledException)
            {
                Logs.Get<AwaitStatement>().Debug("Task was cancelled");
            }
            // task.AwaitCompletion().Wait();
            
            var value = task.CompletionSource.Task.Result;
            
            return Maybe.Just(new ValueReference(ctx, value));
        }
        
        return Maybe.Nothing<ValueReference>();
    }
    public override Task<Maybe<ValueReference>> ExecuteAsync(ExecContext ctx) {
        var fnCtx = ctx as FunctionExecContext;

        var rtValue = Value.Execute(ctx);

        if (rtValue.Value?.DataObject is ScriptTask task) {
            return task.RunAsync().ContinueWith(_ => {
                var value = task.CompletionSource.Task.Result;
                return Maybe.Just(new ValueReference(ctx, value));
            });
        }
        
        return Task.FromResult(Maybe.Nothing<ValueReference>());
    }

}