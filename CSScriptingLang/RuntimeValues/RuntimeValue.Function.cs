using CSScriptingLang.Utils;
using CSScriptingLang.Utils.CodeWriter;

namespace CSScriptingLang.RuntimeValues;

public class RuntimeValue_Function : RuntimeValue
{
    public int                 Index  { get; set; }
    public RuntimeValue_Object Object { get; set; }

    public RuntimeValue_Function() { }
    public RuntimeValue_Function(int index) {
        Index = index;
    }

    public override string Inspect(Writer parentWriter = null) {
        var w = new Writer(parentWriter);

        w.WriteInline($"Function({Index}:{RuntimeType.Name})");

        return w.ToString();
    }
}