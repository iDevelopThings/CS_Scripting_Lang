using CSScriptingLang.Interpreter;
using CSScriptingLang.Parsing.AST;

namespace CSScriptingLang.VM.Tables;

public class FunctionTable : ScopeAware<FunctionTable>
{
    private static object _globalNativeFunctionsLock = new();
    public static Dictionary<string, Action<VirtualMachine, FunctionFrame>> GlobalNativeFunctions = new();

    public Dictionary<string, InlineFunctionDeclarationNode> Declarations = new();

    public Dictionary<string, int> Index       = new();
    public Dictionary<string, int> ReturnIndex = new();

    public Stack<RuntimeTypeInfo_Function> InlineFunctionStack = new();

    public FunctionTable(ExecutionContext            context, FunctionTable parent = null) : base(context, parent) { }
    public FunctionTable(InterpreterExecutionContext context, FunctionTable parent = null) : base(context, parent) { }

    public int this[string key] {
        get => TryGet(key, out var value) ? value : Parent?[key] ?? -1;
        set => Add(key, value);
    }

    public void Declare(string key, InlineFunctionDeclarationNode value) => Declarations.TryAdd(key, value);
    public bool TryGetDeclaration(string key, out InlineFunctionDeclarationNode value) {
        if (Declarations.TryGetValue(key, out value))
            return true;

        return Parent?.TryGetDeclaration(key, out value) ?? false;
    }

    public bool TryGet(string key, out int value) {
        if (Index.TryGetValue(key, out var val)) {
            value = val;
            return true;
        }

        if (Parent != null)
            return Parent.TryGet(key, out value);

        value = -1;
        return false;
    }

    public static bool TryGetNativeFunction(string key, out Action<VirtualMachine, FunctionFrame> value) {
        if (GlobalNativeFunctions.TryGetValue(key, out var val)) {
            value = val;
            return true;
        }

        value = null;
        return false;
    }

    public void Add(string key, int value) => Index.TryAdd(key, value);

    public void AddReturn(string key, int value) => ReturnIndex.TryAdd(key, value);
    public int  GetReturn(string key) => ReturnIndex.GetValueOrDefault(key, -1);

    public static void AddNativeFunction(string key, Action<FunctionFrame> value)
        => AddNativeFunction(key, (_, frame) => value(frame));

    public static FunctionDeclarationNode AddNativeFunction(string key, Action<VirtualMachine, FunctionFrame> value) {
        lock (_globalNativeFunctionsLock) {
            GlobalNativeFunctions.TryAdd(key, value);

            var decl = new FunctionDeclarationNode(key, new(), null);

            CompileTimeFunctionTable.Global.Register(key, decl);

            return decl;
        }
    }
    public override void Dispose() {
        base.Dispose();
    }
}