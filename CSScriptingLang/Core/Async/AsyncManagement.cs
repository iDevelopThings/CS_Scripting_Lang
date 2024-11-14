using CSScriptingLang.Interpreter.Bindings;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.RuntimeValues.Values;
using JetBrains.Annotations;
using System.ComponentModel;

namespace CSScriptingLang.Core.Async;

/*
using SystemSyncContext = SynchronizationContext;
using SystemTaskScheduler = System.Threading.Tasks.TaskScheduler;

public class TaskScheduler : SystemTaskScheduler
{
    private SyncContext                                        _syncContext { get; set; }
    private ConcurrentQueue<Task>                              _tasks       { get; set; } = new();
    private ConcurrentQueue<Tuple<SendOrPostCallback, object>> _callbacks   { get; set; } = new();

    public TaskScheduler() {
        _syncContext = new SyncContext(this);

        // Start a new thread to handle the tasks
        var thread = new Thread(ProcessTaskQueue);
        thread.IsBackground = true;
        thread.Start();
    }
    private void ProcessTaskQueue() {
        while (true) {
            Run();
            Thread.Sleep(1);
        }
    }

    public void Run() {
        var originalSyncContext = SystemSyncContext.Current;
        SystemSyncContext.SetSynchronizationContext(_syncContext);

        try {
            var count = _tasks.Count + _callbacks.Count;

            if (count == 0)
                return;

            while (count-- > 0) {
                if (_callbacks.TryDequeue(out var callback))
                    callback.Item1(callback.Item2);

                if (!_tasks.TryDequeue(out var task))
                    break;

                TryExecuteTask(task);
            }
        }
        finally {
            SystemSyncContext.SetSynchronizationContext(originalSyncContext);
        }
    }

    public void PostCallback(SendOrPostCallback d, object state) {
        _callbacks.Enqueue(Tuple.Create(d, state));
    }

    protected override IEnumerable<Task> GetScheduledTasks() {
        return _tasks;
    }

    protected override void QueueTask(Task task) {
        _tasks.Enqueue(task);
    }

    protected override bool TryDequeue(Task task) {
        return false;
    }

    protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) {
        if (taskWasPreviouslyQueued && !TryDequeue(task))
            return false;

        return TryExecuteTask(task);
    }
}

public class SyncContext : SystemSyncContext
{
    private readonly TaskScheduler _scheduler;

    public SyncContext(TaskScheduler scheduler) {
        _scheduler = scheduler;
    }

    public override SystemSyncContext CreateCopy() {
        return new SyncContext(_scheduler);
    }

    public override void Post(SendOrPostCallback d, object state) {
        _scheduler.PostCallback(d, state);
    }

    public override void Send(SendOrPostCallback d, object state) {
        throw new NotSupportedException("SyncContext.Send");
    }
}
*/

[LanguageModuleBind("Async")]
public partial class AsyncContext
{
    // public  TaskScheduler    Scheduler { get; set; } = new();
    // public  TaskFactory      Factory;
    // private int              _activeTasks = 0;
    // private Queue<Exception> _exceptions { get; set; } = new();

    private AsyncContext() {
        // Factory = new TaskFactory(Scheduler);
    }

    /*
    [LanguageFunction]
    public Value Start(ExecContext ctx, Value value) {
        if (value.Is.Function)
            value = ctx.Call(value);

        var getEnumerator = value["getEnumerator"];

        if (getEnumerator.Type != ValueType.Function)
            throw new RuntimeException("Task objects must define getEnumerator");

        var enumerator = state.Call(getEnumerator);

        var task = Factory.StartNew(async () => {
            try {
                await AsyncUtil.RunTask(state, enumerator);
            }
            catch (Exception e) {
                lock (_exceptions)
                    _exceptions.Enqueue(e);
            }
            finally {
                Interlocked.Decrement(ref _activeTasks);
            }
        });

        Interlocked.Increment(ref _activeTasks);

        // return a task that completes when the started task completes
        Func<Task> waitTask = async () => {
            await await task;
        };

        return AsyncUtil.ToObject(waitTask());
    }*/

    /*
    [LanguageFunction]
    public Value Execute(Value result) {
        var task = Factory.StartNew(async () => {
            try {
                if (result.DataObject is AsyncPromise p) {
                    await p.T;
                }

                throw new InterpreterRuntimeException("Async.execute: can only be called on an instance of AsyncPromise");
            }
            catch (Exception ex) {
                lock (_exceptions)
                    _exceptions.Enqueue(ex);
            }
            finally {
                Interlocked.Decrement(ref _activeTasks);
            }

            return result;
        });

        Interlocked.Increment(ref _activeTasks);

        async Task<Value> WaitTask() => await await task;

        return ToObject(WaitTask());
    }

    public Value Execute(Value obj, AsyncPromise promise, Func<Value> func) {
        var factoryTask = Factory.StartNew(async () => {
            try {
                var result = await promise.ExecuteAsync(func);
            }
            catch (Exception ex) {
                lock (_exceptions)
                    _exceptions.Enqueue(ex);
            }
            finally {
                Interlocked.Decrement(ref _activeTasks);
            }

            return promise.Result;
        });

        Interlocked.Increment(ref _activeTasks);

        async Task<Value> WaitTask() => await await factoryTask;

        promise.T = WaitTask();

        return obj;
        // async Task<Value> WaitTask() => await promise.T;
        // return ToObject(WaitTask());
    }

    public static Value ToObject(Task task, [CallerMemberName] string caller = "") {
        return ToObject(task.ContinueWith(t => {
            var val = AsyncPromise.Resolved(Value.Unit());
            val["caller"] = caller;
            return val;
        }), caller);
    }

    public static Value ToObject(Task<Value> task, [CallerMemberName] string caller = "") {
        return AsyncPromise.ResolvedValue(task, caller);
    }
    */

    [EditorBrowsable(EditorBrowsableState.Never), UsedImplicitly]
    public static Value RethrowAsyncException(AggregateException e) {
        var exception = e.InnerExceptions.Count != 1 ? e : e.InnerException ?? e;
        System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception).Throw();
        throw exception;
    }
    /*[LanguageFunction]
    public Value Execute(FunctionExecContext ctx, Value fnValue, Value instance, params Value[] args) {

        var task = Factory.StartNew(async () => {
            try {
                var result = ctx.Call(fnValue, instance, args);
                if (result.DataObject is AsyncPromise p) {
                    await p.TaskInstance;
                }

                throw new InterpreterRuntimeException("Async.execute: can only be called on an instance of AsyncPromise");
            }
            catch (Exception ex) {
                lock (_exceptions)
                    _exceptions.Enqueue(ex);
            }
            finally {
                Interlocked.Decrement(ref _activeTasks);
            }

        });

        Interlocked.Increment(ref _activeTasks);

        Func<Task> waitTask = async () => {
            await await task;
        };

        var obj = Value.Object();
        obj.DataObject = waitTask().ContinueWith(t => Value.Unit());
        return obj;
    }*/

    [LanguageFunction]
    public Value Sleep(FunctionExecContext ctx, int ms) {
        return ScriptTask.Delay(ms).Wrap(ctx);
    }

    /*
    [LanguageFunction]
    public bool Run() {
        if (SystemSyncContext.Current is SyncContext)
            throw new InterpreterRuntimeException("Async.run: cannot be called in an async function");

        Exception ex = null;

        lock (_exceptions) {
            if (_exceptions.Count > 0)
                ex = _exceptions.Dequeue();
        }

        if (ex != null) {
            var sb = new StringBuilder();
            sb.AppendLine("Unhandled error in task:");
            sb.Append(ex.Message);

            throw new InterpreterRuntimeException(sb.ToString(), ex);
        }

        Scheduler.Run();

        lock (_exceptions)
            return _activeTasks > 0 || _exceptions.Count > 0;
    }

    [LanguageFunction]
    [LanguageFunctionDisableParameterChecks]
    public void RunToCompletion() {
        var waitTask = Task.Run(async () => {
            while (Run()) {
                await Task.Delay(1);
            }
        });

        waitTask.Wait();
    }
    */


}