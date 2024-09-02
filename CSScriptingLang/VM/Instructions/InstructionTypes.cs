using CSScriptingLang.Lexing;
using CSScriptingLang.RuntimeValues;
using Generators;

namespace CSScriptingLang.VM.Instructions;

[InstructionHandlerAllowFallthrough]
public class InstructionMarker : Instruction
{
    public override OpCode OpCode        => OpCode.Marker;
    public override string OperandString => "";

    public string Name          { get; set; }
    public bool   LogExecutions { get; set; }

    public List<object> Context { get; set; } = new();

    protected InstructionMarker(string name, bool logExecutions, params object[] context) {
        Name          = name;
        LogExecutions = logExecutions;

        Context.AddRange(context);
    }
}

public class InstructionBlockMarkerBegin : InstructionMarker
{
    public InstructionBlockMarkerBegin(string name, bool logExecutions, params object[] context)
        : base(name, logExecutions, context) { }
}

public class InstructionBlockMarkerEnd : InstructionMarker
{
    public InstructionBlockMarkerEnd(string name, bool logExecutions, params object[] context)
        : base(name, logExecutions, context) { }
}

#region Stack Management Instruction

public class InstructionLoadTemporaryInlineFunction : TypedInstruction<InstructionLoadTemporaryInlineFunction>
{
    public override OpCode OpCode => OpCode.LoadTemporaryInlineFunction;
    public InstructionLoadTemporaryInlineFunction() { }
}

// Pushes a value onto the stack or var table depending on the operand
public abstract class InstructionLoad : TypedInstruction<InstructionLoad>
{
    public override OpCode OpCode => OpCode.Load;
}

[VMInstruction<OperandRuntimeValue>]
public class InstructionLoadConstant : InstructionLoad
{
    public OperandRuntimeValue Operand {
        get => (OperandRuntimeValue) _operand;
        set => _operand = value;
    }

    public override string OperandString => Operand.AsString();

    public InstructionLoadConstant(RuntimeValue value) {
        SetOperand<OperandRuntimeValue>(value);
    }
}

[VMInstruction<OperandVariable>]
public class InstructionLoadVariable : InstructionLoad
{
    public OperandVariable Operand {
        get => (OperandVariable) _operand;
        set => _operand = value;
    }

    public override string OperandString => Operand.AsString();

    public InstructionLoadVariable(string value) {
        SetOperand<OperandVariable>(value);
    }
}

// Stores the top value on the stack in a variable
[VMInstruction<OperandVariable>]
public class InstructionStore : TypedInstruction<InstructionStore>
{
    public override OpCode OpCode => OpCode.Store;

    public OperandVariable Operand {
        get => (OperandVariable) _operand;
        set => _operand = value;
    }

    public override string OperandString => Operand.AsString();

    public InstructionStore(string value) {
        SetOperand<OperandVariable>(value);
    }
}

// Pops a value from the stack
[VMInstruction<OperandStackPopCount>]
public class InstructionStackPop : TypedInstruction<InstructionStackPop>
{
    public override OpCode OpCode => OpCode.Pop;

    public class OperandStackPopCount : Operand<OperandStackPopCount>
    {
        public int PopCount { get; set; }
        public OperandStackPopCount(int count = 1) {
            PopCount = count;
        }
        public override string AsString() => PopCount.ToString();
    }


    public OperandStackPopCount Operand {
        get => (OperandStackPopCount) _operand;
        set => _operand = value;
    }

    public override string OperandString => Operand.AsString();

    public InstructionStackPop(int value = 1) {
        SetOperand<OperandStackPopCount>(value);
    }
}

#endregion

#region Function Instructions

public class OperandDeclareFunction : Operand<OperandDeclareFunction>
{
    public struct FunctionParameter
    {
        public string Name;
        public string Type;
    }

    public string Name { get; set; }

    public List<FunctionParameter> Parameters { get; set; } = new();

    public int BodyStartIndex { get; set; }
    public int BodyEndIndex   { get; set; }

    public OperandDeclareFunction(string name) {
        Name = name;
    }

    public OperandDeclareFunction AddParameter(string name, string type) {
        Parameters.Add(new FunctionParameter {Name = name, Type = type});
        return this;
    }

    public override string AsString() => Name;
}

[VMInstruction<OperandLoadFunctionParameter>]
public class InstructionLoadFunctionParameter : TypedInstruction<InstructionLoadFunctionParameter>
{
    public class OperandLoadFunctionParameter : Operand<OperandLoadFunctionParameter>
    {
        public int Index { get; set; }
        
        public OperandLoadFunctionParameter(int index) {
            Index = index;
        }
        
        public override string AsString() => Index.ToString();
    }
    
    public override OpCode OpCode => OpCode.LoadFunctionParameter;

    public OperandLoadFunctionParameter Operand {
        get => (OperandLoadFunctionParameter) _operand;
        set => _operand = value;
    }

    public override string OperandString => Operand.AsString();

    public InstructionLoadFunctionParameter(int value) {
        SetOperand<OperandLoadFunctionParameter>(value);
    }
}

[VMInstruction<OperandDeclareFunction>]
public class InstructionDeclareInlineFunction : TypedInstruction<InstructionDeclareInlineFunction>
{
    public override OpCode OpCode => OpCode.DeclareInlineFunction;

    public OperandDeclareFunction Operand {
        get => (OperandDeclareFunction) _operand;
        set => _operand = value;
    }

    public override string OperandString => Operand.AsString();

    public InstructionDeclareInlineFunction() {
        SetOperand<OperandDeclareFunction>("");
    }
}

[VMInstruction<OperandDeclareFunction>]
public class InstructionDeclareFunction : TypedInstruction<InstructionDeclareFunction>
{
    public override OpCode OpCode => OpCode.DeclareFunction;

    public OperandDeclareFunction Operand {
        get => (OperandDeclareFunction) _operand;
        set => _operand = value;
    }

    public override string OperandString => Operand.AsString();

    public InstructionDeclareFunction(string value) {
        SetOperand<OperandDeclareFunction>(value);
    }
}

[VMInstruction<OperandCallFunction>]
public class InstructionCallFunction : TypedInstruction<InstructionCallFunction>
{
    public override OpCode OpCode => OpCode.CallFunction;

    public class OperandCallFunction : Operand<OperandCallFunction>
    {
        public string Name    { get; set; }
        public int    NumArgs { get; set; }

        public OperandCallFunction(string name) {
            Name = name;
        }

        public override string AsString() => Name;
    }

    public OperandCallFunction Operand {
        get => (OperandCallFunction) _operand;
        set => _operand = value;
    }

    public override string OperandString => Operand.AsString();

    public InstructionCallFunction(string value) {
        SetOperand<OperandCallFunction>(value);
    }
}

[VMInstruction<OperandCallFunction>]
public class InstructionCallNativeFunction : TypedInstruction<InstructionCallNativeFunction>
{
    public override OpCode OpCode => OpCode.CallNativeFunction;

    public class OperandCallFunction : Operand<OperandCallFunction>
    {
        public string Name    { get; set; }
        public int    NumArgs { get; set; }

        public OperandCallFunction(string name) {
            Name = name;
        }

        public override string AsString() => Name;
    }

    public OperandCallFunction Operand {
        get => (OperandCallFunction) _operand;
        set => _operand = value;
    }

    public override string OperandString => Operand.AsString();

    public InstructionCallNativeFunction(string value) {
        SetOperand<OperandCallFunction>(value);
    }
}
#endregion

#region ReturnStatement Instruction

[VMInstruction<OperandReturn>]
public class InstructionReturnStatement : TypedInstruction<InstructionReturnStatement>
{
    public override OpCode OpCode => OpCode.Return;

    public OperandReturn Operand {
        get => (OperandReturn) _operand;
        set => _operand = value;
    }

    public override string OperandString => Operand?.AsString();

    public class OperandReturn : Operand<OperandReturn>
    {
        public bool IsVoid { get; set; }

        public OperandReturn(bool isVoid = false) {
            IsVoid = isVoid;
        }

        public override string AsString() => IsVoid ? "void" : "value";
    }


    public InstructionReturnStatement(bool value = true) {
        SetOperand<OperandReturn>(value);
    }
}

#endregion

#region Jump/ProgramState Instructions

public class InstructionHaltProgram : Instruction
{
    public override OpCode OpCode        => OpCode.Halt;
    public override string OperandString => "";
}

public abstract class InstructionWithIdx : Instruction
{
    // public override OpCode OpCode => OpCode.JumpIfFalse;

    public OperandInstructionIdx Operand {
        get => (OperandInstructionIdx) _operand;
        set => _operand = value;
    }

    public override string OperandString => Operand.AsString();

    protected InstructionWithIdx(int value) {
        SetOperand<OperandInstructionIdx>(value);
    }
}

public class InstructionJump : InstructionWithIdx
{
    public override OpCode OpCode => OpCode.Jump;
    public InstructionJump(int value) : base(value) { }
}

public class InstructionJumpIfFalse : InstructionWithIdx
{
    public override OpCode OpCode => OpCode.JumpIfFalse;
    public InstructionJumpIfFalse(int value) : base(value) { }
}

public class InstructionJumpIfTrue : InstructionWithIdx
{
    public override OpCode OpCode => OpCode.JumpIfTrue;
    public InstructionJumpIfTrue(int value) : base(value) { }
}

#endregion

#region Block Instructions

public class InstructionBlockBegin : Instruction
{
    public override OpCode OpCode        => OpCode.BeginBlock;
    public override string OperandString => "";
}

public class InstructionBlockEnd : Instruction
{
    public override OpCode OpCode        => OpCode.EndBlock;
    public override string OperandString => "";
}

#endregion

#region Loop Instructions

public class InstructionLoopSetup : Instruction
{
    public override OpCode OpCode        => OpCode.LoopSetup;
    public override string OperandString => "";
}

public class InstructionLoopCondition : Instruction
{
    public override OpCode OpCode        => OpCode.LoopCondition;
    public override string OperandString => "";
}

public class InstructionLoopIteration : Instruction
{
    public override OpCode OpCode        => OpCode.LoopIteration;
    public override string OperandString => "";
}

#endregion

#region Math/Operator Instructions

[VMInstruction<OperandBinaryOperation>]
public class InstructionBinaryOperation : Instruction
{
    public override OpCode OpCode => OpCode.BinaryOp;

    public class OperandBinaryOperation : Operand<OperandBinaryOperation>
    {
        public OperatorType Op { get; set; }

        public OperandBinaryOperation(OperatorType op) {
            Op = op;
        }

        public override string AsString() => Op.ToString();
    }

    public OperandBinaryOperation Operand {
        get => (OperandBinaryOperation) _operand;
        set => _operand = value;
    }

    public override string OperandString => Operand.AsString();

    public InstructionBinaryOperation(OperatorType value) {
        SetOperand<OperandBinaryOperation>(value);
    }
}

[VMInstruction<OperandUnaryOperation>]
public class InstructionUnaryOperation : Instruction
{
    public override OpCode OpCode => OpCode.UnaryOp;

    public class OperandUnaryOperation : Operand<OperandUnaryOperation>
    {
        public OperatorType Op        { get; set; }
        public string       VarName   { get; set; }
        public bool         IsPostfix { get; set; }

        public OperandUnaryOperation(OperatorType op, string varName, bool isPostfix) {
            Op        = op;
            VarName   = varName;
            IsPostfix = isPostfix;
        }

        public override string AsString() => Op.ToString();
    }

    public OperandUnaryOperation Operand {
        get => (OperandUnaryOperation) _operand;
        set => _operand = value;
    }

    public override string OperandString => Operand.AsString();

    public InstructionUnaryOperation(OperatorType value, string varName, bool isPostfix) {
        SetOperand<OperandUnaryOperation>(value, varName, isPostfix);
    }
}

#endregion

#region Object Instructions

[VMInstruction<OperandCreateObject>]
public class InstructionCreateObject : TypedInstruction<InstructionCreateObject>
{
    public override OpCode OpCode => OpCode.CreateObject;

    public class OperandCreateObject : Operand<OperandCreateObject>
    {
        public List<string> Properties { get; set; } = new();

        public OperandCreateObject() { }
        public OperandCreateObject(List<string> properties) {
            Properties = properties;
        }

        public override string AsString() => string.Join(", ", Properties);
    }

    public OperandCreateObject Operand {
        get => (OperandCreateObject) _operand;
        set => _operand = value;
    }

    public override string OperandString => Operand.AsString();

    public InstructionCreateObject(List<string> value) {
        SetOperand<OperandCreateObject>(value);
    }
}

public class InstructionLoadIndex : Instruction
{
    public override OpCode OpCode        => OpCode.LoadIndex;
    public override string OperandString => "";
}

public class InstructionLoadProperty : Instruction
{
    public override OpCode OpCode        => OpCode.LoadProperty;
    public override string OperandString => "";
}

public class InstructionStoreProperty : Instruction
{
    public override OpCode OpCode        => OpCode.StoreProperty;
    public override string OperandString => "";
}

#endregion