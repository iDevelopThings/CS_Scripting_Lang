using CSScriptingLang.Lexing;
using CSScriptingLang.RuntimeValues;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.RuntimeValues.Values;
using Engine.Engine.Logging;

namespace CSScriptingLang.Interpreter.Bindings;

public class ReflectionFunctions
{
    public static Logger Logger = Logs.Get<ReflectionFunctions>()
       .SetLogFlags(LoggerFlags.All);

    [NativeFunctionBind("reflect")]
    public static ValueObject Native_Reflect(ref NativeFunctionExecutionContext ctx) {
        /*var caller    = ctx.Ctx.CurrentCallFrame.ReturnNode;
        var typeParam = caller.TypeParameters?.FirstOrDefault();

        if (typeParam == null) {
            throw new InterpreterException("Struct type not specified", caller, caller.GetScript());
        }
        
        var type = TypeTable.Current.Get(typeParam.Name);
        if (type == null) {
            throw new InterpreterException($"Struct type '{typeParam.Name}' not found", caller, caller.GetScript());
        }

        
        
        var obj = ValueFactory.Object.Make();

        var fieldsObj      = ValueFactory.Object.Make();
        obj.SetField("fields", fieldsObj);
        
        foreach (var pair in type.Fields) {
            fieldsObj.SetField(pair.Key, ValueFactory.Object.Make(new ObjectDictionary() {
                {"name", ValueFactory.String.Make(pair.Key)},
                {"type", ValueFactory.String.Make(pair.Value.Type.RuntimeType.Name)}
            }));
        }

        return obj;*/
        return null;
    }

}