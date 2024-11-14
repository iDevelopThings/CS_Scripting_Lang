using CSScriptingLang.Interpreter.Bindings;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.RuntimeValues.Values;

namespace CSScriptingLang.Interpreter.Libraries;

[LanguageModuleBind(FunctionsAsGlobals = true)]
public static partial class Lib_Require
{
    [LanguageGlobalFunction("require")]
    public static Value Require(ExecContext ctx, string path) {
        var result = Interpreter.Instance.ModuleResolver.Resolve(ctx, path);

        return result;
    }
}