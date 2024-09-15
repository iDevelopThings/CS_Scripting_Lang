using System.Diagnostics;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.RuntimeValues.Values;

namespace CSScriptingLang.Interpreter;

[DebuggerDisplay($"{{{nameof(ToDebugString)}(),nq}}")]
public class VariableSymbol
{
    public string Name { get; set; }

    public Value Val { get; set; }

    public VariableSymbol Reference { get; set; }

    public object RawValue => Val?.GetUntypedValue();

    // When true, the variable is defined, but just for declaration purposes
    // So it's safe to set the value/override it
    public bool IsBaseDeclaration { get; set; }

    public T NativeValue<T>() where T : notnull => throw new System.NotImplementedException();

    public VariableSymbol(string name) {
        Name = name;
        Val  = Value.Null();
    }
    public VariableSymbol(string name, Value value) {
        Name = name;
        Val  = value;
    }

    public string ToDebugString() {
        if (Val != null) {
            return $"{Name} : {Val?.Type} = {Val?.ToString()}";
        }

        return $"{Name} : null";
    }
    public void IsReference(VariableSymbol argSymbol) {
        Reference = argSymbol;
    }
}

[DebuggerDisplay($"{{{nameof(ToString)}(),nq}}")]
public class Symbol
{
    private static   long Counter; // Ensures uniqueness
    private readonly long Id;      // Unique ID for each symbol

    public string Name { get; }

    private static Dictionary<string, Symbol> Registry { get; set; } = new();

    private Symbol(string name) {
        Id   = ++Counter;
        Name = name;
    }

    // Factory method for creating new symbols
    public static Symbol Create(string name = null) {
        return new Symbol(name);
    }

    public static Symbol For(string name) {
        if (Registry.TryGetValue(name, out Symbol value))
            return value;

        value          = Create(name);
        Registry[name] = value;

        return value;
    }

    public override bool Equals(object obj) {
        if (obj is Symbol otherSymbol) {
            return Id == otherSymbol.Id;
        }

        return false;
    }

    public override int GetHashCode() {
        return Id.GetHashCode();
    }

    public override string ToString() {
        return Name != null ? $"Symbol({Name})" : $"Symbol()";
    }
}