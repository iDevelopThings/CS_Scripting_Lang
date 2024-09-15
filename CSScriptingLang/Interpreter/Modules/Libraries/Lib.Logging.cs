using CSScriptingLang.Interpreter.Bindings;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.RuntimeValues.Values;
using Engine.Engine.Logging;

namespace CSScriptingLang.Interpreter.Libraries;

[LanguageModuleBind(FunctionsAsGlobals = true)]
public static partial class Lib_Logging
{
    // public sealed partial class Library : ILibrary
    // {
    //     public IEnumerable<KeyValuePair<string, Value>> GetDefinitions(ExecContext ctx) {
    //         yield break;
    //     }
    // }

    [LanguageGlobalFunction("print")]
    public static void Print(params Value[] args) {

        if (args == null) {
            Console.WriteLine("Print: null");
            return;
        }

        var argCount = args.Length;
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

        var hasFormatArg = args.Length > 0 && args[0].Type == RTVT.String;
        if (hasFormatArg) {
            object[] paramsObj = args.Skip(1)
               .Select(arg => arg.Inspect())
               .ToArray<object>();

            var str = args[0].Inspect();
            // str = FixFormatStr(str);

            var formatted = string.Format(str, paramsObj);

            Console.WriteLine(formatted);

            return;
        }

        var argsString = string.Join(" ", args.Select(arg => arg.Inspect()));
        Console.WriteLine(argsString);

    }
}