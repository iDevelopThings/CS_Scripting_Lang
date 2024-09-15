using System.Text;
using CSScriptingLang.Interpreter.Bindings;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.RuntimeValues.Values;

namespace CSScriptingLang.Interpreter.Libraries;

[LanguageModuleBind(FunctionsAsGlobals = true)]
public static partial class Lib_Inspect
{
    // public sealed partial class Library : ILibrary
    // {
    //     public IEnumerable<KeyValuePair<string, Value>> GetDefinitions(ExecContext ctx) {
    //         yield break;
    //     }
    // }

    [LanguageGlobalFunction("inspect")]
    public static void Inspect(
        Value  inspectValue,
        string contextString = null
    ) {
        Console.Write("Inspect");

        if (inspectValue.Symbol?.Name != null) {
            Console.Write($" (");
            Console.Write(inspectValue.Symbol.Name);
            // if (inspectValue.Symbol.Reference != null) {
            //     Console.Write($" -> reference of : {inspectValue.Symbol.Reference.Name}");
            // }

            Console.Write(")");
        }

        if (contextString != null) {
            Console.Write($" ({contextString})");
        }

        Console.WriteLine(":");

        Console.WriteLine($"\tType: {inspectValue.PrototypeType?.ValueType?.Name}(RTVT={inspectValue.Type})");

        string WriteValue(Value Value, int indent = 0) {
            string GetIndent(int i) => new(' ', i * 2);
            var ident = GetIndent(indent);
            var sb    = new StringBuilder();
            
            switch (Value) {
                case {Is.Array: true}: {
                    var arr = Value.As.List();
                    sb.Append($"\n{ident}[\n");
                    for (var i = 0; i < arr.Count; i++) {
                        var val = arr[i];
                        sb.Append($"{ident}{GetIndent(indent + 1)}{i}: ");
                        var valStr = WriteValue(val);
                        sb.Append(valStr);
                        if (i < arr.Count - 1) {
                            sb.Append(", ");
                        }
                    }

                    sb.Append($"\n{ident}]");
                    break;
                }
                case {Is.Object: true}: {
                    var obj = Value.As.Object();
                    sb.Append($"{ident}{{");
                    foreach (var (key, val) in obj) {
                        sb.Append($"{ident}{GetIndent(indent + 1)}{key}: ");
                        var valStr = WriteValue(val, 0);
                        sb.Append(valStr);
                        sb.Append(", ");
                    }

                    sb.Append($"{ident}}}");
                    break;
                }
                case {Is.String: true}: {
                    sb.Append($"{ident}\"{Value.As.String()}\"");
                    break;
                }
                case {Is.Number: true}: {
                    sb.Append($"{ident}{Value.Type.FormattedStringNumber(Value.GetUntypedValue())}");
                    break;
                }
                case {Is.Boolean: true}: {
                    sb.Append($"{ident}{Value.As.Bool()}");
                    break;
                }
                case {Is.Null: true}: {
                    sb.Append($"{ident}null");
                    break;
                }
                case {Is.Unit: true}: {
                    sb.Append($"{ident}unit");
                    break;
                }
                case {Is.Function: true}: {
                    sb.Append($"{ident}function({Value.As.Fn().Name})");
                    break;
                }
                
                default: {
                    throw new NotImplementedException();
                }
            }

            return sb.ToString();
        }

        Console.WriteLine($"\tValue: {string.Join("\n\t\t", WriteValue(inspectValue).Split('\n'))}");

        var fields = inspectValue.Members.ToList();
        if (fields.Count > 0) {
            Console.WriteLine("\tFields:");
            foreach (var (name, val) in fields) {
                Console.WriteLine($"\t\t{name}: {val.ToString()}");
            }
        }

        /*if (inspectValue is ValueSignal signal) {
            Console.WriteLine("\tSignal:");
            Console.WriteLine($"\t\tSubscribers: {signal.Listeners.Count}");
        }

        if (inspectValue is ValueFunction func) {
            Console.WriteLine("\tFunction:");
            if (func.Value is { } fn) {
                Console.WriteLine($"\t\tParameters: {(fn.Parameters.Count == 0 ? "None" : string.Join(", ", fn.Parameters.Arguments.Select(p => p.Name)))}");
            }
        }*/
    }
}