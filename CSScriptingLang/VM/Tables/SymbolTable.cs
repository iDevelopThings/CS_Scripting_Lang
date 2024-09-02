using CSScriptingLang.Parsing.AST;
using CSScriptingLang.RuntimeValues;
using CSScriptingLang.Utils;

namespace CSScriptingLang.VM.Tables;

public class SymbolTable : ScopeAware<SymbolTable>
{
    public Dictionary<string, RuntimeValue> Values { get; } = new();

    public int FrameIp { get; set; }

    public SymbolTable(ExecutionContext context, SymbolTable parent = null) : base(context, parent) { }

    public static void Init() {
        // using var _ = ScopeTimer.NewWith("SymbolTable WarmUp");
        // ObjectPool<RuntimeValue>.WarmUp(1024);

        /*Interpreter.SymbolTable.DeclareNativeFunction(() => {
            return new FunctionDeclarationNode("print") {
                NativeFunction = (interpreter, frame) => {
                    var argCount = frame.Args.Count;
                    if (argCount == 0) {
                        Console.WriteLine();
                        return;
                    }

                    if (frame.Args[0].Type.Type == RTVT.String && argCount == 1) {
                        Console.WriteLine(frame.Args[0].Value.Inspect());
                        return;
                    }

                    if (frame.Args[0].Type.Type == RTVT.String) {
                        var paramsObj = frame.Args.Skip(1)
                           .Select(arg => arg.Value.Inspect())
                           .ToArray();
                        var str       = frame.Args[0].Value.Inspect();
                        var formatted = string.Format(str, paramsObj);
                        Console.WriteLine(formatted);
                        return;
                    }

                    for (int i = 0; i < argCount; i++) {
                        Console.Write(frame.Args[i].Value.Inspect());
                        if (i < argCount - 1)
                            Console.Write(" ");
                    }

                    if (argCount > 0)
                        Console.WriteLine();
                }
            };
        });*/

        /*FunctionTable.AddNativeFunction("print", (machine, frame) => {
                if (frame.NumArgs == 0) {
                    Console.WriteLine();
                    return;
                }

                frame.Args.Reverse();

                if (frame.Args[0].Type == RTVT.String && frame.NumArgs == 1) {
                    Console.WriteLine(frame.Args[0].Value);
                    return;
                }

                if (frame.Args[0].Type == RTVT.String) {
                    var paramsObj = frame.Args.Skip(1).Select(arg => arg.Value).ToArray();
                    var formatted = string.Format(frame.Args[0].As<string>(), paramsObj);
                    Console.WriteLine(formatted);
                    return;
                }

                for (int i = 0; i < frame.NumArgs; i++) {
                    Console.Write(frame.Args[i].Value);
                    if (i < frame.NumArgs - 1)
                        Console.Write(" ");
                }

                if (frame.NumArgs > 0)
                    Console.WriteLine();
            })
           .SetNative(true);*/
    }


    public bool Exists(string name) {
        if (Values.ContainsKey(name))
            return true;
        return Parent?.Exists(name) ?? false;
    }

    public RuntimeValue Add(string name, object value, bool canOverride) {
        if (Exists(name)) {
            if (canOverride) {
                return Values[name] = value as RuntimeValue ?? RuntimeValue.Rent(value);
            }

            return Values[name];
        }

        var val = value as RuntimeValue ?? RuntimeValue.Rent(value);
        Values.Add(name, val);

        return val;
    }

    public RuntimeValue this[string key] {
        get => TryGetValue(key, out var value) ? value : null;
        set => Add(key, value.Value, true);
    }

    public bool TryGetValue(string name, out RuntimeValue value) {
        if (Values.TryGetValue(name, out var val)) {
            value = val;
            return true;
        }

        if (Parent != null)
            return Parent.TryGetValue(name, out value);

        value = null;
        return false;
    }

    public override void Dispose() {
        foreach (var value in Values.Values) {
            value.Dispose();
        }

        Values.Clear();

        base.Dispose();
    }
}