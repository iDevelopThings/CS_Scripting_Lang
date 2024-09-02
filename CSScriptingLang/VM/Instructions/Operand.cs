using CSScriptingLang.RuntimeValues;

namespace CSScriptingLang.VM.Instructions;


public abstract class OperandBase
{
    public abstract string AsString();
}

public abstract class Operand<T> : OperandBase where T : Operand<T> { }


public partial class OperandRuntimeValue : Operand<OperandRuntimeValue>
{
    public RuntimeValue Value { get; set; }

    public OperandRuntimeValue(RuntimeValue value) {
        Value = value;
    }

    public override string AsString() => Value.ToString();
}

public partial class OperandVariable : Operand<OperandVariable>
{
    public string Name { get; set; }

    public OperandVariable(string variableName) {
        Name = variableName;
    }

    public override string AsString() => Name;
}

public partial class OperandInstructionIdx : Operand<OperandInstructionIdx>
{
    public int Index { get; set; }

    public OperandInstructionIdx(int idx) {
        Index = idx;
    }

    public override string AsString() => Index.ToString();
}