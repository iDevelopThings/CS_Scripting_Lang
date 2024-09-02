using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.Utils.CodeWriter;

namespace CSScriptingLang.RuntimeValues;

public class RuntimeValue_Object : RuntimeValue
{
    public new RuntimeTypeInfo_Object RuntimeType {
        get => (RuntimeTypeInfo_Object) base.RuntimeType;
        set => base.RuntimeType = value;
    }

    public Dictionary<string, RuntimeValue> Fields {
        get => (Dictionary<string, RuntimeValue>) Value;
        set => Value = value;
    }

    public RuntimeValue_Object() {
        _value = new Dictionary<string, RuntimeValue>();
    }

    public override RuntimeValue GetField(string name) {
        return Fields.GetValueOrDefault(name);
    }
    public override RuntimeValue GetField(RuntimeValue name) {
        return GetField(name.As<string>());
    }

    public override void SetField(string name, RuntimeValue value) {
        if (!Fields.ContainsKey(name)) {
            var fieldType = RuntimeType.RegisterField(name, value.RuntimeType);
            if (value is RuntimeValue_Function funcType) {
                funcType.Object                              = this;
                ((RuntimeTypeInfo_Function) fieldType).Owner = RuntimeType;
            }
        }

        Fields[name] = value;
    }

    public override void SetField(RuntimeValue name, RuntimeValue value) {
        SetField(name.As<string>(), value);
    }

    public override string Inspect(Writer parentWriter = null) {
        var w = new Writer(parentWriter);

        using (w.bNoIndent("Object")) {
            foreach (var pair in Fields) {
                w.WriteInlineIndented($"{pair.Key} : ");
                var val = pair.Value.Inspect(w);
                w.WriteInline(val);
                w.WriteInline(",\n");
            }
        }


        return w.ToString();
    }
}