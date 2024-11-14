using System.Diagnostics;
using System.Reflection;
using CSScriptingLang.Common.Extensions;
using CSScriptingLang.Core;
using CSScriptingLang.Core.Diagnostics;
using CSScriptingLang.Core.Logging;
using CSScriptingLang.Interpreter;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Libraries;
using CSScriptingLang.Interpreter.Modules;
using CSScriptingLang.Lexing;
using CSScriptingLang.RuntimeValues.Values;

namespace CSScriptingLang.Tests;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class UsingIncrementalSyntaxTreeEvaluation : Attribute;

public class IncrementalParserTest
{
    protected static Logger Logger => Logs.Get<IncrementalParserTest>();

    public readonly struct VariablesProxy(IncrementalParserTest test)
    {
        public Value this[string name] {
            get => test.Variables[name]?.Val;
            set {
                var v = test.Variables[name];
                if (v != null) {
                    v.Val = value;
                }
            }
        }
    }

    public InterpreterFileSystem   FileSystem  { get; set; }
    public Interpreter.Interpreter Interpreter { get; set; }
    public ExecContext             Ctx         { get; set; }

    public ModuleResolver ModuleResolver => Interpreter.ModuleResolver;
    public VariablesStack Variables      => Ctx.Variables;
    public FunctionsStack Functions      => Ctx.Functions;

    public VariablesProxy Vars => new(this);

    public string          WorkingDir   { get; set; }
    public string          MainFilePath { get; set; }
    public InterpreterFile MainFile     { get; set; }

    public IncrementalParserTest() {
        var lspTestAttr = GetType().GetCustomAttribute(typeof(LSPTestAttribute));
        if (lspTestAttr != null) {
            InterpreterConfig.Mode = InterpreterMode.Lsp;
            DiagnosticManager.AddConsumer(new LSPTestDiagnosticConsumer());
        }

        if (GetType().GetAttribute<UsingIncrementalSyntaxTreeEvaluation>(out var _)) {
            InterpreterConfig.ExecMode = InterpreterExecMode.IncrementalSyntaxTree;
        }

        Directory.SetCurrentDirectory(@"F:\c#\CSScriptingLang\CSScriptingLang.Tests");
        WorkingDir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "./", ".temp"));

        ErrorWriter.FatalErrorHandlingMethod = FatalErrorHandlingMethodType.ThrowException;
    }

    [OneTimeSetUp]
    public void StartTest() {
        Trace.Listeners.Add(new ConsoleTraceListener());
        SetupFilesystem();
    }
    
    [OneTimeTearDown]
    public void EndTest() {
        Trace.Flush();
    }

    protected virtual void SetupFilesystem() {
        var testCtx = TestContext.CurrentContext;
        var test    = testCtx.Test;

        if (!Directory.Exists(WorkingDir))
            Directory.CreateDirectory(WorkingDir);

        foreach (var file in Directory.GetFiles(WorkingDir)) {
            File.Delete(file);
        }
    }

    [SetUp]
    public virtual void SetupBase() {
        var testCtx = TestContext.CurrentContext;
        var test    = testCtx.Test;
        /*

        if (!Directory.Exists(WorkingDir))
            Directory.CreateDirectory(WorkingDir);

        // Delete all files in the directory
        foreach (var file in Directory.GetFiles(WorkingDir)) {
            File.Delete(file);
        }

        MainFilePath = Path.Combine(WorkingDir, $"{test.ClassName}{test.MethodName}.vlt");
        if (!File.Exists(MainFilePath))
            File.WriteAllText(MainFilePath, "");

        FileSystem = new InterpreterFileSystem(WorkingDir, false);
        MainFile   = FileSystem.AddFile(MainFilePath, "");
        */

        MainFilePath = Path.Combine(WorkingDir, $"{test.ClassName}{test.MethodName}.vlt");
        if (!File.Exists(MainFilePath))
            File.WriteAllText(MainFilePath, "");

        FileSystem = new InterpreterFileSystem(WorkingDir, false);
        MainFile   = FileSystem.AddFile(MainFilePath, "");

        Interpreter = new Interpreter.Interpreter(FileSystem);
        Ctx         = CSScriptingLang.Interpreter.Interpreter.Ctx;
    }

    public InterpreterFile AddModule(string path, string content) {
        if (Path.GetExtension(path) != Script.Extension)
            path = Path.ChangeExtension(path, Script.Extension);

        path = Path.Combine(FileSystem.Root, path);

        return FileSystem.AddFile(path, content);
    }

    public Script AddScriptFromOS(string path, bool isEntryModule = true, bool ignoreCache = true) {
        if (Path.GetExtension(path) != Script.Extension)
            path = Path.ChangeExtension(path, Script.Extension);

        var newPath = Path.Combine(WorkingDir, Path.GetFileName(path));
        if (File.Exists(path)) {
            File.Copy(path, newPath, true);
        }

        var file = FileSystem.AddFile(newPath, File.ReadAllText(newPath));

        Interpreter.ModuleResolver.Resolve(Ctx, file.Path, isEntryModule, ignoreCache);

        var script = Interpreter.ModuleResolver.GetScriptByAbsPath(file.Abs);

        return script;
    }

    public (Script script, InterpreterFile file) AddScript(string path, string content, bool isEntryModule = true, bool ignoreCache = true) {
        if (Path.GetExtension(path) != Script.Extension)
            path = Path.ChangeExtension(path, Script.Extension);

        path = Path.IsPathRooted(path) ? path : Path.Combine(FileSystem.Root, path!);

        var file = FileSystem.AddFile(path, content);

        Interpreter.ModuleResolver.Resolve(Ctx, file.Path, true, true);

        var script = Interpreter.ModuleResolver.GetScriptByAbsPath(file.Abs);

        return (script, file);
    }

    public void SetMainScriptContent(string str) {
        MainFile.Content = str;
        File.WriteAllText(MainFilePath, str);
    }

    public Script InitScript(string str) {
        SetMainScriptContent(str);

        Interpreter.ModuleResolver.Resolve(Ctx, MainFile.Path, true, true);

        return Interpreter.ModuleResolver.GetScriptByAbsPath(MainFile.Abs);
    }

    public Script Parse(string str, bool isEntryModule = false, bool ignoreCache = false) {
        SetMainScriptContent(str);

        Interpreter.ModuleResolver.Resolve(Ctx, MainFile.Path, isEntryModule, ignoreCache);

        var script = Interpreter.ModuleResolver.GetScriptByAbsPath(MainFile.Abs);

        return script;
    }

    public void Execute(string str, Action<Interpreter.Interpreter> setup = null) {
        SetMainScriptContent(str);

        Ctx = CSScriptingLang.Interpreter.Interpreter.Ctx;

        setup?.Invoke(Interpreter);

        try {
            Interpreter.Execute(Ctx, MainFile.Path);
        }
        catch (Exception e) when (e is BaseLanguageException or CompilationException) {
            if (!FileSystem.IsPhysical)
                File.WriteAllText(MainFilePath, str);

            CSScriptingLang.Interpreter.Interpreter.Logger.Exception(e);
        }
    }

    public Value ExecuteExpr(string expr) {
        Assert.DoesNotThrow(
            () => {
                try {
                    Execute($"var result = {expr};");
                }
                catch (Exception e) when (e is BaseLanguageException) {
                    CSScriptingLang.Interpreter.Interpreter.Logger.Exception(e);
                }
            }
        );

        var result = Ctx.Variables["result"];

        Logger.Debug($"Expression({expr}) result: {Lib_Inspect.InspectString(result?.Val)}");

        return result?.Val;
    }
}