using System.Collections.Concurrent;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.RuntimeValues.Values;
using JetBrains.Annotations;

namespace CSScriptingLang.Core.Async;

public class ScriptTask
{
    public Func<CancellationToken, Task<Value>> ExecuteAsync { get; }

    public TaskCompletionSource<Value> CompletionSource   { get; }
    public CancellationTokenSource     CancellationSource { get; } = new();
    public Task<Value>                 CurrentTask        { get; set; }

    public static HashSet<ScriptTask>     RunningTasks { get; } = new();
    public static CancellationTokenSource GlobalCancellationSource = new();

    public ScriptTask() {
        CompletionSource = new TaskCompletionSource<Value>();
        CancellationSource = new CancellationTokenSource();

        RunningTasks.Add(this);
    }
    public ScriptTask(Task task) : this() {
        ExecuteAsync = async (ct) => {
            try {
                await task;
                return Value.Null();
            }
            catch (OperationCanceledException) {
                throw;
            }
        };
    }
    public ScriptTask(Task<Value> task) : this() {
        ExecuteAsync = async (ct) => await task.ConfigureAwait(false);
    }
    public ScriptTask(Func<Task<Value>> executeAsync) : this() {
        ExecuteAsync = async (ct) => await executeAsync();
    }
    public ScriptTask(Func<CancellationToken, Task<Value>> executeAsync) : this() {
        ExecuteAsync = executeAsync;
    }
    public ScriptTask(Func<Task> executeAsync) : this() {
        ExecuteAsync = async (ct) => {
            await executeAsync();
            return Value.Null();
        };
    }
    public ScriptTask(Func<CancellationToken, Task> executeAsync) : this() {
        ExecuteAsync = async (ct) => {
            await executeAsync(ct);
            return Value.Null();
        };
    }

    public bool IsCompleted => CompletionSource.Task.IsCompleted;
    public bool IsStarted   => CurrentTask != null;
    public bool IsCancelled => CancellationSource.Token.IsCancellationRequested;

    public Value Value => CompletionSource.Task.Result;

    // Start executing the task and complete the CompletionSource when done
    public async Task RunAsync() {
        if (IsStarted) return;

        try {
            if (CompletionSource.Task.IsCompleted)
                return;

            if (CurrentTask?.IsCompleted == true || CurrentTask?.IsFaulted == true)
                return;
            
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(CancellationSource.Token, GlobalCancellationSource.Token);

            CurrentTask = ExecuteAsync(linkedCts.Token);
            var value = await CurrentTask;

            if (CompletionSource.Task.IsCompleted) {
                return;
            }

            CompletionSource.SetResult(value ?? Value.Null());

            RunningTasks.Remove(this);
        }
        catch (OperationCanceledException) {
            CompletionSource.SetCanceled();
            RunningTasks.Remove(this);
        }
        catch (Exception e) {
            CompletionSource.SetException(e);

            RunningTasks.Remove(this);
        }
    }

    // Await the completion of this task
    public Task<Value> AwaitCompletion() {
        return CompletionSource.Task;
    }
    
    public void Cancel() {
        CancellationSource.Cancel();
    }
    public static void CancelAll() {
        GlobalCancellationSource.Cancel();

        GlobalCancellationSource = new();
    }

    public static ScriptTask Delay(int ms) {
        return new ScriptTask(async (ct) => await Task.Delay(ms,ct));
    }

    public Value Wrap(ExecContext ctx) {
        var result = Value.Object(ctx);
        result.DataObject = this;

        result["run"] = Value.Function("run", (c, instance, args) => {
            _ = RunAsync();
            return Value.Null();
        });

        result["await"] = Value.Function("await", (c, instance, args) => {
            AwaitCompletion().Wait();
            return Value.Null();
        });

        return result;
    }

    public static Value Wrap(ExecContext ctx, Task task)
        => new ScriptTask(task).Wrap(ctx);
    public static Value Wrap(ExecContext ctx, Task<Value> task)
        => new ScriptTask(task).Wrap(ctx);
    public static Value Wrap(ExecContext ctx, Func<Task<Value>> executeAsync)
        => new ScriptTask(executeAsync).Wrap(ctx);
    public static Value Wrap(ExecContext ctx, Func<Task> executeAsync)
        => new ScriptTask(executeAsync).Wrap(ctx);
}