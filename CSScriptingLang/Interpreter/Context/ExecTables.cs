using System.Runtime.CompilerServices;
using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.RuntimeValues.Values;

namespace CSScriptingLang.Interpreter.Context;

public class ExecTable<TKey, TValue, TUpdaterFuncValue>
{
    public Dictionary<TKey, TValue> Table { get; set; } = new();

    public Action<TValue, TUpdaterFuncValue> UpdaterFunc  { get; set; }
    public Func<string, TValue>              Constructor  { get; set; }
    public Func<TValue, TUpdaterFuncValue>   GetValueFunc { get; set; }

    public ExecTable(
        ExecStack<TKey, TValue, TUpdaterFuncValue> stack
    ) {
        UpdaterFunc  = stack.UpdaterFunc;
        Constructor  = stack.Constructor;
        GetValueFunc = stack.GetValueFunc;
    }

    public TValue this[TKey key] {
        get => Get(key);
        set => Set(key, value);
    }

    public TValue Get(TKey key)                   => Get(key, out var value) ? value : default;
    public bool   Get(TKey key, out TValue value) => Table.TryGetValue(key, out value);

    public TValue Declare(TKey key) {
        return Get(key, out var value) ? value : Set(key, Constructor(key.ToString()));

    }
    public TValue Declare(TKey key, TValue value) => Set(key, value);
    public TValue Declare(TKey key, Func<TValue> factory) {
        // if the key already exists, don't overwrite it
        // if it doesn't exist, set it to the factory value
        return !Get(key, out var value) ? Set(key, factory()) : value;

    }

    public TValue SetValue(TKey key, TUpdaterFuncValue value) => Set(key, value);

    public TValue Set(TKey key, TValue value) {
        if (!Table.TryAdd(key, value)) {
            return Table[key] = value;
        }

        return value;
    }

    public TValue Set(TKey key, TUpdaterFuncValue value) {
        if (Get(key, out var current)) {
            UpdaterFunc(current, value);
            return current;
        }

        var newValue = Constructor(key.ToString());
        UpdaterFunc(newValue, value);
        return Set(key, newValue);
    }

    public bool ContainsKey(TKey key) => Table.ContainsKey(key);
    public bool Contains(TKey key, bool checkValue = false) {
        if (checkValue) {
            return Table.ContainsKey(key) && Table[key] != null;
        }

        return Table.ContainsKey(key);
    }
}

public class ExecStack<TKey, TValue, TUpdaterFuncValue> : Stack<ExecTable<TKey, TValue, TUpdaterFuncValue>>
{
    public Action<TValue, TUpdaterFuncValue> UpdaterFunc { get; set; }

    public Func<string, TValue>            Constructor  { get; set; }
    public Func<TValue, TUpdaterFuncValue> GetValueFunc { get; set; }

    public Stack<string> ScopeNames { get; set; } = new();

    public ExecStack(
        Action<TValue, TUpdaterFuncValue> updaterFunc  = null,
        Func<string, TValue>              constructor  = null,
        Func<TValue, TUpdaterFuncValue>   getValueFunc = null
    ) {
        UpdaterFunc  = updaterFunc ?? ((value, updater) => { });
        Constructor  = constructor ?? (name => default);
        GetValueFunc = getValueFunc ?? (value => default);
    }

    public void PushScope(string name = "", int line = 0, string file = "") {
        ScopeNames.Push($"{name}({line}:{file})");
        Push(new(this));
    }

    public void PopScope() {
        ScopeNames.Pop();

        var scope = Pop();
        var table = scope.Table;
        foreach (var (key, value) in table) {
            if (value is VariableSymbol symbol) {
                var val = symbol.Val;
                if(val["_disposed"] == true) {
                    continue;
                }
                
                var disposeFn = val["dispose"];
                if (disposeFn.Type == RTVT.Function) {
                    disposeFn.As.Fn().Call(val._context, null);
                }
                
                val["_disposed"] = true;
                
                table.Remove(key);
            }
        }
    }

    public TValue Get(TKey key) => Get(key, out var value) ? value : default;
    public bool Get(TKey key, out TValue value) {
        var scopes = this.ToArray();
        foreach (var scope in scopes) {
            if (scope.Get(key, out value)) {
                return true;
            }
        }

        value = default;
        return false;
    }
    public TValue Declare(TKey key) {
        if (Get(key, out var value)) {
            return value;
        }

        return Set(key, Constructor(key.ToString()));
    }
    public TValue Declare(TKey key, TUpdaterFuncValue value) => Set(key, value);
    public TValue Declare(TKey key, TValue            value) => Set(key, value);
    public TValue Declare(TKey key, Func<TValue> factory) {
        // if the key already exists, don't overwrite it
        // if it doesn't exist, set it to the factory value
        if (!Get(key, out var value)) {
            return Set(key, factory());
        }

        return value;
    }
    public TValue SetValue(TKey key, TUpdaterFuncValue value) => Set(key, value);
    public TValue Set(TKey key, TUpdaterFuncValue value) {
        if (Get(key, out var current)) {
            UpdaterFunc(current, value);
            return current;
        }

        var newValue = Constructor(key.ToString());
        UpdaterFunc(newValue, value);
        return Set(key, newValue);
    }
    public TValue Set(TKey key, TValue value) {
        if (Count == 0) {
            PushScope();
        }

        return this.Peek()[key] = value;
    }


    public bool Contains(TKey key) {
        foreach (var scope in this) {
            if (scope.Contains(key)) {
                return true;
            }
        }

        return false;
    }

    public TValue this[TKey key] {
        get => Get(key);
        set => Set(key, value);
    }

    public new void Clear() {
        base.Clear();
        PushScope();
    }
}

// Type = ExecTable<string, Symbol, RuntimeValue>
public class VariablesStack : ExecStack<string, VariableSymbol, Value>
{
    public VariablesStack() : base(
        (symbol, value) => symbol.Val = value,
        name => new VariableSymbol(name),
        symbol => symbol.Val
    ) { }

    public T     Get<T>(string   key) where T : class => Get(key).Val as T;
    public Value GetValue(string key)                 => Get(key).Val;

    public object RawValue(string    key) => Get(key).Val.GetUntypedValue();
    public T      RawValue<T>(string key) => (T) RawValue(key);

    public T GetValue<T>(string key) {
        if (Get(key, out var value)) {
            return value.NativeValue<T>();
        }

        return default;
    }
}

// Type = ExecTable<string, FunctionDeclaration, FunctionDeclaration>
public class FunctionsStack : ExecStack<string, FunctionDeclaration, FunctionDeclaration>
{
    public FunctionsStack() : base(
        (value, updater) => { },
        name => {
            throw new NotImplementedException();
        },
        value => value
    ) { }
}