using CSScriptingLang.RuntimeValues.Types;

namespace CSScriptingLang.RuntimeValues.Values;

public abstract partial class BaseValue
{
    public Dictionary<string, BaseValue> Fields = new();

    public virtual BaseValue SetFieldByPath(string path, BaseValue value) {
        var parts = path.Split('.');
        var last  = parts.Length - 1;
        var obj   = this;
        for (var i = 0; i < last; i++) {
            obj = obj.GetField(parts[i]);
        }

        obj.SetField(parts[last], value);
        return this;
    }

    public virtual BaseValue GetFieldByPath(string path) {
        var parts = path.Split('.');
        var value = this;
        foreach (var part in parts) {
            value = value.GetField(part);
        }

        return value;
    }

    public virtual bool HasField(string name) => Fields.ContainsKey(name);

    public virtual BaseValue GetField(string name) {
        var prototype = this;
        while (prototype != null) {
            if (prototype.Fields.TryGetValue(name, out var value)) {
                return value;
            }

            prototype = prototype.Prototype;
        }
        
        return null;
    }
    public virtual BaseValue GetField(BaseValue name) => GetField(name.Value<string>());

    public virtual BaseValue GetValueAtIndex(int index) {
        throw new NotImplementedException();
    }

    public virtual IEnumerable<(string, BaseValue)> GetFields()      => Fields.Select(pair => (pair.Key, pair.Value));
    public virtual IEnumerable<BaseValue>           GetFieldValues() => Fields.Values;

    public virtual void SetValueAtIndex(int index, BaseValue value) {
        throw new NotImplementedException();
    }

    public virtual void SetField(string name, BaseValue value) {
        if (!Fields.ContainsKey(name)) {
            var fieldType = RuntimeType.RegisterField(name, value.Clone());
            SetFieldOwner(value, fieldType);

            Fields[name] = value;

            return;
        }

        if (CanOverrideField(name)) {
            SetFieldOwner(value, null);
            Fields[name] = value;
        } else {
            Logger.Warning($"Cannot override field '{name}' in type '{RuntimeType.Name}'");
        }
    }

    protected virtual bool CanOverrideField(string name) {
        if (NativeFieldBindings.TryGetValue(GetType(), out var fields)) {
            if (fields.TryGetValue(name, out var bind)) {
                if (bind.Name == name) {
                    return false;
                }
            }
        }

        return true;
    }

    protected virtual void SetFieldOwner(BaseValue value, RuntimeType fieldType) {
        value.Outer = this;

        if (value is ValueFunction funcType && value.RuntimeType is RuntimeTypeInfo_Function funcInfo) {
            funcInfo.Owner = RuntimeType;
        }
    }

    public virtual void SetField(BaseValue name, BaseValue value) => SetField(name.Value<string>(), value);

    public virtual void SetFields(IEnumerable<(string, BaseValue)> fields) {
        foreach (var (name, value) in fields) {
            SetField(name, value);
        }
    }

    public virtual BaseValue this[string name] {
        get => GetField(name);
        set => SetField(name, value);
    }
}