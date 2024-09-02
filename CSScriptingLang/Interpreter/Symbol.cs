using CSScriptingLang.RuntimeValues;
using CSScriptingLang.RuntimeValues.Types;

namespace CSScriptingLang.Interpreter;

public class Symbol
{
    public string Name { get; set; }

    private RuntimeValue _value;
    public RuntimeValue Value {
        get => _value;
        set {
            var currentRefs = _value?.References;
            _value?.RemoveReference(this);
            _value = value;
            if (_value != null && _value.Symbol != this) {
                _value.SetSymbol(this);
                _value.AddReference(currentRefs);
            }
        }
    }

    public object RawValue => Value?.Value;

    public int ReferenceCount => Value?.ReferenceCount ?? 0;

    public RuntimeTypeInfo Type => Value?.RuntimeType;

    public T As<T>() where T : RuntimeValue => Value as T;

    public static implicit operator RuntimeValue_Array(Symbol    s) => s.As<RuntimeValue_Array>();
    public static implicit operator RuntimeValue_Object(Symbol   s) => s.As<RuntimeValue_Object>();
    public static implicit operator RuntimeValue_Function(Symbol s) => s.As<RuntimeValue_Function>();

    public Symbol(string name, RuntimeValue value) {
        Name  = name;
        Value = value;
        if (Value == null) {
            Value = RuntimeValue.RentNull();
        }

        Value?.SetSymbol(this);
    }

    public void AddReference(object    reference) => Value?.AddReference(reference);
    public void RemoveReference(object reference) => Value?.RemoveReference(reference);
    public string GetReferencesString(bool inlineString = false) {
        var values = Value.References.Select(r => $"\t\t{(inlineString ? "" : "- ")} {r.ToString()}");
        if (inlineString) {
            return string.Join(", ", values);
        }

        return string.Join("\n", values);
    }
}