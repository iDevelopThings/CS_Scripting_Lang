using CSScriptingLang.Interpreter.Modules;
using CSScriptingLang.Utils;
using CSScriptingLang.VM;
using CSScriptingLang.VM.Tables;
using Engine.Engine.Logging;

namespace CSScriptingLang.Interpreter;

public class InterpreterExecutionContext : IDisposable
{
    private static Logger Logger = Logs.Get<InterpreterExecutionContext>();

    public Interpreter                 Interpreter { get; set; }
    public InterpreterExecutionContext Parent      { get; set; }
    public InterpreterExecutionContext Current     => Interpreter.Context;

    public SymbolTable SymbolTable { get; set; }
    public TypeTable   TypeTable   { get; set; }

    public ModuleRegistry ModuleRegistry { get; set; }
    public Module         Module         { get; set; }

    public bool IsGlobal { get; set; }

    public object ContextObject { get; set; }

    public static InterpreterExecutionContext Create(Interpreter interpreter, bool isRoot = false) {
        var context = new InterpreterExecutionContext {
            Interpreter = interpreter,
            Parent      = isRoot ? null : interpreter.Context,
            IsGlobal    = isRoot
        };

        context.SymbolTable = new SymbolTable(context, context.Parent?.SymbolTable);
        context.TypeTable   = new TypeTable(context, context.Parent?.TypeTable);

        context.ModuleRegistry = context.Parent != null ? context.Parent.ModuleRegistry : new ModuleRegistry(context);
        context.Module         = context.Parent?.Module ?? new Module("global");

        if (isRoot) {
            TypeTable.GlobalTypeTable = context.TypeTable;
            context.TypeTable.RegisterStaticTypes();
        }

        return context;
    }

    public override string ToString() {
        return $"InterpreterExecutionContext: {Module.Name}\n" +
               $"\tSymbols={SymbolTable.Symbols.Count}, \n" +
               $"\tTypes={TypeTable.Types.Count},\n" +
               $"{(ContextObject != null ? $"\tContextObject: {ContextObject}\n" : "")}";
    }

    public InterpreterExecutionContext Pop() {
        if (Interpreter.ContextStack.TryPeek(out var context)) {
            context.Dispose();
            return context;
        }

        return null;
    }

    public void Dispose() {
        /*if(Interpreter.CurrentFrame?.DeferStatements.Count > 0) {
            Interpreter.CurrentFrame.DeferStatements.ForEach(defer => {
                Interpreter.Execute(defer);
            });
        }*/
        if (SymbolTable?.TryDisposeValues() == true) {
            SymbolTable?.RefCountLogger.Debug($"Can dispose table '{SymbolTable.ToString().BoldBrightWhite()}'");
            SymbolTable?.Dispose();
        }

        Interpreter.ContextStack.Pop();
        
        InterpreterEvents.OnExecutionScopePopped?.Invoke(this);
    }

    public int GetDepth() {
        var depth   = 0;
        var current = this;
        while (current.Parent != null) {
            depth++;
            current = current.Parent;
        }

        return depth;
    }
}