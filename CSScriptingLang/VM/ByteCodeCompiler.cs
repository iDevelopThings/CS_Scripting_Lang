using System.Runtime.CompilerServices;
using System.Text;
using CSScriptingLang.Lexing;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.RuntimeValues;
using CSScriptingLang.Utils;
using CSScriptingLang.Utils.CodeWriter;
using CSScriptingLang.VM.Instructions;
using CSScriptingLang.VM.Tables;

namespace CSScriptingLang.VM;

public class ByteCodeCompiler
{
    private bool _trackInstructionRegistration = false;

    public static ByteCodeCompiler Instance { get; private set; }

    public ErrorWriter ErrorWriter { get; set; }

    public List<Instruction> Instructions = new();

    public List<InstructionBlockData> Blocks = new();

    private CompileTimeFunctionTable FunctionTable = CompileTimeFunctionTable.Global;

    public ByteCodeCompiler(ProgramNode program, ErrorWriter errorWriter) {
        Instance    = this;
        ErrorWriter = errorWriter;

        CompilerInstructionBlock.Global = new CompilerInstructionBlock(this);

        if (program != null)
            Compile(program);
    }

    private T Push<T>(object[] args = null, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string method = "") where T : Instruction {
        var instruction = Activator.CreateInstance(typeof(T), args) as T;
        if (instruction == null) {
            throw new Exception("Failed to create instruction");
        }

        instruction.Index  = Instructions.Count;
        instruction.Caller = Caller.FromAttributes(file, line, method);

        Instructions.Add(instruction);

        if (_trackInstructionRegistration) {
            Console.WriteLine($">> {Instructions.Count - 1} {instruction}");
        }

        return instruction;
    }

    private CompilerInstructionBlock AddMarker(string name, bool logExecutions = false, params object[] extraContext) {
        Push<InstructionMarker>([name, logExecutions, extraContext]);
        return CompilerInstructionBlock.Child();
    }

    private UsingCallbackHandle AddScopedMarker(string name, bool logExecutions = false, params object[] extraContext) {
        Push<InstructionBlockMarkerBegin>([$"-- BEGIN {name}", logExecutions, extraContext]);

        return new UsingCallbackHandle(() => {
            Push<InstructionBlockMarkerEnd>([$"-- END {name}", logExecutions, extraContext]);
        });
    }

    public void Compile(ProgramNode program) {
        using var _  = ScopeTimer.NewWith("ByteCodeCompiler.Compile");
        using var __ = new CompilerInstructionBlock(this);

        foreach (var fnDecl in program.Functions) {
            FunctionTable.Register(fnDecl.Name, fnDecl);
        }

        Generate(program);

        FixInstructionIndexes();
    }

    private void FixInstructionIndexes() {
        for (var i = 0; i < Instructions.Count; i++) {
            var instruction = Instructions[i];
            if (instruction.Index != i) {
                Console.WriteLine($"Instruction index mismatch: {instruction.Index} != {i} {instruction}");
                instruction.Index = i;
            }
        }
    }

    private void Generate(BaseNode node) {
        switch (node) {

            case ProgramNode programNode:
                GenerateProgram(programNode);
                break;

            case FunctionDeclarationNode functionNode:
                GenerateFunctionDeclaration(functionNode);
                break;

            case InlineFunctionDeclarationNode inlineFunctionNode:
                GenerateInlineFunctionDeclaration(inlineFunctionNode);
                break;

            case FunctionCallNode functionCallNode:
                GenerateFunctionCall(functionCallNode);
                break;

            case BlockNode blockNode:
                GenerateBlock(blockNode);
                break;

            case ReturnStatementNode returnNode: {
                GenerateReturnStatement(returnNode);
                break;
            }

            case NumberNode numberNode:
                Push<InstructionLoadConstant>([RuntimeValue.Rent(numberNode.Value)]);
                break;
            case StringNode stringNode:
                Push<InstructionLoadConstant>([RuntimeValue.Rent(stringNode.Value)]);
                break;
            case ObjectLiteralNode objectNode:
                GenerateCreateObject(objectNode);
                break;
            case PropertyAccessNode propertyAccessNode:
                GeneratePropertyAccess(propertyAccessNode);
                break;
            case IndexAccessNode indexAccessNode:
                GenerateIndexAccess(indexAccessNode);
                break;


            case BinaryOperationNode binaryNode:
                GenerateBinaryNode(binaryNode);
                break;

            case VariableNode variableNode:
                Push<InstructionLoadVariable>([variableNode.Name]);
                break;

            case AssignmentNode assignmentNode: {
                var varToTest = assignmentNode.Variable ?? assignmentNode.Value;

                if (varToTest is PropertyAccessNode propertyAccess) {
                    using var _ = AddScopedMarker($"Assignment property access: {propertyAccess.Name}, Path={propertyAccess.GetPath()}", true);

                    if (assignmentNode.VariableName != null) {
                        GeneratePropertyAccess(propertyAccess);
                        Push<InstructionStore>([assignmentNode.VariableName]);
                        Push<InstructionLoadVariable>([assignmentNode.VariableName]);
                    } else {
                        Generate(propertyAccess.Object);
                        Push<InstructionLoadConstant>([RuntimeValue.Rent(propertyAccess.Name)]);
                        Generate(assignmentNode.Value);
                        Push<InstructionStoreProperty>();
                    }

                    break;
                }

                if (varToTest is IndexAccessNode indexAccess) {
                    using var _ = AddScopedMarker($"Assignment index access: {indexAccess.Index}, Path={indexAccess.GetPath()}", true);

                    if (assignmentNode.VariableName != null) {
                        Generate(indexAccess.Object);
                        Push<InstructionStore>([assignmentNode.VariableName]);
                    } else {
                        Generate(indexAccess.Object);
                        Generate(indexAccess.Index);
                        Generate(assignmentNode.Value);
                        Push<InstructionStoreProperty>();
                    }

                    break;
                }

                if (varToTest is FunctionCallNode fnCall) {
                    using var _ = AddScopedMarker($"Assignment function call: {fnCall.Name}");

                    if (assignmentNode.VariableName != null) {
                        fnCall.VariableName ??= fnCall.Name;

                        GenerateFunctionCall(fnCall);
                        Push<InstructionStore>([assignmentNode.VariableName]);
                    } else {
                        GenerateFunctionCall(fnCall);
                    }

                    break;
                }

                Generate(assignmentNode.Value);
                Push<InstructionStore>([assignmentNode.VariableName]);
                break;
            }

            case VariableDeclarationNode declarationNode: {
                Generate(declarationNode.Assignment);
                break;
            }

            case ForLoopNode forLoopNode:
                GenerateForLoop(forLoopNode);
                break;

            case IfStatementNode ifNode:
                GenerateIfStatement(ifNode);
                break;

            default:
                throw new Exception($"Unknown AST node type: {node.GetType()} {node}");
        }
    }


    private void GenerateCreateObject(ObjectLiteralNode node) {
        using var _ = AddScopedMarker("Object Literal");

        var props = new List<string>();
        foreach (var property in node.Properties) {
            props.Add(property.Name);
            Push<InstructionLoadConstant>([RuntimeValue.Rent(property.Name)]);
            if (property.Value is InlineFunctionDeclarationNode inlineFn) {
                GenerateInlineFunctionDeclaration(inlineFn);
                Push<InstructionLoadTemporaryInlineFunction>();
            } else {
                Generate(property.Value);
            }
        }

        Push<InstructionCreateObject>([props]);
    }
    private void GeneratePropertyAccess(PropertyAccessNode node) {
        using var _ = AddScopedMarker($"Property Access {node.Name} -> {node.GetPath()}");

        Generate(node.Object);
        Push<InstructionLoadConstant>([RuntimeValue.Rent(node.Name)]);
        Push<InstructionLoadProperty>();
    }
    private void GenerateIndexAccess(IndexAccessNode node) {
        using var _ = AddScopedMarker("Index Access");

        Generate(node.Object);
        Generate(node.Index);
        Push<InstructionLoadIndex>();
    }

    private void GenerateProgram(ProgramNode node) {
        node.Statements.ForEach(Generate);
        Push<InstructionHaltProgram>();
    }

    private void GenerateBlock(BlockNode node) {
        using var _ = AddScopedMarker("Block");

        Push<InstructionBlockBegin>();
        node.Statements.ForEach(Generate);
        Push<InstructionBlockEnd>();
    }

    private void GenerateReturnStatement(ReturnStatementNode node) {
        if (node.ReturnValue != null) {
            Generate(node.ReturnValue);
        }

        Push<InstructionReturnStatement>([node.ReturnValue != null]);
    }

    private void GenerateFunctionDeclaration(FunctionDeclarationNode node) {
        using var _ = AddScopedMarker($"Function Declaration: {node.Name}", true);

        if (!FunctionTable.TryGetFunction(node.Name, out var fnDecl)) {
            FunctionTable.Register(node.Name, node);
        }

        FunctionTable = FunctionTable.AddChild();

        var instruction = Push<InstructionDeclareFunction>([node.Name]);

        node.Parameters.Arguments.ForEach(arg => {
            instruction.Operand.AddParameter(arg.Name, arg.Type);
        });

        instruction.Operand.BodyStartIndex = Instructions.Count;

        node.Parameters.Arguments.ForEach(arg => {
            // Push<InstructionLoadVariable>([arg.Name]);
        });

        node.Body.Statements.ForEach(Generate);

        if (!node.HasReturnStatementDefined) {
            Push<InstructionReturnStatement>([true]);
        }

        instruction.Operand.BodyEndIndex = Instructions.Count;

        FunctionTable = FunctionTable.Parent;

        /*
        Push(OpCode.BeginFunction, node.Name);
        Push(OpCode.BeginBlock, $"FnBodyBlock: {node.Name}");
        node.Parameters.Arguments.ForEach(arg => {
            Push<InstructionLoadVariable>([arg.Name]);
        });

        node.Body.Statements.ForEach(Generate);

        Push(OpCode.EndBlock, $"FnBodyBlock: {node.Name}");

        Push(OpCode.Return, $"FnReturn: {node.Name}");
        Push(OpCode.EndFunction, $"FnEnd: {node.Name}");
        */
    }
    private void GenerateInlineFunctionDeclaration(InlineFunctionDeclarationNode node) {
        using var _ = AddScopedMarker($"Inline Function Declaration: {node}", true);

        FunctionTable = FunctionTable.AddChild();

        var instruction = Push<InstructionDeclareInlineFunction>();

        node.Parameters.Arguments.ForEach(arg => {
            instruction.Operand.AddParameter(arg.Name, arg.Type);

        });

        instruction.Operand.BodyStartIndex = Instructions.Count;

        node.Parameters.Arguments.ForEach(arg => {
            // Push<InstructionLoadVariable>([arg.Name]);
            // Push<InstructionStore>([arg.Name]);
            // Push<InstructionLoadVariable>([arg.Name]);
        });

        node.Body.Statements.ForEach(Generate);

        if (!node.HasReturnStatementDefined) {
            Push<InstructionReturnStatement>([true]);
        }

        instruction.Operand.BodyEndIndex = Instructions.Count;

        FunctionTable = FunctionTable.Parent;
    }

    private void GenerateFunctionCall(FunctionCallNode node) {
        using var _ = AddScopedMarker($"Function Call: {node.Name}", true);

        FunctionDeclarationNode fnDecl = null;

        if (node.Name != null && Tables.FunctionTable.TryGetNativeFunction(node.Name, out var nativeFn)) {
            // InstructionCallNativeFunction
            foreach (var arg in node.Arguments.ExpressionNodes) {
                Generate(arg);
            }

            var nativeInstruction = Push<InstructionCallNativeFunction>([node.Name]);
            nativeInstruction.Operand.NumArgs = node.Arguments.ExpressionNodes.Count;

            return;
        }

        if (node.VariableName == null && node.Name != null) {
            if (!FunctionTable.TryGetFunction(node.Name, out fnDecl)) {
                // ErrorWriter.LogErrorWithCaller($"Function '{node.Name}' not found", node.StartToken.Range, node.EndToken.Range);
                // return;
            }
        }

        if (node.Variable != null) {
            // if (node.Variable == null) {
            // Push<InstructionLoadVariable>([node.VariableName]);
            // } else {
            Generate(node.Variable);
            // }
        }

        using (var __ = AddScopedMarker($"Function Call Args: {node.Name}")) {
            for (var i = 0; i < node.Arguments.ExpressionNodes.Count; i++) {
                var arg = node.Arguments.ExpressionNodes[i];

                Generate(arg);

                // Push<InstructionLoadFunctionParameter>([i]);

                // We don't have declaration for native fns
                // if (fnDecl != null) {
                //     Push<InstructionStore>([fnDecl.Parameters.Arguments[i].Name]);
                //     Push<InstructionLoadVariable>([fnDecl.Parameters.Arguments[i].Name]);
                // }
            }
        }

        var instruction = Push<InstructionCallFunction>([node.Name]);
        instruction.Operand.NumArgs = node.Arguments.ExpressionNodes.Count;

        /*
        Push(OpCode.ArgsExpressionList, node.Arguments.ExpressionNodes.Count);

        for (var i = 0; i < node.Arguments.ExpressionNodes.Count; i++) {
            var arg = node.Arguments.ExpressionNodes[i];

            Generate(arg);

            // We don't have declaration for native fns
            if (!fnDecl.IsNative) {
                Push<InstructionStore>([fnDecl.Parameters.Arguments[i].Name]);
                Push<InstructionLoadVariable>([fnDecl.Parameters.Arguments[i].Name]);
            }
        }

        Push(OpCode.Call, node.Name);
        */

    }

    private void GenerateIfStatement(IfStatementNode node) {
        using var _ = AddScopedMarker("If Statement");

        Generate(node.Condition);

        var jmp = Push<InstructionJumpIfFalse>([0]);

        // Push(OpCode.JumpIfFalse, 0); // To be replaced with actual jump target

        Generate(node.ThenBranch);

        jmp.Operand.Index = Instructions.Count;

        if (node.ElseBranch != null) {
            var elseJmp = Push<InstructionJumpIfFalse>([0]);

            Generate(node.ElseBranch);

            elseJmp.Operand.Index = Instructions.Count;
        }
    }

    private void GenerateBinaryNode(BinaryOperationNode node) {
        // using var _ = AddMarker("Binary Operation");

        Generate(node.Left);
        Generate(node.Right);


        switch (node.Operator) {

            case OperatorType.MinusEquals:
            case OperatorType.PlusEquals: {
                var variable = node.Left as VariableNode;
                if (variable == null) {
                    throw new Exception("Left side of assignment must be a variable");
                }

                Push<InstructionBinaryOperation>([node.Operator]);
                Push<InstructionStore>([variable.Name]);
                break;
            }

            default:
                Push<InstructionBinaryOperation>([node.Operator]);
                break;
        }
    }

    private void GenerateForLoop(ForLoopNode node) {
        using var _ = AddScopedMarker("For Loop");

        // For loop structure:
        // for (initialization; condition; iteration) { body }

        // 1. Initialization
        Generate(node.Initialization);

        Push<InstructionLoopSetup>();

        // 2. Condition check - remember the start of the loop
        var loopStart = Instructions.Count;
        Generate(node.Condition);
        Push<InstructionLoopCondition>();

        // Placeholder for jump instruction after loop body
        var jmp = Push<InstructionJumpIfFalse>([0]);

        // 3. Loop body
        Generate(node.Body);

        // 4. Iteration step
        Generate(node.Increment);
        Push<InstructionLoopIteration>();

        // Unconditional jump back to the start of the loop condition check
        Push<InstructionJump>([loopStart]);

        // 5. Update the conditional jump to exit the loop
        jmp.Operand.Index = Instructions.Count;
    }

    public void Dump() {

        var s = new CodeWriterSettings(CodeWriterSettings.CSharpDefault) {
            NewLineBeforeBlockBegin = true,
            Indent                  = "    ",
            TranslationMapping = {
                ["`"] = "\""
            }
        };
        var w      = new Writer(s);
        var blocks = CompilerInstructionBlock.Global;

        var usingStack       = new Stack<UsingHandle>();
        var markerBlockDepth = 0;

        void writeInstruction(Instruction instruction) {
            var n               = instruction.GetType().Name.Replace("Instruction", "");
            var operandTypeName = (instruction.UntypedOperand?.GetType().Name ?? "").Replace("Operand", "");

            var sb = new StringBuilder();
            sb.Append($"[{instruction.Index}] ");

            if (instruction is InstructionMarker marker) {
                sb.Append($"M: {marker.Name}");
            } else {
                sb.Append(n);

                if (instruction.OperandString != null && instruction.OperandString.Length > 0) {
                    sb.Append($" -> {instruction.OperandString}");
                }

                if (operandTypeName.Length > 0) {
                    sb.Append($" -> {operandTypeName}");
                }
            }

            w._(sb.ToString());
        }


        foreach (var instruction in Instructions) {
            switch (instruction) {
                case InstructionBlockMarkerBegin marker:
                    markerBlockDepth++;
                    // w.Write("");
                    usingStack.Push(w.b($"{marker.Name.Replace("-- ", $"-- [{instruction.Index}] ")}"));
                    continue;
                case InstructionBlockMarkerEnd marker:
                    usingStack.Pop().Dispose();
                    w._(marker.Name.Replace("-- ", $"-- [{instruction.Index}] "));
                    // w.Write("");
                    markerBlockDepth--;
                    continue;

                case InstructionBlockBegin:
                    usingStack.Push(w.b($"[{instruction.Index}] Block Begin"));
                    continue;
                case InstructionBlockEnd:
                    usingStack.Pop().Dispose();
                    w._($"[{instruction.Index}] Block End");
                    continue;

            }

            writeInstruction(instruction);
        }


        /*void writeInstructionBlock(CompilerInstructionBlock block) {


            using (w.b( /*$"Block[{block.StartInstructionIndex}->{block.ChildrenStartingIndex}->{block.ChildrenEndingIndex}->{block.EndInstructionIndex}]"#1#)) {
                foreach (var instruction in block.Instructions) {

                    if (block.Children.Count > 0 && instruction.Index == block.ChildrenStartingIndex) {
                        break;
                    }

                    writeInstruction(instruction);
                }

                foreach (var child in block.Children) {
                    writeInstructionBlock(child);
                }

                if (block.Children.Count > 0) {
                    for (var i = block.ChildrenEndingIndex; i < block.EndInstructionIndex; i++) {
                        writeInstruction(Instructions[i]);
                    }
                }
            }
        }
        writeInstructionBlock(blocks);*/

        Console.Write(w.ToString());

    }
}