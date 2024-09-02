using System.Runtime.CompilerServices;
using CSScriptingLang.Lexing;
using CSScriptingLang.RuntimeValues;
using CSScriptingLang.Utils;
using CSScriptingLang.VM.Instructions;
using CSScriptingLang.VM.Tables;

namespace CSScriptingLang.VM;

public struct FunctionFrame
{
    public int Ip { get; set; }

    public List<RuntimeValue> Args { get; set; } = new();

    public RuntimeValue Object { get; set; }

    public int NumArgs { get; set; }

    public FunctionFrame() {
        Ip      = 0;
        Object  = null;
        NumArgs = 0;
    }
}

public partial class VirtualMachine
{
    public int                 Ip           { get; set; }
    public List<Instruction>   Instructions { get; set; }
    public Stack<RuntimeValue> Stack = new();

    private bool IsRunning       { get; set; } = true;
    private bool LogStackChanges { get; set; } = true;

    public VirtualMachine() {
        ExecutionContext.Create(this, true); // not calling PushFrame() because this is the root context
    }
    public VirtualMachine(List<Instruction> instructions) {
        Instructions = instructions;

        ExecutionContext.Create(this, true); // not calling PushFrame() because this is the root context
    }
    public void Load(List<Instruction> compilerInstructions) {
        Instructions = compilerInstructions;
    }

    private RuntimeValue PopValue([CallerMemberName] string caller = "") {
        if (Stack.Count == 0) {
            Logger.Error("Stack is empty");
            return null;
        }

        var value = Stack.Pop();
        if (LogStackChanges) {
            Logger.Info($"[{Ip}][{caller}] Popped value: {value} - Size: {Stack.Count}");
        }

        return value;
    }
    private void PushValue(RuntimeValue value, [CallerMemberName] string caller = "") {
        Stack.Push(value);
        if (LogStackChanges) {
            Logger.Info($"[{Ip}][{caller}] Pushed value: {value} - Size: {Stack.Count}");
        }
    }

    public void ExecuteSafe() {
        Execute();

        /*
        try {
            Execute();
        }
        catch (Exception e) {
            Logger.Error(e.ToString());
            throw;
        }
        */
    }

    public void Execute() {
        using var _ = ScopeTimer.NewWith("VirtualMachine.Execute");

        while (Ip < Instructions.Count && IsRunning) {
            if (Ip < 0) {
                Console.WriteLine("IP is negative, stopping execution");
                break;
            }

            var instruction = Instructions[Ip];
            Ip++;

            // Console.WriteLine($"{new string('>', logIndentScope * 2)} [{Ip}] {instruction}");

            if (TryExecuteHandler(instruction)) {
                continue;
            }

            Logger.Error($"Unhandled instruction: {instruction?.GetType().Name.Split(".").Last()}");
        }

    }


    private void OnMarker(InstructionMarker inst) {
        if (inst.LogExecutions) {
            Console.WriteLine($"Hit marker(idx={inst.Index}): {inst.Name}");
        }
    }
    private void OnBlockMarkerEnd(InstructionBlockMarkerEnd     inst) { }
    private void OnBlockMarkerBegin(InstructionBlockMarkerBegin inst) { }

    private void OnHaltProgram(InstructionHaltProgram inst) {
        IsRunning = false;
        Console.WriteLine("Halting program");
    }
    private void OnJump(InstructionJump inst) {
        ChangeIp(inst.Operand.Index);
    }
    private void OnJumpIfFalse(InstructionJumpIfFalse inst) {
        if (!PopValue().AsBool())
            ChangeIp(inst.Operand.Index);
    }
    private void OnJumpIfTrue(InstructionJumpIfTrue inst) {
        if (PopValue().AsBool())
            ChangeIp(inst.Operand.Index);
    }
    private void OnStackPop(InstructionStackPop inst) {
        for (var i = 0; i < inst.Operand.PopCount; i++) {
            if (Stack.Count > 0)
                PopValue();
        }
    }

    private void OnBlockBegin(InstructionBlockBegin inst) {
        PushFrame();
    }
    private void OnBlockEnd(InstructionBlockEnd inst) {
        PopFrame();
    }

    private void OnCreateObject(InstructionCreateObject inst) {
        var objType = TypeTable.RegisterObjectType(
            TypeTable.GenerateUniqueTypeId(RTVT.Object),
            null
        );

        var obj = new Dictionary<string, RuntimeValue>();
        foreach (var _ in inst.Operand.Properties) {
            var value = PopValue();
            var key   = PopValue();

            objType.RegisterField(key.As<string>(), value.Type);

            obj[key.As<string>()] = value;
        }

        var rtValue = objType.Constructor(obj);

        PushValue(rtValue);
    }

    private void OnLoadProperty(InstructionLoadProperty inst) {
        var prop = PopValue();
        var obj  = PopValue();

        var field = obj.GetField(prop);

        PushValue(field);
    }
    private void OnStoreProperty(InstructionStoreProperty inst) {
        var value = PopValue();
        var prop  = PopValue();
        var obj   = PopValue();

        obj.SetField(prop, value);

        PushValue(obj);
    }
    private void OnLoadIndex(InstructionLoadIndex inst) {
        var index = PopValue();
        var obj   = PopValue();

        var field = obj.GetField(index);

        PushValue(field);
    }

    private void OnLoadConstant(InstructionLoadConstant instruction) {
        PushValue(instruction.Operand.Value);
    }
    private void OnLoadTemporaryInlineFunction(InstructionLoadTemporaryInlineFunction instruction) {
        var fnType = FunctionTable.InlineFunctionStack.Pop();
        PushValue(fnType.Constructor(fnType));
    }

    private void OnLoadVariable(InstructionLoadVariable instruction) {
        if (Symbols.TryGetValue(instruction.Operand.Name, out var value)) {
            PushValue(value);
            return;
        }

        Logger.Error($"Variable '{instruction.Operand.Name}' not found in symbol table");
    }

    private void OnStore(InstructionStore instruction) {
        Symbols[instruction.Operand.Name] = PopValue();
    }

    private void OnDeclareInlineFunction(InstructionDeclareInlineFunction inst) {
        var rtType = TypeTable.RegisterFunctionType(
            "",
            Ip,
            null
        );

        foreach (var param in inst.Operand.Parameters) {
            rtType.Parameters.Add(new RuntimeTypeInfo_Function.Parameter {
                Name = param.Name,
                Type = TypeTable.Get(param.Type)
            });
        }

        FunctionTable.InlineFunctionStack.Push(rtType);

        ChangeIp(inst.Operand.BodyEndIndex);
    }
    private void OnDeclareFunction(InstructionDeclareFunction inst) {
        var rtType = TypeTable.RegisterFunctionType(
            inst.Operand.Name,
            Ip,
            null
        );
        foreach (var param in inst.Operand.Parameters) {
            rtType.Parameters.Add(new RuntimeTypeInfo_Function.Parameter {
                Name = param.Name,
                Type = TypeTable.Get(param.Type)
            });
        }

        FunctionTable[inst.OperandString] = Ip;
        ChangeIp(inst.Operand.BodyEndIndex);
        FunctionTable.AddReturn(inst.OperandString, Ip);
    }

    private void OnCallNativeFunction(InstructionCallNativeFunction instruction) {
        var frame = new FunctionFrame {
            Ip      = Ip,
            NumArgs = instruction.Operand.NumArgs,
        };

        frame.Args = [..Stack.PopRange(frame.NumArgs)];

        CallStack.Push(frame);

        if (FunctionTable.TryGetNativeFunction(instruction.OperandString, out var nativeFunction)) {
            nativeFunction(this, frame);
        }

        CallStack.Pop();
    }

    private void OnCallFunction(InstructionCallFunction instruction) {
        var frame = new FunctionFrame {
            Ip      = Ip,
            NumArgs = instruction.Operand.NumArgs,
        };

        for (int i = 0; i < frame.NumArgs; i++) {
            var arg = PopValue();
            frame.Args.Add(arg);
        }

        RuntimeTypeInfo_Function type = null;
        if (Stack.Count > 0 && Stack.Peek() is RuntimeValue_Function fnValue) {
            type = fnValue.RuntimeType as RuntimeTypeInfo_Function;
        }

        for (var i = 0; i < type.Parameters.Count; i++) {
            var param = type.Parameters[i];
            Symbols[param.Name] = frame.Args[i];
        }

        PushFrame();

        CallStack.Push(frame);

        if (type != null) {
            ChangeIp(type.Index);
            return;
        }

        /*if (frame.Object != null) {
            if (Symbols.TryGetValue(instruction.OperandString, out var value)) {
                if (value is {Type: RTVT.Function}) {
                    ChangeIp(value.As<RuntimeTypeInfo_Function>().Index);
                    return;
                }
            }
        }*/

        ChangeIp(FunctionTable[instruction.OperandString]);
    }

    private void OnReturnStatement(InstructionReturnStatement inst) {
        RuntimeValue returnValue = null;
        if (!inst.Operand.IsVoid) {
            returnValue = PopValue();
        }

        PopFrame();

        ChangeIp(CallStack.Pop().Ip);

        if (!inst.Operand.IsVoid) {
            PushValue(returnValue);
        }
    }

    private void OnBinaryOperation(InstructionBinaryOperation inst) {
        var b = PopValue();
        var a = PopValue();

        PushValue(inst.Operand.Op switch {
            OperatorType.Plus               => a + b,
            OperatorType.Minus              => a - b,
            OperatorType.Multiply           => a * b,
            OperatorType.Divide             => a / b,
            OperatorType.Modulus            => a % b,
            OperatorType.Equals             => a.AreEqual(b),
            OperatorType.NotEquals          => a.AreEqual(b).Not(),
            OperatorType.GreaterThan        => a.GreaterThan(b),
            OperatorType.LessThan           => a.LessThan(b),
            OperatorType.GreaterThanOrEqual => a.GreaterThanOrEqual(b),
            OperatorType.LessThanOrEqual    => a.LessThanOrEqual(b),
            OperatorType.And                => a.And(b),
            _                               => throw new NotImplementedException($"Unhandled operator type: {inst.Operand.Op}")
        });
    }

    private void OnUnaryOperation(InstructionUnaryOperation inst) {
        switch (inst.Operand.Op) {
            /*case OperatorType.Increment: {
                using var mod = RuntimeTypeInfo_Number.Temporary(1);
                Symbols[inst.Operand.VarName] += mod;
                return;
            }
            case OperatorType.Decrement: {
                using var mod = RuntimeTypeInfo_Number.Temporary(1);
                Symbols[inst.Operand.VarName] -= mod;
                return;
            }
            case OperatorType.PlusEquals:
                Symbols[inst.Operand.VarName] += PopValue();
                return;
            case OperatorType.MinusEquals:
                Symbols[inst.Operand.VarName] -= PopValue();
                return;*/

            default:
                throw new NotImplementedException($"Unknown unary operation: {inst.Operand.Op}");
        }

    }

    private void OnLoopSetup(InstructionLoopSetup         inst) { }
    private void OnLoopCondition(InstructionLoopCondition inst) { }
    private void OnLoopIteration(InstructionLoopIteration inst) { }


    private void ChangeIp(int newIp, [CallerMemberName] string caller = "", [CallerLineNumber] int line = 0) {
        // var prevIp = Ip;
        Ip = newIp;
        // Console.WriteLine($"{new string('>', logIndentScope * 2)} [{caller}:{line}] Jumping to instruction: {prevIp} -> {Ip} -> {Instructions?[Ip]}");
    }
}