using CSScriptingLang.Core;
using CSScriptingLang.Interpreter.Bindings;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Coroutines;
using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.Interpreter.Execution.Statements;
using CSScriptingLang.Interpreter.Modules;
using CSScriptingLang.Interpreter.SyntaxAnalysis;
using CSScriptingLang.Lexing;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.Utils;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.RuntimeValues.Values;

namespace CSScriptingLang.Interpreter;

public partial class Interpreter
{
    public ProgramExpression Program => Module?.MainScript?.AstData.Program;

    // Should only be used for the repl/lsp
    public static ExecContext Ctx { get; set; }

    public Interpreter() { }
    public Interpreter(InterpreterFileSystem fs = null) {
        Configure(fs);
    }

    public void Configure(InterpreterFileSystem fs) {
        FileSystem     = fs;
        ModuleResolver = new ModuleResolver(FileSystem);
        Scheduler      = new Scheduler(this);

        if (InterpreterConfig.Mode == InterpreterMode.Lsp) {
            FileSystem.OnFileAdded += file => {
                ModuleResolver.LoadFile(file);
            };
        }
    }

    public ExecContext GetNewExecContext() => new(this);

    public ExecContext Initialize(ExecContext ctx = null) {
        using var _ = Timer.NewWith("Initialization".Bold());

        ctx             ??= GetNewExecContext();
        ctx.Interpreter =   this;

        NativeBinder.BindNativeFunctions(ctx);

        Module ??= ModuleResolver.MainModule;

        return ctx;
    }

    /// <summary>
    /// Mainly used in `LanguageTests.BaseCompilerTest` for manually setting up the interpreter with it's rules
    /// </summary>
    public ExecContext ExecuteStandalone(ExecContext ctx = null) {
        ctx             ??= GetNewExecContext();
        ctx.Interpreter =   this;

        NativeBinder.BindNativeFunctions(ctx);

        Module ??= ModuleResolver.MainModule;

        ctx.Libraries.OnPreLoad += collection => {
            TypesTable.Initialize();
        };
        ctx.Libraries.Load(ctx, collection => {
            Logger.Info("Loading libraries");
        });

        // TypeAnalyzer.TypeCheck(Program, ctx);

        ExecuteProgram(Program, ctx);

        return ctx;
    }

    /// <summary>
    /// Does the full initialization process required to execute the program
    /// </summary>
    public ExecContext Execute(ExecContext ctx = null, Action onBeforeExecuteProgram = null) {
        ctx             ??= GetNewExecContext();
        ctx.Interpreter ??= this;

        NativeBinder.BindNativeFunctions(ctx);

        ModuleResolver.Load(ctx);

        Module     ??= ModuleResolver.MainModule;
        ctx.Module =   ModuleResolver.MainModule;

        ctx.Libraries.OnPreLoad += collection => {
            TypesTable.Initialize();
        };
        ctx.Libraries.Load(ctx, collection => {
            Logger.Info("Loading libraries");
        });

        TypeAnalyzer.TypeCheck(Program, ctx);

        onBeforeExecuteProgram?.Invoke();

        ExecuteProgram(Program, ctx);

        return ctx;
    }


    public ExecResult ExecuteProgram(ProgramExpression node, ExecContext ctx) {
        using var _ = Timer.NewWith("Execute Program".BoldBrightGreen());

        var result = NewResult();

        ctx.OnBeforeExecuteProgram?.Invoke(ctx);

        node.Execute(ctx);
        
        /*
        foreach (var n in node.Nodes) {
            Execute(n, ctx);

            Scheduler.Tick();
        }

        while (Scheduler.HasActiveCoroutines()) {
            Scheduler.Tick();
        }
        */

        return result;
    }
    public ExecResult ExecuteNodes(IEnumerable<BaseNode> nodes, ExecContext ctx) {
        var nodeList = nodes.ToList();

        using var _ = Timer.NewWith($"Execute Nodes({nodeList.Count})".BoldBrightGreen());

        var result = NewResult();

        foreach (var n in nodeList) {
            result += Execute(n, ctx);

            Scheduler.Tick();
        }

        while (Scheduler.HasActiveCoroutines()) {
            Scheduler.Tick();
        }

        return result;
    }

    public ExecResult Execute(BaseNode node, ExecContext ctx) {
        ExecResult result;

        switch (node) {
            case SignalDeclarationNode n:
                result = Execute(n, ctx);
                break;
            
            case BlockExpression n: {
                result = NewResult();
                n.Execute(ctx);
                return result;
            }

            case CallExpression n: {
                return NewResult(n.Execute(ctx));
            }

            case VariableDeclarationNode n: {
                return NewResult(n.ExecuteMulti(ctx));
            }

            case ExpressionListNode n: {
                result = NewResult();
                foreach (var valueReference in n.Execute(ctx)) {
                    result += valueReference;
                }
                return result;
            }
            case Expression e: {
                return NewResult(e.Execute(ctx));
            }

            case DeferStatement n:
                return NewResult(n.Execute(ctx));

            case IfStatementNode n:
                return NewResult(n.Execute(ctx));

            case ForRangeStatement n: 
                return NewResult(n.Execute(ctx));
            case ForLoopStatement n:
                return NewResult(n.Execute(ctx));

            default: {
                result = NewResult();

                if (node != null) {
                    LogError(node, $"Unhandled node type: {node.GetType().Name}");
                } else {
                    throw new NotImplementedException($"Null node");
                }

                break;
            }
        }

        return result;
    }

    private ExecResult Execute(SignalDeclarationNode node, ExecContext ctx) {
        var result = NewResult();

        if (node.Type == null) {
            node.Type = TypeTable.RegisterSignalType(node.Name, null);
            node.TypeAs<RuntimeTypeInfo_Signal>().Parameters.AddRange(node.Parameters.Arguments.Select(p => {
                p.Type ??= TypeTable.Get(p.TypeName);

                return new RuntimeTypeInfo_Signal.Parameter() {
                    Name = p.Name,
                    Type = p.Type
                };
            }));
        }

        var symbol = ctx.Variables.Declare(node.Name);
        // var signal = new ValueSignal(symbol, node);

        var signal = Value.Signal();
        symbol.Val = signal;

        return result;
    }


    private ExecResult ExecuteOp(
        Value        left,
        OperatorType op,
        Value        right,
        ExecContext  ctx
    ) {
        var result = NewResult();

        if (left != null) {
            var opResult = left.Operator(op, right);
            result += opResult;
            return result;
        }

        return result;
    }


    public ExecResult ExecuteBlock(
        BlockExpression   node,
        ExecContext ctx,
        bool        pushScope            = true,
        Action      onBeforeExecuteBlock = null,
        Action      onAfterExecuteBlock  = null
    ) {
        var result = NewResult();

  

        return result;
    }
}