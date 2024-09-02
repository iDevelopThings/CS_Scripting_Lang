using CSScriptingLang.Interpreter;

namespace CSScriptingLang.VM;

public class ScopeAware<T> : IDisposable where T : ScopeAware<T>
{
    public InterpreterExecutionContext InterpreterContext { get; set; }
    public ExecutionContext            Context            { get; set; }
    public T                           Parent             { get; set; }

    public ScopeAware() { }
    public ScopeAware(InterpreterExecutionContext context, T parent) {
        InterpreterContext = context;
        Parent             = parent;
    }

    public ScopeAware(ExecutionContext context, T parent) {
        Context = context;
        Parent  = parent;
    }

    public virtual void Dispose() {
        Context = null;
    }
}