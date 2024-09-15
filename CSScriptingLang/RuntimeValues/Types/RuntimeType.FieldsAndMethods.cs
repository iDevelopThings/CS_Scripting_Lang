using CSScriptingLang.RuntimeValues.Values;
using CSScriptingLang.Utils;

namespace CSScriptingLang.RuntimeValues.Types;

public abstract partial class RuntimeType
{
    public class RuntimeTypeField
    {
        public string          Name             { get; set; }
        public BaseValue       Type             { get; set; }
        public Func<BaseValue> ValueConstructor { get; set; }
    }

    public Dictionary<string, RuntimeTypeField> Fields { get; } = new();

    public RuntimeType RegisterField(string name, BaseValue type) {
        Fields[name] = new RuntimeTypeField {
            Name             = name,
            Type             = type,
            ValueConstructor = type.Clone
        };

        return this;
    }

    public ValueFunction.ExecutableFunction GetValueConstructor() {
        if (Fields.TryGetValue("__ctor", out var ctor)) {
            return ((ValueFunction) ctor.Type).Executable;
        }

        return null;
    }
}