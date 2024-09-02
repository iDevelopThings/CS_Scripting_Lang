using System.Collections.Concurrent;

namespace CSScriptingLang.Utils;

public interface IPooledObject
{
    public virtual void OnRent()   { }
    public virtual void OnReturn() { }
}

public class PooledObject<T> : IPooledObject, IDisposable where T : new()
{
    public static T Rent() => ObjectPool<T>.Rent();

    public virtual void Return() => ObjectPool<T>.Return(this);

    public virtual void OnRent()   { }
    public virtual void OnReturn() { }

    public virtual void Dispose() {
        // disposed = true;
        // GC.SuppressFinalize(this);
    }

    ~PooledObject() { }
}

public struct ObjectPoolHandle<T> : IDisposable where T : new()
{
    public T Value { get; private set; }

    public ObjectPoolHandle(T value) {
        Value = value;
    }

    public static implicit operator T(ObjectPoolHandle<T> handle) => handle.Value;

    public void Dispose() {
        ObjectPool<T>.Return(Value);
    }
}

public class ObjectPool<T> where T : new()
{
    private static ConcurrentBag<T> Pool = new();

    public static int MaxItemsThreshold { get; set; } = 512;

    public static void WarmUp(int count) {
        for (var i = 0; i < count; i++)
            Pool.Add(new T());
    }

    public static T Rent() {
        if (Pool.IsEmpty) {
            var obj = new T();
            if (obj is IPooledObject p)
                p.OnRent();
            return obj;
        }

        if (!Pool.TryTake(out var result))
            result = new T();

        if (result is IPooledObject pooled)
            pooled.OnRent();

        return result;
    }

    public static ObjectPoolHandle<T> RentHandle() {
        return new ObjectPoolHandle<T>(Rent());
    }

    public static void Return(object obj) {
        if (obj is IPooledObject pooled)
            pooled.OnReturn();

        if (Pool.Count > MaxItemsThreshold) {
            if (obj is not IDisposable disposable)
                return;
            disposable.Dispose();
        } else
            Pool.Add((T) obj);
    }

    public static void Clear() {
        while (Pool.TryTake(out var result)) {
            if (result is IDisposable disposable)
                disposable.Dispose();
        }
    }
}