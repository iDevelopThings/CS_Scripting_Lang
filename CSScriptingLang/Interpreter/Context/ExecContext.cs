using System.Diagnostics;
using System.Runtime.CompilerServices;
using CSScriptingLang.Core.Diagnostics;
using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.Interpreter.Libraries;
using CSScriptingLang.Interpreter.Modules;
using CSScriptingLang.Lexing;
using CSScriptingLang.Mixins;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.IncrementalParsing.Syntax;
using CSScriptingLang.IncrementalParsing.Syntax.SyntaxNodes;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.RuntimeValues.Values;
using CSScriptingLang.Utils;
using MoreLinq;
using SharpX;
using FunctionDeclaration = CSScriptingLang.Interpreter.Execution.Expressions.FunctionDeclaration;
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

[AddMixin(typeof(DiagnosticLoggingMixin))]
public partial class ExecContext
{
    public Interpreter Interpreter { get; set; }

    public VariablesStack                           Variables        { get; set; } = new();
    public ExecTable<string, VariableSymbol, Value> CurrentVariables => Variables.Peek();
    public Dictionary<string, VariableSymbol>       AllVariables     => Variables.SelectMany(s => s.Table).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

    public FunctionsStack                                              Functions        { get; set; } = new();
    public Dictionary<string, FunctionDeclaration>                     AllFunctions     => Functions.SelectMany(s => s.Table).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    public ExecTable<string, FunctionDeclaration, FunctionDeclaration> CurrentFunctions => Functions.Peek();

    public Module         Module         { get; set; }
    public ModuleResolver ModuleResolver => Interpreter.ModuleResolver;

    public Stack<Frame> CallStack { get; private set; } = new();

    public int     frameSize = 0;
    public Frame[] Frames    = new Frame[250];

    public Frame CurrentCallFrame => Frames[frameSize > 0 ? frameSize - 1 : 0];

    public CallExpression Caller     { get; set; }
    public CallExpr       CallerExpr { get; set; }

    public ValueEvaluationType EvaluationType { get; set; } = ValueEvaluationType.RValue;

    public LibraryManager Libraries { get; set; } = new() {
        new StandardLibraries(),
    };

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
        CallerExpr  = ctx.CallerExpr;

        Frames    = ctx.Frames;
        frameSize = ctx.frameSize;

        if (pushScope) {
            PushScope();
        }
    }

    public ExecContext(Interpreter interpreter, bool pushScope = true) {
        Interpreter = interpreter;
        Module      = interpreter.Module;

        if (pushScope) {
            PushScope();
        }
    }

    public ExecContext(bool pushScope = true) {
        if (pushScope) {
            PushScope();
        }
    }

    public bool GetVariable(string name, out VariableSymbol symbol) {
        return Variables.Get(ModulePrefixedName(name), out symbol);
        // return CurrentVariables.Get(ModulePrefixedName(name), out symbol);
    }
    public VariableSymbol GetVariable(string name) => GetVariable(name, out var symbol) ? symbol : null;
    public Value GetOrCreateVariable(string name, Func<Value> factory) {
        if (GetVariable(name, out var symbol))
            return symbol.Val;

        var value = factory();
        CurrentVariables.Set(ModulePrefixedName(name), value);

        return value;
    }

    public virtual UsingCallbackHandle UsingScope(bool push = true) {
        if (push)
            PushScope();
        return new UsingCallbackHandle(
            () => {
                if (push)
                    PopScope();
            }
        );
    }
    public UsingCallbackHandle UsingBlockCallbacks(Action onBeforeExecuteBlock = null, Action onAfterExecuteBlock = null) {
        onBeforeExecuteBlock?.Invoke();

        return new UsingCallbackHandle(
            () => {
                onAfterExecuteBlock?.Invoke();
            }
        );
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
        if (Module == null)
            return name;

        return Module.IsMainModule ? name : $"{Module.Name}.{name}";
    }

    public Value MakeFunction(InlineFunctionDeclaration fn) {
        var closure = new FnClosure(fn);

        void OnBeforeExecuteBlock(FunctionExecContext ctx, Value instance, Value[] arguments) {
            if (instance != null)
                ctx.Scope.Set("this", instance);

            for (var i = 0; i < fn.Parameters.Arguments.Count; i++) {
                var param = fn.Parameters.Arguments[i];
                var arg   = arguments[i];

                ctx.Scope.Set(param.Name, arg);
            }
        }

        if (fn.IsSeq) {
            closure.Interpreted = (ctx, instance, arguments) => {
                var enumerable = fn.Body.ExecuteEnumerable(
                    ctx,
                    true,
                    () => OnBeforeExecuteBlock(ctx, instance, arguments)
                );

                var cEnumerator = enumerable.GetEnumerator();

                var enumerator = Value.Object(ctx);
                enumerator["current"] = cEnumerator.Current.MatchJust(out var value) ? value : Value.Null();
                enumerator["moveNext"] = Value.Function(
                    "moveNext", (_, args) => {
                        if (cEnumerator.MoveNext()) {
                            enumerator["current"] = cEnumerator.Current.MatchJust(out var v) ? v : Value.Null();
                            return true;
                        }

                        return false;
                    }
                );
                enumerator["dispose"] = Value.Function(
                    "dispose", (_, args) => {
                        cEnumerator.Dispose();
                        return Value.Null();
                    }
                );

                return enumerator;
            };

            return Value.Function(closure);
        }

        closure.Interpreted = (ctx, instance, arguments) => {

            fn.Body.Execute(
                ctx,
                true,
                () => OnBeforeExecuteBlock(ctx, instance, arguments)
            );

            foreach (var val in ctx.ReturnValues) {
                if (val != null)
                    return val;
            }

            return null;
            // throw new FatalInterpreterException("Function did not return a value", fn);
        };

        return Value.Function(closure);
    }

    public (Value value, VariableSymbol symbol) DeclareFunction(InlineFunctionDeclaration fn) {
        var fnValue = MakeFunction(fn);

        var fnSymbol = CurrentVariables.Set(ModulePrefixedName(fn.Name), fnValue);

        return (fnValue, fnSymbol);
    }
    public Value MakeFunction(FunctionDecl fn) {
        var closure = new FnClosure(fn);

        void OnBeforeExecuteBlock(FunctionExecContext ctx, Value instance, Value[] arguments) {
            if (instance != null)
                ctx.Scope.Set("this", instance);

            foreach (var (i, param) in fn.Arguments.Arguments.Index()) {
                var arg = arguments[i];
                ctx.Scope.Set(param.Name, arg);
            }
        }

        if (fn.IsSeq) {
            closure.Interpreted = (ctx, instance, arguments) => {
                var enumerable = fn.Body.Execute(
                    ctx,
                    true,
                    () => OnBeforeExecuteBlock(ctx, instance, arguments)
                );

                var cEnumerator = enumerable.GetEnumerator();

                var enumerator = Value.Object(ctx);
                enumerator["current"] = cEnumerator.Current.MatchJust(out var value) ? value : Value.Null();
                enumerator["moveNext"] = Value.Function(
                    "moveNext", (_, args) => {
                        if (cEnumerator.MoveNext()) {
                            enumerator["current"] = cEnumerator.Current.MatchJust(out var v) ? v : Value.Null();
                            return true;
                        }

                        return false;
                    }
                );
                enumerator["dispose"] = Value.Function(
                    "dispose", (_, args) => {
                        cEnumerator.Dispose();
                        return Value.Null();
                    }
                );

                return enumerator;
            };

            return Value.Function(closure);
        }

        closure.Interpreted = (ctx, instance, arguments) => {

            var results = fn.Body.Execute(
                ctx,
                true,
                () => OnBeforeExecuteBlock(ctx, instance, arguments)
            ).ToList();

            foreach (var val in ctx.ReturnValues) {
                if (val != null)
                    return val;
            }

            return null;
            // throw new FatalInterpreterException("Function did not return a value", fn);
        };

        return Value.Function(closure);
    }
    public (Value value, VariableSymbol symbol) DeclareFunction(FunctionDecl fn) {
        var fnValue = MakeFunction(fn);

        var fnSymbol = CurrentVariables.Set(ModulePrefixedName(fn.Name), fnValue);

        return (fnValue, fnSymbol);
    }


    public IEnumerable<VariableSymbol> DeclareVariable(VariableDeclarationNode node) {
        foreach (var initializer in node.Initializers) {
            var declName = ModulePrefixedName(initializer.Name);
            var symb = Variables.Declare(
                declName, () => new VariableSymbol(declName) {
                    IsBaseDeclaration = true,
                }
            );
            yield return symb;
        }
    }
    public IEnumerable<VariableSymbol> DeclareVariable(VariableDecl node) {
        foreach (var initializer in node.VarValuePairs) {
            var declName = ModulePrefixedName(initializer.var);
            var symb = Variables.Declare(
                declName, () => new VariableSymbol(declName) {
                    IsBaseDeclaration = true,
                }
            );
            yield return symb;
        }
    }

    public bool TryGetVariable(string name, out VariableSymbol variable) {
        if (Variables.Get(ModulePrefixedName(name), out variable))
            return true;

        if (CurrentCallFrame?.TryGetLocal(name, out variable) ?? false)
            return true;

        variable = null;

        return false;
    }

    public FunctionExecContext CreateFnExecContext(Value instance = null, FnClosure fn = null) {
        return new(this) {
            Function = fn?.Declaration,
            FnDecl   = fn?.Decl,
            This     = instance,
        };
    }

    public Value Call(Value fnValue, Value instance, params Value[] args) {
        var fn = fnValue.As.Fn();

        var fnContext = CreateFnExecContext(instance, fn);
        fnContext.PushScope();

        var parentFrame = fn.Frame ?? CurrentCallFrame;

        var frame = new Frame(
            returnExpression: fnContext.Caller,
            name: fnContext.Function?.Name ?? fn.Name,
            parent: parentFrame,
            depth: (parentFrame?.Depth ?? 0) + 1
        ) {
            CallExpression = fnContext.CallerExpr,
        };

        fn.Frame = frame;

        Frames[frameSize++] = frame;

        fnContext.PushTypeArgs();

        // Function is native bind if null
        if (fn.Declaration != null)
            fnContext.PushCallArgs(frame, instance, args);
        if (fn.Decl != null)
            fnContext.PushCallArgs(frame, instance, args);

        Value returnValue = null;
        try {
            // using var _ = TimedScope.Scoped_Print("Function Call: " + fn.Name);

            returnValue = fn.Call(fnContext, instance, args);
        }
        catch (ReturnException e) {
            returnValue = e.ReturnValue;
            fnContext.ReturnValues.Add(returnValue);
        }

        /*while (true) {
            try {
                returnValue = fn.Call(fnContext, instance, args);

                if (fnContext.TailCallExpression != null) {
                    // If there's a tail call, reset the function and args and continue
                    var tailCall = fnContext.TailCallExpression;

                    fnValue = tailCall.Variable?.Execute(fnContext) ?? tailCall.Identifier?.Execute(fnContext) ?? fnContext.ValReference(Value.Null());
                    fn      = fnValue.As.Fn();
                    args    = tailCall.Arguments.Execute(fnContext).Select(v => v.Value).ToArray();

                    fnContext.TailCallExpression = null; // Clear tail call and re-execute
                    continue;
                }
            }
            catch (ReturnException e) {
                returnValue = e.ReturnValue;
                break;
            }

            break;
        }*/

        frameSize--;

        fnContext.PopScope();

        return returnValue;
    }

    private Stack<Module> _moduleSwitchStack = new();
    public UsingCallbackHandle SwitchModule(Module module) {
        if (Module == module || module == null)
            return new UsingCallbackHandle(() => { });

        _moduleSwitchStack.Push(Module);
        Module = module;

        return new UsingCallbackHandle(
            () => {
                Module = _moduleSwitchStack.Pop();
            }
        );
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
        return new UsingCallbackHandle(
            () => {
                EvaluationType = old;
            }
        );
    }
    /// <summary>
    /// Execute in LValue context(IE; for assignment)
    /// </summary>
    /// <returns></returns>
    [DebuggerStepThrough]
    public UsingCallbackHandle UsingLValueMode()
        => UsingEvaluationMode(ValueEvaluationType.LValue);
    /// <summary>
    /// Execute in RValue context(IE; for reading)
    /// </summary>
    /// <returns></returns>
    public UsingCallbackHandle UsingRValueMode()
        => UsingEvaluationMode(ValueEvaluationType.RValue);
    /// <summary>
    /// Execute in LValue context(IE; for assignment)
    /// </summary>
    /// <param name="eval"></param>
    /// <returns></returns>
    [DebuggerStepThrough]
    public ValueReference ExecuteLValue(Func<ExecContext, ValueReference> eval)
        => ExecuteWithType(ValueEvaluationType.LValue, eval);
    [DebuggerStepThrough]
    public ValueReference ExecuteLValue(Func<ExecContext, Maybe<ValueReference>> eval)
        => ExecuteWithType(ValueEvaluationType.LValue, ctx => eval(ctx).Value());
    /// <summary>
    /// Execute in RValue context(IE; for reading)
    /// </summary>
    /// <param name="eval"></param>
    /// <returns></returns>
    [DebuggerStepThrough]
    public ValueReference ExecuteRValue(Func<ExecContext, ValueReference> eval)
        => ExecuteWithType(ValueEvaluationType.RValue, eval);
    [DebuggerStepThrough]
    public ValueReference ExecuteRValue(Func<ExecContext, Maybe<ValueReference>> eval)
        => ExecuteWithType(ValueEvaluationType.RValue, ctx => eval(ctx).Value());


    public UsingCallbackHandle SetCaller(CallExpression node) {
        Caller = node;
        return new UsingCallbackHandle(
            () => {
                Caller = null;
            }
        );
    }
    public UsingCallbackHandle SetCaller(CallExpr node) {
        CallerExpr = node;
        return new UsingCallbackHandle(
            () => {
                CallerExpr = null;
            }
        );
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

    public void LogError(BaseNode node, string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "")
        => Diagnostic_Error_Fatal().Message(message).Range(node).Caller(Utils.Caller.FromAttributes(file, line, member)).Report();

    public void LogError(SyntaxElement node, string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "")
        => Diagnostic_Error_Fatal().Message(message).Range(node).Caller(Utils.Caller.FromAttributes(file, line, member)).Report();

    public void LogWarning(BaseNode node, string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "")
        => Diagnostic_Warning().Message(message).Range(node).Caller(Utils.Caller.FromAttributes(file, line, member)).Report();

    public void LogWarning(SyntaxElement node, string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "")
        => Diagnostic_Warning().Message(message).Range(node).Caller(Utils.Caller.FromAttributes(file, line, member)).Report();

    /*public void LogError(BaseNode node, string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "") {
        ErrorWriter.Configure(node?.GetScript(), file, line, member);
        throw new FatalInterpreterException(message, node)
           .WithCaller(Utils.Caller.FromAttributes(file, line, member));
    }
    public void LogWarning(BaseNode node, string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "") {
        ErrorWriter.Create(node, file, line, member).LogWarning(message, node);
    }*/

    public void PushDefer(Expression expression) {
        if (this is not FunctionExecContext fnCtx)
            LogError(expression, "Defer statement outside of function context");

        CurrentCallFrame?.DeferExpressions.Add(expression);
    }

    public class BreakException : Exception
    {
        public int Count { get; set; }
        public BreakException(BreakException ex) : this(ex.Count - 1) { }
        public BreakException(int count) {
            Count = count;
        }
    }

    public class ReturnException : BreakException
    {
        public ValueReference ReturnValue { get; set; }
        public ReturnException() : base(1) { }
        public ReturnException(ValueReference returnValue) : base(1) {
            ReturnValue = returnValue;
        }
    }

    public class ContinueException : Exception { }

    public void Return()                 => throw new ReturnException();
    public void Return(ValueReference v) => throw new ReturnException(v);

    public void Break(ValueReference count) {
        throw new BreakException((int) count.Value);
    }
    public void Continue() {
        throw new ContinueException();
    }
}

public class FunctionExecContext : ExecContext
{
    public Value          This       { get; set; }
    public VariableSymbol ThisSymbol { get; set; }

    public InlineFunctionDeclaration Function { get; set; }
    public FunctionDecl              FnDecl   { get; set; }

    public List<VariableSymbol> Params { get; set; } = new();

    public ExecTable<string, VariableSymbol, Value> Scope { get; set; }

    public struct TypeParameter
    {
        public string Name;

        private ValueType _type;

        public ValueType Type {
            get => _type ??= TypesTable.GetPrototypeTypeByName(Name);
            set => _type = value;
        }

        public override string ToString() {
            return $"{Name} : {(Type?.Name ?? "null/undefined/unknown type")}";
        }
    }

    public List<TypeParameter> TypeArgs { get; set; } = new();

    public List<Value>    ReturnValues       { get; set; } = new();
    public CallExpression TailCallExpression { get; set; }

    public FunctionExecContext(ExecContext ctx) : base(ctx, false) { }

    public override void PushScope([CallerMemberName] string name = "", [CallerLineNumber] int line = 0, [CallerFilePath] string file = "") {
        base.PushScope(name, line, file);
        Scope = CurrentVariables;
    }
    public Value[] PushCallArgs(Frame frame, Value instance, Value[] args) {
        if (instance != null) {
            // insert "this" value into argument array
            Array.Resize(ref args, args.Length + 1);
            Array.Copy(args, 0, args, 1, args.Length - 1);
            args[0] = instance;

            frame.Locals["this"] = Scope.Set("this", instance);
        }

        if (Function != null) {
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

                frame.Locals[param.Name] = symbol;

                Params.Add(symbol);
            }
        }
        if (FnDecl != null) {
            foreach (var (i, param) in FnDecl.Arguments.Arguments.Index()) {
                Value argValue = null;
                if (args.Length <= i) {
                    // if (!param.IsOptional) {
                    // throw new InterpreterRuntimeException($"Missing required argument {param.Name}");
                    // }

                    argValue = Value.Null();
                } else {
                    argValue = args[i];
                }

                var symbol = Scope.Set(param.Name, argValue);

                frame.Locals[param.Name] = symbol;

                Params.Add(symbol);
            }
        }


        return args;
    }

    public void PushTypeArgs() {
        if (Caller != null) {
            foreach (var typeArg in Caller.TypeParameters) {
                var type = TypesTable.GetPrototypeTypeByName(typeArg.Name);

                TypeArgs.Add(
                    new TypeParameter {
                        Name = typeArg.Name,
                        Type = type,
                    }
                );
            }
        }

        if (CallerExpr != null) {
            foreach (var typeArg in CallerExpr.TypeParams) {
                var type = TypesTable.GetPrototypeTypeByName(typeArg.Name);
                TypeArgs.Add(
                    new TypeParameter {
                        Name = typeArg.Name,
                        Type = type,
                    }
                );
            }
        }
    }

}