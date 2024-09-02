namespace CSScriptingLang.Utils;

public struct UsingCallbackHandle : IDisposable/*, IEquatable<UsingCallbackHandle>*/
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

    public void Dispose() {
        if (_onDispose == null)
            return;
        _onDispose();
        _onDispose = (Action) null;
    }
    /*public bool Equals(UsingCallbackHandle other) {
        return Equals(_onDispose, other._onDispose);
    }
    public override bool Equals(object obj) {
        return obj is UsingCallbackHandle other && Equals(other);
    }
    public override int GetHashCode() {
        return (_onDispose != null ? _onDispose.GetHashCode() : 0);
    }*/
}

/*public struct UsingValueCallbackHandle : IDisposable, IEquatable<UsingValueCallbackHandle>
{
    private Action _onDispose;
    public  object Value;

    public UsingValueCallbackHandle(object value, Action onDisposed) {
        Value      = value;
        _onDispose = onDisposed;
    }

    public void Dispose() {
        if (_onDispose == null)
            return;
        _onDispose();
        _onDispose = (Action) null;
        Value      = null;
    }

    public bool Equals(UsingValueCallbackHandle other) {
        return Equals(_onDispose, other._onDispose) && Equals(Value, other.Value);
    }
    public override bool Equals(object obj) {
        return obj is UsingValueCallbackHandle other && Equals(other);
    }
    public override int GetHashCode() {
        return HashCode.Combine(_onDispose, Value);
    }
}*/