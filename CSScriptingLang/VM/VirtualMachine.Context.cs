using CSScriptingLang.VM.Tables;
using Engine.Engine.Logging;

namespace CSScriptingLang.VM;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = false)]
public class InstructionHandlerAllowFallthroughAttribute : Attribute { }

public partial class VirtualMachine
{
    private Logger Logger = Logs.Get<VirtualMachine>();

    public ExecutionContext     Context       => ExecutionContext.Current;
    public FunctionTable        FunctionTable => Context.FunctionTable;
    public SymbolTable          Symbols       => Context.Symbols;
    public TypeTable            TypeTable     => Context.TypeTable;
    public Stack<FunctionFrame> CallStack     => ExecutionContext.CallStack;

    public ExecutionContext PushFrame() => ExecutionContext.Create(this);
    public ExecutionContext PopFrame()  => ExecutionContext.Pop();
}