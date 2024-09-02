using System.Text.RegularExpressions;
using CSScriptingLang.RuntimeValues;

namespace CSScriptingLang.Interpreter.Bindings;

public class PrintFunctions
{
    [NativeFunctionBind("print")]
    public static void Native_Print(NativeFunctionExecutionContext context, params object[] args) {
        var interpreter = context.Interpreter;
        var frame       = context.Frame;

        var rtArgs = args as RuntimeValue[];
        if (rtArgs == null) {
            Console.WriteLine("Print: null");
            return;
        }

        var argCount = rtArgs.Length;
        if (argCount == 0) {
            Console.WriteLine();
            return;
        }

        /*string FixFormatStr(string str) {
            // Find all '{0-9}' and '{}' and find the largest {x} number, then replace all {} with {0-9}

            var matches = Regex.Matches(str, @"\{(\d+)?\}");
            if (matches.Count == 0)
                return str;

            int max = 0;
            foreach (Match match in matches) {
                if (match.Groups.Count > 1) {
                    var num = int.Parse(match.Groups[1].Value);
                    if (num > max)
                        max = num;
                }
            }

            var newStr = str;
            for (int i = 0; i <= max; i++) {
                newStr = newStr.Replace($"{{{i}}}", $"{{{i}}}");
            }

            return newStr;
        }*/

        var hasFormatArg = rtArgs.Length > 0 && rtArgs[0].Type == RTVT.String;
        if (hasFormatArg) {
            object[] paramsObj = rtArgs.Skip(1)
               .Select(arg => arg.Inspect())
               .ToArray<object>();

            var str = rtArgs[0].Inspect();
            // str = FixFormatStr(str);

            var formatted = string.Format(str, paramsObj);

            Console.WriteLine(formatted);

            return;
        } else {
            for (int i = 0; i < argCount; i++) {
                Console.Write(rtArgs[i].Inspect());
                if (i < argCount - 1)
                    Console.Write(" ");
            }

            if (argCount > 0)
                Console.WriteLine();
        }
    }

    [NativeFunctionBind("inspect")]
    public static void Native_Inspect(
        NativeFunctionExecutionContext context,
        RuntimeValue                   value,
        [NativeFunctionParameterBind(RTVT.String)]
        RuntimeValue contextString
    ) {
        var interpreter = context.Interpreter;
        var frame       = context.Frame;

        if (value == null) {
            Console.WriteLine("Inspect: null");
            return;
        }

        Console.WriteLine($"Inspect({value} - {(contextString != null ? $"Context: '{contextString.As<string>()}'" : "")})");
        Console.WriteLine($"\tType: {value.Type}");
        Console.WriteLine($"\tValue: {value.Value}");
        Console.WriteLine($"\tReferenceCount: {value.ReferenceCount}");

    }
}