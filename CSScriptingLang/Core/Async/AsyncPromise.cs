using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Libraries;
using CSScriptingLang.Lexing;
using CSScriptingLang.RuntimeValues.Values;

namespace CSScriptingLang.Core.Async;

public class AsyncPromise
{

    public ExecContext Context { get; set; }
    public Frame       Frame   { get; set; }

    public Value Function { get; set; }

    public Task<Value> T { get; set; }

    public Value     Result { get; set; }
    public Exception Error  { get; set; }

    public AsyncPromise(ExecContext context, Frame frame, Value function) {
        Context  = context;
        Frame    = frame;
        Function = function;
    }

    public static AsyncPromise For(Func<Value> func) {
        var promise = new AsyncPromise(null, null, null);
        promise.Execute(func);
        return promise;
    }

    public static Value ResolvedValue(Func<Task<Value>> task, [CallerMemberName] string caller = "") {
        var obj = Value.Object();

        var promise = new AsyncPromise(null, null, null);
        promise.T = task();
        obj.DataObject = promise;

        obj["caller"] = caller;
        return obj;
    }
    
    public static Value ResolvedValue(Task<Value> task, [CallerMemberName] string caller = "") {
        if (task.IsCompleted && task.Result.DataObject is AsyncPromise p) {
            return task.Result;
        }

        var obj = Value.Object();

        var promise = new AsyncPromise(null, null, null);
        promise.T = task;

        obj.DataObject = promise;

        obj["caller"] = caller;
        return obj;

    }
    public static Value ResolvedValue(AsyncPromise promise) {
        var obj = Value.Object();
        obj.DataObject = promise;
        return obj;
    }

    public static Value Resolved(Value val)
        => Resolved(() => val);

    public static Value Resolved(Func<Value> func) {
        var promise = new AsyncPromise(null, null, null);
        promise.FromValue(func);
        return ResolvedValue(promise);
    }
    private Value FromValue(Func<Value> func) {
        try {
            var val = func();
            T = Task.FromResult(val);
            Resolve(val);
            return val;
        }
        catch (Exception ex) {
            Reject(ex);
            return null;
        }
    }

    public void Resolve(Value result) {
        Console.WriteLine($"Promise resolved");
        Lib_Logging.Print(result);
        Result = result;
    }
    public void Reject(Exception ex) {
        Console.WriteLine($"Promise rejected");
        Lib_Logging.Print(ex.Message);
        Error = ex;
    }

    public void Execute(Func<Value> func) {
        try {
            var result = func();
            Resolve(result);
        }
        catch (Exception ex) {
            Reject(ex);
        }
    }
    public Task<Value> ExecuteAsync(Func<Value> func) {
        try {
            var result = func();
            Resolve(result);
            return Task.FromResult(result);
        }
        catch (Exception ex) {
            Reject(ex);
            return Task.FromResult<Value>(null);
        }
    }
    
    public void Execute(Func<Task<Value>> func) {
        try {
            var result = func();
            // If T is already defined then we need to chain the tasks
            if (T != null) {
                T.ContinueWith(t => result);
            } else {
                T = result;
            }
        }
        catch (Exception ex) {
            Reject(ex);
        }
    }

    public static Value UnwrapValue(Task<Value> task, ExecContext ctx) {
        // task.Wait();
        if (task.IsCompleted) {
            if (task.Result.DataObject is AsyncPromise promise) {
                if (promise.Error != null) {
                    throw promise.Error;
                }
                return promise.Result;
            }
            if (task.Result is { } v) {
                return v;
            }
        }

        if (task.IsFaulted) {
            throw task.Exception;
        }

        // if (!task.IsCompleted) {
        //     throw new Exception("Task not completed");
        // }

        return null;
    }

    public static Value UnwrapValue(Value task, ExecContext ctx) {
        if (task.DataObject is not AsyncPromise promise) {
            throw new InterpreterRuntimeException("Cannot unwrap a non-promise value");
        }
        return UnwrapValue(promise.T, ctx);
    }

    public static bool UnwrapPromise(ValueReference value, out AsyncPromise promise) {
        if (value.Object.DataObject is not AsyncPromise p) {
            promise = null;
            return false;
        }

        promise = p;

        return true;
    }

    public static bool UnwrapAndWaitValue(AsyncPromise promise, out Value value) {
        if (promise.T.IsCompleted) {
            value = UnwrapValue(promise.T, promise.Context);
            return true;
        }

        value = promise.AwaitValue();

        return value != null;
    }

    public Task<Value> GetAwaiterFunc() => T;
    
    public Value AwaitValue() {
        if (T.IsCompleted) {
            return UnwrapValue(T, Context);
        }

        /*if (!(SynchronizationContext.Current is SyncContext)) {
            // throw new InterpreterRuntimeException("Cannot use async functions in a synchronous context");

            // if(T.Status != TaskStatus.RanToCompletion) {
                // T.Wait();
            // }
            
            // wait for completion by yielding so that the interpreter can continue
            while (!T.IsCompleted) {
                Thread.Sleep(10);
                Task.Yield();
                // Console.WriteLine("Yielded");
            }
            
            Console.WriteLine("Awaited");
        } else {
            T.Wait();
        }*/
        
        
        T.Wait();


        return UnwrapValue(T, Context);
    }
}