using System.Runtime.CompilerServices;
using CSScriptingLang.Utils;

namespace CSScriptingLang.VM.Instructions;

public abstract class Instruction
{
    public abstract OpCode OpCode        { get; }
    public abstract string OperandString { get; }

    public int    Index  { get; set; }
    public Caller Caller { get; set; }

    protected object _operand = null;

    public object UntypedOperand => _operand;

    private Type _instructionType;
    public Type InstructionType {
        get => _instructionType ??= GetType();
        set => _instructionType = value;
    }

    public T OperandAs<T>() where T : OperandBase => (T) _operand;
    public T SetOperand<T>(params object[] args) where T : OperandBase {
        _operand = (T) Activator.CreateInstance(typeof(T), args);
        return OperandAs<T>();
    }
    public T SetOperand<T>(T operand) where T : OperandBase {
        _operand = operand;
        return OperandAs<T>();
    }

    public static T Create<T>([CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string method = "") where T : Instruction, new() {
        return new T {
            Index  = -1,
            Caller = Caller.FromAttributes(file, line, method)
        };
    }

    public override string ToString() => $"{OpCode} -> {OperandString}";
}

public abstract class TypedInstruction<TInstruction> : Instruction
    where TInstruction : TypedInstruction<TInstruction>
{
    public override string OperandString => "";
}

public abstract class TypedInstructionOperand<TInstruction, TOperand> : TypedInstruction<TInstruction>
    where TInstruction : TypedInstructionOperand<TInstruction, TOperand>
    where TOperand : OperandBase
{
    public TOperand Operand {
        get => (TOperand) _operand;
        set => _operand = value;
    }

    public override string OperandString => "";
}