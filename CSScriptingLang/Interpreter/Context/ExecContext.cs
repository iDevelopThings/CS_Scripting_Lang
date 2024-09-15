using System.Diagnostics;
using System.Runtime.CompilerServices;
using CSScriptingLang.Interpreter.Execution.Declaration;
using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.Interpreter.Libraries;
using CSScriptingLang.Interpreter.Modules;
using CSScriptingLang.Lexing;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.RuntimeValues.Values;
using CSScriptingLang.Utils;
using ValueType = CSScriptingLang.RuntimeValues.Types.ValueType;

namespace CSScriptingLang.Interpreter.Context;

public enum ValueEvaluationType
{
    /// <summary>
    /// Evaluate to get the value
    /// IE; this will return raw `Value` objects/variables
    /// </summary>
    [DebuggerDisplay("RValue(reference=false)")]
    RValue,
    /// <summary>
    /// This will return `ValueReference` structs holding data about the value
    /// Used in expressions where the value is being assigned to
    /// </summary>
    [DebuggerDisplay("LValue(reference=true)")]
    LValue,
}

public class ExecContext
{
    public Interpreter Interpreter { get; set; }

    public VariablesStack                           Variables        { get; set; } = new();
    public ExecTable<string, VariableSymbol, Value> CurrentVariables => Variables.Peek();
    public Dictionary<string, VariableSymbol>       AllVariables     => Variables.SelectMany(s => s.Table).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

    public FunctionsStack                                              Functions        { get; set; } = new();
    public Dictionary<string, FunctionDeclaration>                     AllFunctions     => Functions.SelectMany(s => s.Table).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    public ExecTable<string, FunctionDeclaration, FunctionDeclaration> CurrentFunctions => Functions.Peek();

    public Dictionary<(string fqn, string name), (RuntimeType, Value)> Prototypes { get; set; } = new();

    public Module         Module         { get; set; }
    public ModuleResolver ModuleResolver => Interpreter.ModuleResolver;

    public Stack<Frame> CallStack        { get; private set; } = new();
    public Frame        CurrentCallFrame => CallStack.Peek();

    public CallExpression Caller { get; set; }

    public ValueEvaluationType EvaluationType { get; set; } = ValueEvaluationType.RValue;

    public LibraryManager Libraries { get; set; } = new() {
        new StandardLibraries(),
    };

    public Action<ExecContext> OnBeforeExecuteProgram { get; set; }

    public event Action<ExecContext, LibraryCollection> OnConfigureLibraries {
        add => Libraries.OnConfigure += value;
        remove => Libraries.OnConfigure -= value;
    }

    public event Action<LibraryManager> OnPreLoadLibraries {
        add => Libraries.OnPreLoad += value;
        remove => Libraries.OnPreLoad -= value;
    }

    public ExecContext(ExecContext ctx, bool pushScope = true) {
        Interpreter = ctx.Interpreter;
        Variables   = ctx.Variables;
        Module      = ctx.Module;
        Functions   = ctx.Functions;
        CallStack   = ctx.CallStack;
        Caller      = ctx.Caller;

        if (pushScope) {
            PushScope();
            PushFrame();
        }
    }

    public ExecContext(Interpreter interpreter, bool pushScope = true) {
        Interpreter = interpreter;
        Module      = interpreter.Module;

        if (pushScope) {
            PushScope();
            PushFrame();
        }
    }

    public ExecContext(bool pushScope = true) {
        if (pushScope) {
            PushScope();
            PushFrame();
        }
    }

    public bool           GetVariable(string name, out VariableSymbol symbol) => CurrentVariables.Get(ModulePrefixedName(name), out symbol);
    public VariableSymbol GetVariable(string name) => GetVariable(name, out var symbol) ? symbol : null;

    public virtual Frame PushFrame(
        CallExpression returnExpression = null,
        Frame          parent           = null,
        string         name             = null
    ) {
        var frame = new Frame(
            returnExpression,
            parent ?? (CallStack.TryPeek(out var p) ? p : null),
            name
        );
        CallStack.Push(frame);
        InterpreterEvents.OnFunctionFramePushed?.Invoke(frame);
        return frame;
    }
    public virtual Frame PopFrame() {
        var frame = CallStack.Pop();
        InterpreterEvents.OnFunctionFramePopped?.Invoke(frame);
        return frame;
    }

    public virtual UsingCallbackHandle UsingScope() {
        PushScope();
        return new UsingCallbackHandle(PopScope);
    }

    public virtual void PushScope([CallerMemberName] string name = "", [CallerLineNumber] int line = 0, [CallerFilePath] string file = "") {
        Variables.PushScope(name, line, file);
        Functions.PushScope(name, line, file);
    }

    public virtual void PopScope() {
        Variables.PopScope();
        Functions.PopScope();
    }

    public string ModulePrefixedName(string name) {
        return Module.IsMainModule ? name : $"{Module.Name}.{name}";
    }

    public Value MakeFunction(InlineFunctionDeclaration fn, RuntimeType owner = null) {
        var closure = new FnClosure(fn);

        closure.Interpreted = (ctx, instance, arguments) => {
            ctx.Function.Body.Execute(
                ctx,
                true,
                () => {
                    if (instance != null)
                        ctx.Scope.Set("this", instance);

                    for (var i = 0; i < ctx.Function.Parameters.Arguments.Count; i++) {
                        var param = ctx.Function.Parameters.Arguments[i];
                        var arg   = arguments[i];

                        ctx.Scope.Set(param.Name, arg);
                    }
                }
            );

            foreach (var val in ctx.ReturnValues) {
                if (val != null)
                    return val;
            }

            return Value.Null();
        };

        return Value.Function(closure);
    }
    
    public (Value value, VariableSymbol symbol) DeclareFunction(InlineFunctionDeclaration fn, RuntimeType owner = null) {
        var fnValue  = MakeFunction(fn, owner);
        
        var fnSymbol = CurrentVariables.Set(ModulePrefixedName(fn.Name), fnValue);

        return (fnValue, fnSymbol);

        /*
        if (fn.TypeReference.IsResolvedOrDefined())
            return (RuntimeTypeInfo_Function) fn.Type;

        var declName = ModulePrefixedName(fn.Name);

        if (owner != null) {
            fn.Parameters.Arguments.Insert(0, new ArgumentDeclarationNode {
                Name     = "this",
                TypeName = owner.Name,
            });
        }

        var fnDec = CurrentFunctions.Declare(declName, fn);

        var type = TypeTable.Current.RegisterFunctionType(declName, fn, owner);
        type.Parameters.AddRange(fn.Parameters.Arguments.Select(p => {
            if (!p.IsVariadic)
                p.Type ??= TypeTable.Current.Get(p.TypeName);

            return new RuntimeTypeInfo_Function.Parameter() {
                Name       = p.Name,
                Type       = p.Type,
                IsVariadic = p.IsVariadic,
            };
        }));

        fn.TypeReference.SetType(type, Module);

        var fnInst = ValueFactory.Function.Make(fnDec);
        fnInst.Executable = (ctx, args) => {
            ctx.Interpreter.ExecuteBlock(ctx.Function.Body, ctx, false);

            foreach (var val in ctx.ReturnValues) {
                if (val != null)
                    return val;
            }

            return Value.Null();
        };

        var fnSymb = CurrentVariables.Declare(declName, new VariableSymbol(declName, fnInst));

        return type;*/
    }

    public RuntimeTypeInfo_Signal DeclareSignal(SignalDeclarationNode signal) {
        if (signal.TypeReference.IsResolvedOrDefined())
            return (RuntimeTypeInfo_Signal) signal.Type;

        var declName = ModulePrefixedName(signal.Name);
        var type     = TypeTable.Current.RegisterSignalType(declName, signal);

        type.Parameters.AddRange(signal.Parameters.Arguments.Select(p => {
            p.Type ??= TypeTable.Current.Get(p.TypeName);

            return new RuntimeTypeInfo_Signal.Parameter() {
                Name = p.Name,
                Type = p.Type,
            };
        }));

        signal.TypeReference.SetType(type, Module);

        return type;
    }

    public RuntimeType DeclareType(TypeDeclaration declaration) {
        if (declaration.TypeReference.IsResolvedOrDefined())
            return declaration.Type;

        var declName = ModulePrefixedName(declaration.Name);
        var type = new RuntimeTypeInfo_Struct {
            Name       = declName,
            LinkedNode = declaration,
        };
        TypeTable.Current.RegisterType(type);

        declaration.TypeReference.SetType(type, Module);

        foreach (var member in declaration.Members) {
            var rtType = TypeTable.TryGet(member.TypeName);
            type.RegisterField(member.Name, ValueFactory.Make(rtType.GetType()));
        }

        foreach (var method in declaration.Methods) {
            DeclareFunction(method, type);
            type.RegisterField(method.Name, ValueFactory.Function.Make(method));
        }

        return type;
    }

    public (ValueType type, Value proto) DeclarePrototype(string name, string fqnName, Value prototype, RuntimeType owner = null) {
        /*var objType = TypeTable.Current.RegisterObjectType(
            fqnName,
            owner,
            name
        );

        objType.FQN = fqnName;

        foreach (var pair in prototype.Members) {
            objType.RegisterField(pair.Key, pair.Value);

        }

        Prototypes[(fqnName, name)] = (objType, prototype);

        return objType;*/

        return TypesTable.RegisterPrototype(name, fqnName, prototype);

    }

    public IEnumerable<VariableSymbol> DeclareVariable(VariableDeclarationNode node) {
        foreach (var initializer in node.Initializers) {
            var declName = ModulePrefixedName(initializer.Name);
            var symb = Variables.Declare(declName, () => new VariableSymbol(declName) {
                IsBaseDeclaration = true,
            });
            yield return symb;
        }
    }

    public Value Call(Value fn, Value instance, params Value[] args) {
        var fnContext = new FunctionExecContext(this) {
            Function = fn.As.Fn().Declaration,
            This     = instance,
        };

        fnContext.PushScope();
        fnContext.PushFrame(
            returnExpression: fnContext.Caller,
            name: fnContext.Function?.Name ?? fn.As.Fn().Name
        );

        fnContext.PushTypeArgs();

        // Function is native bind if null
        if (fn.As.Fn().Declaration != null)
            fnContext.PushCallArgs(instance, args);

        var returnValue = fn.As.Fn().Call(fnContext, instance, args);

        fnContext.PopFrame();
        fnContext.PopScope();

        return returnValue;
    }

    private Stack<Module> _moduleSwitchStack = new();
    public UsingCallbackHandle SwitchModule(Module module) {
        if (Module == module || module == null)
            return new UsingCallbackHandle(() => { });

        _moduleSwitchStack.Push(Module);
        Module = module;

        return new UsingCallbackHandle(() => {
            Module = _moduleSwitchStack.Pop();
        });
    }

    public ExecResult EvaluateWithType(ValueEvaluationType type, Func<ExecResult> eval) {
        var old = EvaluationType;

        EvaluationType = type;
        var result = eval();
        EvaluationType = old;

        return result;
    }
    public ExecResult EvaluateAsLValue(Func<ExecResult> eval) => EvaluateWithType(ValueEvaluationType.LValue, eval);
    public ExecResult EvaluateAsRValue(Func<ExecResult> eval) => EvaluateWithType(ValueEvaluationType.RValue, eval);

    [DebuggerStepThrough]
    public ValueReference ExecuteWithType(ValueEvaluationType type, Func<ExecContext, ValueReference> eval) {
        var old = EvaluationType;

        EvaluationType = type;
        var result = eval(this);
        EvaluationType = old;

        return result;
    }

    [DebuggerStepThrough]
    public UsingCallbackHandle UsingEvaluationMode(ValueEvaluationType type) {
        var old = EvaluationType;
        EvaluationType = type;
        return new UsingCallbackHandle(() => {
            EvaluationType = old;
        });
    }
    /// <summary>
    /// Execute in LValue context(IE; for assignment)
    /// </summary>
    /// <returns></returns>
    [DebuggerStepThrough]
    public UsingCallbackHandle UsingLValueMode() => UsingEvaluationMode(ValueEvaluationType.LValue);
    /// <summary>
    /// Execute in RValue context(IE; for reading)
    /// </summary>
    /// <returns></returns>
    public UsingCallbackHandle UsingRValueMode() => UsingEvaluationMode(ValueEvaluationType.RValue);
    /// <summary>
    /// Execute in LValue context(IE; for assignment)
    /// </summary>
    /// <param name="eval"></param>
    /// <returns></returns>
    [DebuggerStepThrough]
    public ValueReference ExecuteLValue(Func<ExecContext, ValueReference> eval) => ExecuteWithType(ValueEvaluationType.LValue, eval);
    /// <summary>
    /// Execute in RValue context(IE; for reading)
    /// </summary>
    /// <param name="eval"></param>
    /// <returns></returns>
    [DebuggerStepThrough]
    public ValueReference ExecuteRValue(Func<ExecContext, ValueReference> eval) => ExecuteWithType(ValueEvaluationType.RValue, eval);


    public UsingCallbackHandle SetCaller(CallExpression node) {
        Caller = node;

        return new UsingCallbackHandle(() => {
            Caller = null;
        });
    }

    public ValueReference VariableAccessReference(VariableSymbol variable) {
        return ValueReference.VariableAccess(variable, this);
    }
    public ValueReference MemberAccessReference(Value instance, Value key) {
        return ValueReference.MemberAccess(instance, key, this);
    }
    public ValueReference IndexAccessReference(Value value, Value index) {
        return ValueReference.IndexAccess(value, index, this);
    }
    public ValueReference ValReference(Value value) {
        return new ValueReference(this, value);
    }


    public ref ExecResult AddVariableAccessReference(ref ExecResult result, VariableSymbol variable) {
        var varAccess = ValueReference.VariableAccess(variable, this);

        if (EvaluationType == ValueEvaluationType.LValue) {
            result += Value.Reference(varAccess);
        } else {
            result += varAccess.Variable;
            result += varAccess.Value;
        }

        return ref result;
    }
    public ref ExecResult AddMemberAccessReference(ref ExecResult result, Value instance, Value key) {
        var memberAccess = ValueReference.MemberAccess(instance, key, this);

        if (EvaluationType == ValueEvaluationType.LValue) {
            result += Value.Reference(memberAccess);
        } else {
            if (memberAccess.Variable != null)
                result += memberAccess.Variable;
            else if (memberAccess.Object != null)
                result += memberAccess.Object;
            result += memberAccess.Value;
        }

        return ref result;
    }
    public ExecResult AddIndexAccessReference(ref ExecResult result, Value value, Value index) {
        var indexAccess = ValueReference.IndexAccess(value, index, this);

        if (EvaluationType == ValueEvaluationType.LValue) {
            result += Value.Reference(indexAccess);
        } else {
            result += indexAccess.Variable;
            result += indexAccess.Value;
        }

        return result;
    }


    public void LogError(BaseNode node, string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "") {
        ErrorWriter.Configure(node?.GetScript(), file, line, member);

        throw new FatalInterpreterException(message, node)
           .WithCaller(Utils.Caller.FromAttributes(file, line, member));
    }
    public void LogWarning(BaseNode node, string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "") {
        ErrorWriter.Create(node, file, line, member).LogWarning(message, node);
    }

    public void PushDefer(Expression expression) {
        if (this is not FunctionExecContext fnCtx)
            throw new FatalInterpreterException("Defer statement outside of function context", expression);

        CurrentCallFrame?.DeferExpressions.Add(expression);
    }
}

public class FunctionExecContext : ExecContext
{
    public Value          This       { get; set; }
    public VariableSymbol ThisSymbol { get; set; }

    public InlineFunctionDeclaration Function { get; set; }

    public List<VariableSymbol> Params { get; set; } = new();

    public ExecTable<string, VariableSymbol, Value> Scope { get; set; }

    public struct TypeParameter
    {
        public string      Name;
        public RuntimeType Type;
    }

    public List<TypeParameter> TypeArgs { get; set; } = new();

    public List<Value> ReturnValues { get; set; } = new();

    public FunctionExecContext(ExecContext ctx) : base(ctx, false) { }

    public override void PushScope([CallerMemberName] string name = "", [CallerLineNumber] int line = 0, [CallerFilePath] string file = "") {
        base.PushScope(name, line, file);
        Scope = CurrentVariables;
    }
    public Value[] PushCallArgs(Value instance, Value[] args) {
        if (instance != null) {
            // insert "this" value into argument array
            Array.Resize(ref args, args.Length + 1);
            Array.Copy(args, 0, args, 1, args.Length - 1);
            args[0] = instance;

            Scope.Set("this", instance);
        }

        for (var i = 0; i < Function.Parameters.Arguments.Count; i++) {
            var param = Function.Parameters.Arguments[i];

            Value argValue = null;
            if (args.Length <= i) {
                if (!param.IsOptional) {
                    throw new InterpreterRuntimeException($"Missing required argument {param.Name}");
                }

                argValue = Value.Null();
            } else {
                argValue = args[i];
            }

            var symbol = Scope.Set(param.Name, argValue);

            Params.Add(symbol);
        }

        return args;
    }

    public void PushTypeArgs() {
        if (Caller == null) {
            throw new InterpreterRuntimeException("FunctionExecContext.PushTypeArgs: CallNode is null");
        }

        foreach (var typeArg in Caller.TypeParameters) {
            var type = TypeTable.Current.Get(typeArg.Name);

            TypeArgs.Add(new TypeParameter {
                Name = typeArg.Name,
                Type = type,
            });
        }
    }

}