using CSScriptingLang.Core.FileSystem;
using CSScriptingLang.Interpreter.Bindings;
using CSScriptingLang.Interpreter.Modules;
using CSScriptingLang.Lexing;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.RuntimeValues;
using CSScriptingLang.Utils;
using CSScriptingLang.RuntimeValues.Types;

namespace CSScriptingLang.Interpreter;

public partial class Interpreter
{
    public ProgramNode Program => Module?.Program;

    static Interpreter() {
        ClassScopedTimer<Interpreter>.SetColorFn(n => n.BoldBrightBlue());
        ClassScopedTimer<Interpreter>.SetName("Interpreter Timer");
    }
    public Interpreter(FileSystem fs = null) {
        FileSystem = fs;
        PushFrame(null, true);
    }

    public void LoadModule(string name, ProgramNode program) {
        Module = ModuleRegistry.RegisterModule(name, program);
    }
    public void Load(ProgramNode program) => LoadModule("main", program);

    private void CompileModules() {
        using var _ = ClassScopedTimer<Interpreter>.NewWith($"CompileModules({ModuleRegistry.NumModules})");

        Module?.Compile();

        var processedModuleNames = new HashSet<string>();
        var modulesToProcess     = new Queue<string>();

        if (Module != null) {
            modulesToProcess.Enqueue(Module.Name);
        }

        bool LoadModuleImports(Module module) {
            if (module.Program == null) {
                return false;
            }

            if (processedModuleNames.Contains(module.Name))
                return false;

            foreach (var import in module.Program.Cursor.All.Of<ImportStatementNode>()) {
                if (processedModuleNames.Contains(import.Path.Value))
                    continue;
                modulesToProcess.Enqueue(import.Path.Value);
            }

            return true;
        }

        void ProcessModulesQueue() {
            while (modulesToProcess.Count > 0) {
                var moduleName = modulesToProcess.Dequeue();

                Module mod = null;
                if (!ModuleRegistry.TryGetModule(moduleName, out mod)) {
                    mod = ModuleRegistry.LoadModule(moduleName);
                }

                if (mod == null) {
                    Logger.Fatal($"Module '{moduleName}' not found");
                    continue;
                }

                if (mod.Program == null && mod.Compiled == false) {
                    mod.Compile();
                }

                if (!LoadModuleImports(mod))
                    continue;

                processedModuleNames.Add(moduleName);

                if (!mod.Compiled)
                    mod.Compile();
            }
        }

        ProcessModulesQueue();


        foreach (var module in ModuleRegistry.GetModules()) {
            if (module == Module)
                continue;

            if (processedModuleNames.Contains(module.Name))
                continue;

            modulesToProcess.Enqueue(module.Name);

            ProcessModulesQueue();
        }

    }


    private void LoadModules() {
        using var _ = ClassScopedTimer<Interpreter>.NewWith("Load Module Declarations".Bold());

        foreach (var module in ModuleRegistry.GetModules()) {
            foreach (var decl in module.Declarations) {
                switch (decl) {
                    case FunctionDeclarationNode fn:
                        Symbols.DeclareFunction(fn);
                        break;
                    case VariableDeclarationNode var:
                        Execute(var);
                        break;
                }
            }
        }
    }

    public void Execute() {
        using var _ = ClassScopedTimer<Interpreter>.NewWith("Execute".Bold());
        LoadModules();
        Execute(Program);
    }
    public void Execute(Module module) {
        using (ClassScopedTimer<Interpreter>.NewWith("Execution Preparation".Bold())) {
            Module = module;

            FunctionBinder.BindNativeFunctions(this);

            CompileModules();
            LoadModules();
        }

        using (ClassScopedTimer<Interpreter>.NewWith("Execution".Bold())) {
            Execute(Program);
        }
    }


    public RuntimeValue ExecuteGetValue(BaseNode node) {
        var result = Execute(node);
        if (result.TryPopSymbolOrRTValue(out var rtValue)) {
            return rtValue;
        }

        return null;
    }
    public bool ExecuteGetValueAndSymbol(BaseNode node, out Symbol symbol, out RuntimeValue rtValue) {
        var result = Execute(node);
        if (result.ValuesPushed > 0) {
            if (TryPopSymbolAndRTValue(out rtValue, out symbol)) {
                return true;
            }
        }

        symbol  = null;
        rtValue = null;
        return false;
    }

    public (Symbol, RuntimeValue) ExecuteGetValueAndSymbol(BaseNode node) {
        var result = Execute(node);
        if (result.ValuesPushed > 0) {
            if (TryPopSymbolAndRTValue(out var symbol, out var rtValue)) {
                return (rtValue, symbol);
            }
        }

        return (null, null);
    }

    public ExecResult Execute(BaseNode node) {
        switch (node) {
            case VariableDeclarationNode n:
                return Execute(n);
            case ArgumentDeclarationNode n:
                return Execute(n);
            case ArgumentListDeclarationNode n:
                return Execute(n);
            case FunctionDeclarationNode n:
                return Execute(n);
            case InlineFunctionDeclarationNode n:
                return Execute(n);
            case BooleanNode n:
                return Execute(n);
            case LiteralNumberNode n:
                return Execute(n);
            case NumberNode n:
                return Execute(n);
            case StringNode n:
                return Execute(n);
            case ExpressionListNode n:
                return Execute(n);
            case VariableNode n:
                return Execute(n);
            case BinaryOperationNode n:
                return Execute(n);
            case UnaryOperationNode n:
                return Execute(n);
            case ObjectProperty n:
                return Execute(n);
            case ObjectLiteralNode n:
                return Execute(n);
            case PropertyAccessNode n:
                return Execute(n);
            case ArrayLiteralNode n:
                return Execute(n);
            case IndexAccessNode n:
                return Execute(n);
            case ProgramNode n:
                return Execute(n);
            case BlockNode n:
                return Execute(n);
            case AssignmentNode n:
                return Execute(n);
            case DeferStatementNode n:
                return Execute(n);
            case FunctionCallNode n:
                return Execute(n);
            case IfStatementNode n:
                return Execute(n);
            case ForRangeNode n:
                return Execute(n);
            case ForLoopNode n:
                return Execute(n);
            case RangeNode n:
                return Execute(n);
            case ReturnStatementNode n:
                return Execute(n);
            case NodeList<ArgumentDeclarationNode> n:
                return Execute(n);
            case NodeList<IExpressionNode> n:
                return Execute(n);
            case NodeList<BaseNode> n:
                return Execute(n);
            default:
                throw new NotImplementedException($"Unhandled node type: {node.GetType().Name}");
        }
    }

    private ExecResult Execute(ProgramNode node) {
        var result = NewResult();

        foreach (var n in node.Nodes) {
            Execute(n);
        }

        return result;
    }

    private ExecResult Execute(InlineFunctionDeclarationNode node) {
        var result = NewResult();

        if (node.Cursor.First.Parent<ObjectLiteralNode>(out var objNode)) {
            if (objNode.ObjectType == null)
                throw new Exception("Object type not set for inline function declaration");

            var fnValue = RuntimeValue.Rent<RuntimeValue_Function>(node);

            // var fnValue = StaticTypes.Function.Constructor(node);

            PushValue(fnValue);
            result.ValuesPushed++;

            // var objType = objNode.ObjectType;
            // objType.RegisterField(node.ParentAs<ObjectProperty>().Name, RTVT.Function);

            // PushValue(TypeTable.RegisterFunctionType())
            // result.ValuesPushed++;

        }

        return result;
    }
    private ExecResult Execute(FunctionDeclarationNode node) {
        var result = NewResult();
        // Should not be entered during execution
        // throw new NotImplementedException();
        return result;
    }

    private ExecResult Execute(FunctionCallNode node) {
        var result = NewResult();

        InlineFunctionDeclarationNode declaration = null;
        RuntimeValue_Object           obj         = null;

        if (node.Variable != null) {
            if (node.Variable is InlineFunctionDeclarationNode inlineFn) {
                declaration = inlineFn;
            } else {
                var variable = ExecuteGetValue(node.Variable);
                if (variable == null) {
                    Logger.Fatal($"Failed to get value from variable node");
                    return result;
                }

                if (variable is RuntimeValue_Function fn) {
                    declaration = fn.Value as InlineFunctionDeclarationNode;
                    obj         = fn.Object;
                }
            }
        } else {
            if (!Symbols.GetFunctionDeclaration(node.Name, out var function)) {

                if (!Symbols.Get(node.Name, out var symbol)) {
                    Logger.Fatal($"Function '{node.Name}' not found");
                    return result;
                }

                if (symbol.Value is RuntimeValue_Function fn) {
                    declaration = fn.Value as InlineFunctionDeclarationNode;
                    obj         = fn.Object;
                } else {
                    Logger.Fatal($"Symbol '{node.Name}' is not a function");
                    return result;
                }

            } else {
                declaration = function;
            }
        }

        if (declaration == null) {
            Logger.Fatal($"Function '{node.Name}' not found");
            return result;
        }


        using var scope = PushFrame(declaration);
        using var frame = PushFunctionFrame(declaration);

        var argsCount = declaration?.Parameters.GetValidArgumentCount(node.Arguments.ExpressionNodes.Count) ?? 0;

        if (obj != null) {
            if (declaration.IsNative) {
                frame.Args.Add(new Symbol("this", obj));
            } else {
                var thisSymbol = Symbols.Set("this", obj);
                frame.Args.Add(thisSymbol);
            }
        }

        Symbol varArgsParamSymbol       = null;
        var    addedVarArgSymbolToFrame = false;
        for (int i = 0; i < argsCount; i++) {

            var arg = node.Arguments.ExpressionNodes[i];

            if (!declaration.Parameters.Get(i, out var argDef)) {
                throw new Exception("Argument definition not found");
            }

            if (argDef.IsVarArgs && varArgsParamSymbol == null) {
                varArgsParamSymbol = Symbols.Set(argDef.Name, RuntimeValue.Rent(new List<RuntimeValue>()));
            }

            if (!ExecuteGetValueAndSymbol(arg, out var argSymbol, out var argValue)) {
                Logger.Fatal($"Failed to get value from argument node");
                continue;
            }


            var symbol = argSymbol;
            if (symbol == null)
                symbol = argValue.Symbol;

            if (symbol != null && !argDef.IsVarArgs) {
                symbol = Symbols.AddToScope(argDef.Name, argSymbol);
            }

            if (symbol == null && !argDef.IsVarArgs) {
                symbol = Symbols.AddToScope(argDef.Name, argValue);
            }

            if (symbol == null && argDef.IsVarArgs) {
                symbol = varArgsParamSymbol;
            }


            if (argDef.IsVarArgs) {
                if (!addedVarArgSymbolToFrame) {
                    frame.Args.Add(varArgsParamSymbol);
                    addedVarArgSymbolToFrame = true;
                }

                varArgsParamSymbol?.Value.As<List<RuntimeValue>>().Add(argValue);
            } else {
                frame.Args.Add(symbol);
            }

            /*var argContext = Execute(arg);
            if (argContext.TryPopSymbolOrRTValue(out var rtValue)) {

                if (declaration.IsNative) {
                    frame.Args.Add(new Symbol("", rtValue));
                    continue;
                }

                var symbol = Symbols.Set(argDef.Name, rtValue);
                frame.Args.Add(symbol);
            }*/
        }

        if (declaration.IsNative) {
            declaration.NativeFunction(this, frame);
            return result;
        }

        Execute(declaration.Body);

        if (CurrentFrame.HasReturned) {
            TryPopValue<RuntimeValue>(out var rtValue);
            PushValue(rtValue);

            result.ValuesPushed++;

        } else {
            throw new Exception("Function did not return a value");
        }

        // Logger.Info($"Function Call: {node.Name}, Returned? {CurrentFrame.HasReturned}, Value: {CurrentFrame.ReturnValue}");

        return result;
    }

    private ExecResult Execute(DeferStatementNode node) {
        var result = NewResult();

        result += Execute(node.Expression);

        return result;
    }
    private ExecResult Execute(ReturnStatementNode node) {
        var result = NewResult();

        CurrentFrame.HasReturned = true;
        CurrentFrame.ReturnBlock = CurrentFrame.CurrentBlock;

        RuntimeValue rtValue = null;

        if (node.ReturnValue != null) {
            Execute(node.ReturnValue);
            TryPopValue<RuntimeValue>(out rtValue);
        } else {
            rtValue = RuntimeValue.Rent([null]);
        }

        CurrentFrame.ReturnValue = rtValue;
        PushValue(rtValue);
        result.ValuesPushed++;

        return result;
    }

    private ExecResult Execute(VariableDeclarationNode node) {
        var result = NewResult();
        var symbol = Symbols.Define(node.VariableName);
        Execute(node.Assignment);
        return result;
    }

    private ExecResult Execute(AssignmentNode node) {
        var result = NewResult();

        Symbol       symbol  = null;
        RuntimeValue rtValue = null;
        if (node.Value != null && !ExecuteGetValueAndSymbol(node.Value, out symbol, out rtValue)) {
            Logger.Fatal($"Failed to get value from assignment node");
            return result;
        }

        if (node.Variable != null) {
            var variable = ExecuteGetValue(node.Variable);
            if (variable == null) {
                Logger.Fatal($"Failed to get value from variable node");
                return result;
            }

            variable.Value = rtValue;

            return result;

        }

        if (node.VariableName != null) {
            var varSymbol = Symbols.Get(node.VariableName);
            varSymbol.Value = rtValue;
            return result;
        }

        return result;
    }

    private ExecResult Execute(VariableNode node) {
        var result = NewResult();

        if (Symbols.Get(node.Name, out var symbol)) {
            PushValue(symbol.Value);
            result.ValuesPushed++;

            return result;
        }

        if (ModuleRegistry.TryGetModule(node.Name, out var module)) {
            result.Values.Add(module);
            return result;
        }

        return result;
    }

    private ExecResult Execute(PropertyAccessNode node) {
        var result = NewResult();

        var objResult = Execute(node.Object);
        if (objResult.TryPopSymbolOrRTValue(out var obj)) {
            var property = obj.GetField(node.Name);
            if (property != null) {
                PushValue(property);
                result.ValuesPushed++;
                return result;
            }
        }

        if (objResult.TryGet<Module>(out var module)) {
            if (node.Parent is FunctionCallNode) {
                if (module.GetFunction(node.Name, out var function)) {
                    if (Symbols.GetFunctionDeclaration(node.Name, out var rtFunction)) {
                        if (function == rtFunction) {
                            PushValue(RuntimeValue.Rent<RuntimeValue_Function>(rtFunction));
                            result.ValuesPushed++;
                            return result;
                        }
                    }
                }
            } else {
                if (module.GetVariable(node.Name, out var variable)) {
                    if (Symbols.Get(node.Name, out var symbol)) {
                        PushValue(symbol.Value);
                        result.ValuesPushed++;
                        return result;
                    }
                }
            }
        }

        Logger.Fatal($"Failed to get value from property access node (path='{node.GetPath()}')");
        return result;
    }
    private ExecResult Execute(IndexAccessNode node) {
        var result = NewResult();

        var objResult = Execute(node.Object);
        if (!objResult.TryPopSymbolOrRTValue(out var obj)) {
            Logger.Fatal($"Failed to get value from object node");
            return result;
        }

        var indexResult = Execute(node.Index);
        if (!indexResult.TryPopSymbolOrRTValue(out var index)) {
            Logger.Fatal($"Failed to get value from index node");
            return result;
        }

        var property = obj.GetField(index);
        if (property != null) {
            PushValue(property);
            result.ValuesPushed++;
        }

        return result;
    }

    private ExecResult ExecuteLiteral(LiteralValueNode node) {
        var result = NewResult();
        PushValue(RuntimeValue.Rent(node.UntypedValue));
        result.ValuesPushed++;
        return result;
    }
    private ExecResult Execute(LiteralNumberNode node) => ExecuteLiteral(node);
    private ExecResult Execute(NumberNode        node) => ExecuteLiteral(node);
    private ExecResult Execute(BooleanNode       node) => ExecuteLiteral(node);
    private ExecResult Execute(StringNode        node) => ExecuteLiteral(node);

    private ExecResult Execute(ObjectLiteralNode node) {
        var result = NewResult();

        node.ObjectType ??= TypeTable.RegisterObjectType(
            "",
            null
        );
        RuntimeValue_Object rtObj = node.ObjectType.Constructor();

        // var obj = new Dictionary<string, RuntimeValue>();
        foreach (var property in node.Properties) {
            var rtValue = ExecuteGetValue(property.Value);
            if (rtValue == null) {
                Logger.Fatal($"Failed to get value from object property");
                continue;
            }

            rtObj.SetField(property.Name, rtValue);

            // node.ObjectType.RegisterField(property.Name, rtValue.Type);

            // obj[property.Name] = rtValue;
        }


        PushValue(rtObj);
        result.ValuesPushed++;

        return result;
    }

    public ExecResult Execute(ArrayLiteralNode node) {
        var result = NewResult();

        var rtArray = RuntimeValue.Rent<List<RuntimeValue>, RuntimeValue_Array>();
        foreach (var element in node.Elements) {
            var value = Execute(element);
            if (!value.TryPopSymbolOrRTValue(out var rtValue)) {
                Logger.Fatal($"Failed to get value from array element");
                continue;
            }

            rtArray.Add(rtValue);
        }

        PushValue(rtArray);
        result.ValuesPushed++;

        return result;
    }

    private ExecResult Execute(BinaryOperationNode node) {
        var result = NewResult();

        var left  = ExecuteGetValue(node.Left);
        var right = ExecuteGetValue(node.Right);

        switch (node.Operator) {

            case OperatorType.MinusEquals:
            case OperatorType.PlusEquals: {
                var variable = node.Left as VariableNode;
                if (variable == null) {
                    throw new Exception("Left side of assignment must be a variable");
                }

                result += ExecuteBinaryOperation(node.Operator, left, right);

                TryPopValue<RuntimeValue>(out var rtResult);
                Symbols[variable.Name].Value = rtResult;
                break;
            }

            default:
                result += ExecuteBinaryOperation(node.Operator, left, right);
                break;
        }

        return result;
    }
    private ExecResult ExecuteBinaryOperation(OperatorType op, RuntimeValue a, RuntimeValue b) {
        var result = NewResult();

        var rtValue = RuntimeValue.Operation(a, b, op);

        PushValue(rtValue);

        result.ValuesPushed++;
        return result;
    }

    private ExecResult Execute(UnaryOperationNode node) {
        var result = NewResult();

        switch (node.Operator) {
            case OperatorType.Increment:
            case OperatorType.Decrement: {
                var rtValue = ExecuteGetValue(node.Operand);
                ExecuteUnaryOperation(node.Operator, rtValue);

                PushValue(rtValue);

                result.ValuesPushed++;
                return result;
            }
            default:
                throw new NotImplementedException($"Unhandled operator type: {node.Operator}");
        }
    }

    private void ExecuteUnaryOperation(OperatorType op, RuntimeValue a) {
        using var mod       = RuntimeTypeInfoNumberBase.Temporary(1);
        using var modCasted = mod.ImplicitCast(a.Type);

        var newValue = RuntimeValue.Operation(a, modCasted, op);
        if (newValue != a) {
            // Ensure that we carry over the symbol reference
            newValue.SetSymbol(a.Symbol);
            a.Symbol.Value = newValue;
        }
    }

    private ExecResult Execute(IfStatementNode node) {
        var result = NewResult();

        Execute(node.Condition);
        TryPopSymbolOrRTValue(out var condition);

        if (condition.AsBool()) {
            Execute(node.ThenBranch);
        } else {
            if (node.ElseBranch != null)
                Execute(node.ElseBranch);
        }

        return result;
    }

    private ExecResult Execute(ForLoopNode node) {
        var result = NewResult();

        using var _ = PushFrame();

        Execute(node.Initialization);

        while (true) {
            Execute(node.Condition);
            TryPopSymbolOrRTValue(out var condition);

            /*RuntimeValue condition = null;
            if (node.IsRangeBased) {
                var rangeMin = ExecuteGetValue(node.Range);
                var rangeMax = node.Range.Expression;


            } */

            if (!condition!.AsBool())
                break;

            Execute(node.Body);
            Execute(node.Increment);
        }

        return result;
    }

    private ExecResult Execute(ForRangeNode node) {
        var result = NewResult();

        using var _ = PushFrame();

        var rangeResult = Execute(node.Range);
        rangeResult.TryPopSymbolOrRTValue(out var range);

        var rangeMin   = (RuntimeValue) rangeResult[0];
        var rangeValue = (RuntimeValue) rangeResult[1];

        var                rangeMax   = 0;
        RuntimeValue_Array rangeArray = null;

        if (rangeMin == null || rangeValue == null) {
            Logger.Fatal("Failed to get range values");
            return result;
        }

        if (node.Indexers?[0] is not VariableDeclarationNode loopIndexerDecl) {
            Logger.Fatal("Invalid loop indexer declaration");
            return result;
        }

        VariableDeclarationNode loopElementDecl = null;
        if (node.Indexers.Nodes.Count > 1) {
            if (node.Indexers[1] is not VariableDeclarationNode elementDecl) {
                Logger.Fatal("Invalid loop element declaration");
                return result;
            }

            loopElementDecl = elementDecl;
        }


        if (rangeValue is RuntimeValue_Array rangeAsArr) {
            rangeArray = rangeAsArr;
            rangeMax   = rangeArray.Length - 1;
        } else if (rangeValue is RuntimeValue {IsNumber: true} rangeNum) {
            rangeMax = (int) rangeNum.Value;
        } else {
            Logger.Fatal("Invalid range value");
            return result;
        }

        var rangeMaxValue = RuntimeValue.Rent((double) rangeMax);

        var loopIndexVar = Symbols.Define(loopIndexerDecl.VariableName);
        loopIndexVar.Value = RuntimeValue.Rent((double) rangeMin.Value);

        Symbol loopElementVar = null;
        if (loopElementDecl != null) {
            loopElementVar       = Symbols.Define(loopElementDecl.VariableName);
            loopElementVar.Value = rangeArray?[0];
        }

        while (true) {

            var binOpResult = ExecuteBinaryOperation(OperatorType.LessThan, loopIndexVar.Value, rangeMaxValue);
            TryPopSymbolOrRTValue(out var condition);

            if (!condition!.AsBool())
                break;

            Execute(node.Body);

            ExecuteUnaryOperation(OperatorType.Increment, loopIndexVar.Value);

            if (loopElementVar != null && rangeArray != null) {
                var element = rangeArray[loopIndexVar.Value.As<int>()];
                loopElementVar.Value = element;
            }
        }

        return result;
    }

    private ExecResult Execute(RangeNode node) {
        var result = NewResult();

        var min = RuntimeValue.Rent((double) 0);
        var max = ExecuteGetValue(node.Expression);

        result[0] = min;
        result[1] = max;

        PushValue(min);
        result.ValuesPushed++;

        return result;
    }

    private ExecResult Execute(BlockNode node) {
        var result = NewResult();

        using var frame = PushFrame();

        if (CurrentFrame != null) {
            CurrentFrame.CurrentBlock = node;
        }

        foreach (var n in node.Nodes) {
            if (n is DeferStatementNode defer) {
                CurrentFrame!.DeferStatements.Add(defer);
                continue;
            }

            Execute(n);
        }


        return result;
    }


    private ExecResult Execute(ArgumentDeclarationNode node) {
        return new();
    }
    private ExecResult Execute(ArgumentListDeclarationNode node) {
        return new();
    }
    private ExecResult Execute(LiteralValueNode node) {
        return new();
    }
    private ExecResult Execute(ExpressionListNode node) {
        return new();
    }

    private ExecResult Execute(ObjectProperty node) {
        return new();
    }
    private ExecResult Execute(NodeList<ArgumentDeclarationNode> node) {
        return new();
    }
    private ExecResult Execute(NodeList<IExpressionNode> node) {
        return new();
    }
    private ExecResult Execute(NodeList<BaseNode> node) {
        return new();
    }
}