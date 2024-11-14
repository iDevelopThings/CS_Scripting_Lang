using CSScriptingLang.Core.Http;
using CSScriptingLang.Core.Async;
using CSScriptingLang.Core.Serialization.JsonSerialization;
using CSScriptingLang.Interpreter.Bindings;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Lexing;
using CSScriptingLang.RuntimeValues.Prototypes;
using CSScriptingLang.RuntimeValues.Values;

namespace CSScriptingLang.Interpreter.Libraries;

[LanguageModuleBind(FunctionsAsGlobals = true)]
public static partial class Lib_InstanceCreator
{
    [LanguageGlobalFunction("new")]
    [LanguageMetaDefinition("def function new<T>(...object args) T;")]
    [LanguageFunctionDisableParameterChecks]
    public static Value New(FunctionExecContext ctx, params Value[] args) {
        if (ctx.TypeArgs.Count != 1) {
            throw new InterpreterRuntimeException("new: Expected 1 type argument");
        }

        var type = ctx.TypeArgs[0];

        var ctor  = type.Type.GetConstructorFn();
        var val   = ctor.Call(ctx, type.Type, args);
        
        if(type.Type.PrototypeInstance is StructPrototype sp) {
            var userCtorMember = sp.FindConstructorForArgs(args);
            if(userCtorMember != null) {
                var userCtor = userCtorMember.ValueConstructor();
                userCtor.DispatchCall(ctx, val, args);
            }
        }
        
        return val;
    }
}

public class StandardLibraries : ILibraryCollection
{
    public IEnumerable<ILibrary> Create(ExecContext ctx) {
        yield return new Lib_Require.Library();
        yield return new Lib_Inspect.Library();
        yield return new Lib_Logging.Library();
        yield return new Lib_InstanceCreator.Library();
        yield return new Lib_Http.Library();
        yield return new Lib_Json.Library();
        yield return new AsyncContext.Library();
    }
}