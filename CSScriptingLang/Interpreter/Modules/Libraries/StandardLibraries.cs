using CSScriptingLang.Interpreter.Bindings;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Lexing;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.RuntimeValues.Values;

namespace CSScriptingLang.Interpreter.Libraries;

[LanguageModuleBind(FunctionsAsGlobals = true)]
public static partial class Lib_InstanceCreator
{
    [LanguageGlobalFunction("new")]
    public static Value New(FunctionExecContext ctx, params Value[] args) {
        if (ctx.TypeArgs.Count != 1) {
            throw new InterpreterRuntimeException("new: Expected 1 type argument");
        }
        
        var type = ctx.TypeArgs[0];
        
        var ctor = type.Type.GetValueConstructor();
        
        var value = ctor.Invoke(ctx, args);

        return value;
    }
}

public class StandardLibraries : ILibraryCollection
{
    public IEnumerable<ILibrary> Create(ExecContext ctx) {
        yield return new Lib_Inspect.Library();
        yield return new Lib_Logging.Library();
        yield return new Lib_InstanceCreator.Library();
    }
}