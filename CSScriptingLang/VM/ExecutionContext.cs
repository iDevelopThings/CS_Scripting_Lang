using CSScriptingLang.VM.Tables;
using Engine.Engine.Logging;

namespace CSScriptingLang.VM;

public class ExecutionContext : IDisposable
{
    private static Logger                  Logger       = Logs.Get<ExecutionContext>();
    public static  Stack<ExecutionContext> ContextStack = new();
    public static  Stack<FunctionFrame>    CallStack    = new();
    public static  ExecutionContext        Current => ContextStack.TryPeek(out var context) ? context : null;

    public VirtualMachine   VM     { get; set; }
    public ExecutionContext Parent { get; set; }

    public FunctionTable FunctionTable;
    public SymbolTable   Symbols;
    public TypeTable     TypeTable;

    public static ExecutionContext Create(VirtualMachine vm, bool isRoot = false) {
        var context = new ExecutionContext {
            VM     = vm,
            Parent = isRoot ? null : Current,
        };

        context.Symbols = new SymbolTable(context, context.Parent?.Symbols) {
            FrameIp = vm.Ip - 1
        };

        context.FunctionTable = new FunctionTable(context, context.Parent?.FunctionTable);

        context.TypeTable = new TypeTable(context, context.Parent?.TypeTable);
        if (isRoot) {
            context.TypeTable.RegisterStaticTypes();
        }

        ContextStack.Push(context);

        return context;
    }

    public static ExecutionContext Pop() {
        if (ContextStack.Count == 0) {
            Logger.Error($"Attempted to pop symbol frame when there are no more frames to pop");
            return null;
        }

        var context = ContextStack.Pop();
        context.Dispose();
        return context;
    }

    public void Dispose() {
        Symbols?.Dispose();
        FunctionTable?.Dispose();
    }
}