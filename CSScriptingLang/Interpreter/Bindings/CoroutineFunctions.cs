using System.Text.RegularExpressions;
using CSScriptingLang.RuntimeValues;
using CSScriptingLang.RuntimeValues.Types;

namespace CSScriptingLang.Interpreter.Bindings;

/*public class CoroutineFunctions
{
    [NativeFunctionBind("sleep")]
    public static RuntimeValue Native_Sleep(
        ref NativeFunctionExecutionContext context,
        [NativeFunctionParameterBind(RTVT.Number)]
        RuntimeValue duration
    ) {
        var interpreter = context.Interpreter;

        var res = interpreter.NewResult();

        var durationValue = (int) duration.Value;


        Console.WriteLine($"CoroutineFunctions.Native_Sleep: {durationValue}");

        return RuntimeValue.Rent(durationValue);

    }
}*/