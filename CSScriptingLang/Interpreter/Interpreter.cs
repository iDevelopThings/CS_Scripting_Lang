using System.Runtime.ExceptionServices;
using CSScriptingLang.Core;
using CSScriptingLang.Core.Async;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Coroutines;
using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.Interpreter.Execution.Statements;
using CSScriptingLang.Interpreter.Modules;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.IncrementalParsing.Syntax;
using CSScriptingLang.IncrementalParsing.Syntax.SyntaxNodes;
using CSScriptingLang.Utils;
using CSScriptingLang.RuntimeValues.Types;
using DeferStatement = CSScriptingLang.Interpreter.Execution.Statements.DeferStatement;

namespace CSScriptingLang.Interpreter;

public partial class Interpreter
{
    public static Interpreter Instance { get; set; }

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

        if (InterpreterConfig.Mode == InterpreterMode.Lsp && FileSystem == null)
            Logger.Warning("Configuring Interpreter, but FileSystem is null");

        if (InterpreterConfig.Mode == InterpreterMode.Lsp && FileSystem?.IsPhysical == true) {
            FileSystem.OnFileAdded += file => {

                var result = ModuleResolver.Resolve(
                    Ctx,
                    file.Path,
                    true,
                    true
                );

                if (result != null) { }
            };
        }

        Instance = this;
        Ctx      = GetNewExecContext();

    }

    public ExecContext GetOrCreateCtx(ExecContext ctx = null) {
        ExecContext c = null;

        if (ctx != null) {
            if (Ctx == null || ctx != Ctx) {
                Ctx = ctx;
            }
            c = Ctx;
        } else {
            c = Ctx ??= GetNewExecContext();
        }

        c.Interpreter = this;

        return c;
    }

    public ExecContext GetNewExecContext() {
        var ctx = new ExecContext(this);

        TypesTable.Initialize(ctx);

        ctx.Libraries.OnPreLoad += collection => { };

        return ctx;
    }

    public ExecContext Initialize(ExecContext ctx = null) {
        using var _ = Timer.NewWith("Initialization".Bold());

        ctx = GetOrCreateCtx(ctx);

        Module ??= ModuleResolver.MainModule;

        return ctx;
    }

    public ExecContext Execute(ExecContext ctx, string scriptPath, Action onBeforeExecuteProgram = null) {
        ctx = GetOrCreateCtx(ctx);

        var (exports, script) = ModuleResolver.ResolveEntryScript(ctx, scriptPath);

        return ExecuteScript(ctx, script, onBeforeExecuteProgram);
    }

    public ExecContext ExecuteScript(ExecContext ctx, Script script, Action onBeforeExecuteProgram = null) {
        ctx = GetOrCreateCtx(ctx);

        Script = script;
        Module = ctx.Module = Script.Module;

        Ctx.Libraries.Load(
            Ctx, collection => {
                Logger.Info("Loading libraries");
            }
        );

        onBeforeExecuteProgram?.Invoke();

        /*if (InterpreterConfig.WatchRootDirectory) {
            script.File.OnChanged += file => {
                ScriptTask.CancelAll();

                Execute(ctx, script.RelativePath, onBeforeExecuteProgram);
            };
        }*/

        var runNext = false;
        if (InterpreterConfig.WatchRootDirectory) {
            script.File.OnChanged += file => {
                script.File.OnChanged = null;
                runNext               = true;
                ScriptTask.CancelAll();
            };
        }

        var task = Task.Factory.StartNew(
            () => {
                if (InterpreterConfig.ExecMode == InterpreterExecMode.Original)
                    ExecuteProgram(Script.Program, ctx);
                else if (InterpreterConfig.ExecMode == InterpreterExecMode.IncrementalSyntaxTree)
                    ExecuteTree(Script.SyntaxTree, ctx);
            }
        );


        try {
            task.Wait();
        }
        catch (OperationCanceledException) {
            return Execute(ctx, script.RelativePath, onBeforeExecuteProgram);
        }
        catch (AggregateException e) {
            ExceptionDispatchInfo.Capture(e.InnerExceptions.Count != 1 ? e : e.InnerException ?? e).Throw();
        }
        catch (Exception e) {
            ExceptionDispatchInfo.Capture(e).Throw();
        }

        if (runNext) {
            Configure(FileSystem);

            return Execute(Ctx, script.RelativePath, onBeforeExecuteProgram);
        }

        return ctx;
    }

    public ExecResult ExecuteProgram(ProgramExpression node, ExecContext ctx) {
        using var _      = Timer.NewWith("Execute Program".BoldBrightGreen());
        var       result = NewResult();
        node.Execute(ctx, false);
        return result;
    }
    public ExecResult ExecuteTree(SyntaxTree tree, ExecContext ctx) {
        using var _      = Timer.NewWith("Execute Tree".BoldBrightGreen());
        var       result = NewResult();
        var       root   = tree.SyntaxRoot;

        var resultItem = root.DoExecuteMulti(ctx).ToList();

        result += resultItem.FirstOrDefault();

        return result;
    }

    public ExecResult ExecuteNodes(IEnumerable<BaseNode> nodes, ExecContext ctx) {
        var nodeList = nodes.ToList();

        using var _ = Timer.NewWith($"Execute Nodes({nodeList.Count})".BoldBrightGreen());

        var result = NewResult();

        foreach (var n in nodeList) {
            // result += Execute(n, ctx);

            Scheduler.Tick();
        }

        while (Scheduler.HasActiveCoroutines()) {
            Scheduler.Tick();
        }

        return result;
    }

    /*public ExecResult Execute(BaseNode node, ExecContext ctx) {
        ExecResult result;

        switch (node) {
            case SignalDeclaration n:
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
    }*/


}