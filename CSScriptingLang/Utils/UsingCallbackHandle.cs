namespace CSScriptingLang.Utils;

public struct UsingCallbackHandle : IDisposable
{
    private Action _onDispose;
    public  object Value { get; set; }

    public UsingCallbackHandle(Action onDisposed, object value) {
        Value      = value;
        _onDispose = onDisposed;
    }

    public UsingCallbackHandle(Action onDisposed) {
        _onDispose = onDisposed;
    }

    public static implicit operator UsingCallbackHandle(Action onDispose) {
        return new UsingCallbackHandle(onDispose);
    }
    
    public void Dispose() {
        if (_onDispose == null)
            return;
        _onDispose();
        _onDispose = (Action) null;
    }
}