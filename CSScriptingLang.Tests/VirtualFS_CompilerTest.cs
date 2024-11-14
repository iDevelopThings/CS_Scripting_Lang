using System.Diagnostics;
using CSScriptingLang.Core.Diagnostics;
using CSScriptingLang.Interpreter;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Modules;
using CSScriptingLang.Lexing;
using CSScriptingLang.RuntimeValues.Values;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Server;
using Diagnostic = CSScriptingLang.Core.Diagnostics.Diagnostic;

namespace CSScriptingLang.Tests;

public class VirtualFS_CompilerTest
{
    public readonly struct VariablesProxy(VirtualFS_CompilerTest test)
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

    private bool _trackFunctionFrames = false;

    public List<Frame> FunctionFramesPushed { get; } = new();
    public List<Frame> FunctionFramesPopped { get; } = new();

    public VirtualFS_CompilerTest() {
        Directory.SetCurrentDirectory(@"F:\c#\CSScriptingLang\CSScriptingLang.Tests");
        WorkingDir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "./", ".temp"));

        ErrorWriter.FatalErrorHandlingMethod = FatalErrorHandlingMethodType.ThrowException;

        InterpreterEvents.OnFunctionFramePushed += frame => {
            if (_trackFunctionFrames) {
                FunctionFramesPushed.Add(frame);
            }
        };
        InterpreterEvents.OnFunctionFramePopped += frame => {
            if (_trackFunctionFrames) {
                FunctionFramesPopped.Add(frame);
            }
        };
    }

    [OneTimeSetUp]
    public void StartTest() {
        Trace.Listeners.Add(new ConsoleTraceListener());
    }
    [OneTimeTearDown]
    public void EndTest() {
        Trace.Flush();
    }

    [SetUp]
    public virtual void SetupBase() {
        FunctionFramesPushed.Clear();
        FunctionFramesPopped.Clear();

        if (!Directory.Exists(WorkingDir)) {
            Directory.CreateDirectory(WorkingDir);
        }

        // Delete all files in the directory
        foreach (var file in Directory.GetFiles(WorkingDir)) {
            File.Delete(file);
        }

        MainFilePath = Path.Combine(WorkingDir, $"{TestContext.CurrentContext.Test.ClassName}{TestContext.CurrentContext.Test.MethodName}.vlt");
        if (!File.Exists(MainFilePath)) {
            File.WriteAllText(MainFilePath, "");
        }

        FileSystem = new InterpreterFileSystem(WorkingDir, false);
        MainFile   = FileSystem.AddFile(MainFilePath, "");

        Interpreter = new Interpreter.Interpreter(FileSystem);
        Ctx         = CSScriptingLang.Interpreter.Interpreter.Ctx;
    }

    public InterpreterFile AddModule(string path, string content) {
        if (Path.GetExtension(path) != Script.Extension) {
            path = Path.ChangeExtension(path, Script.Extension);
        }
        path = Path.Combine(FileSystem.Root, path);

        return FileSystem.AddFile(path, content);
    }

    public Script InitScript(string str) {
        // if (!str.Contains("module \"main\";") && !str.Contains("module 'main';")) {
            // str = $"module \"main\";\n{str}";
        // }

        MainFile.Content = str;
        
        File.WriteAllText(MainFilePath, str);
        
        Interpreter.ModuleResolver.Resolve(Ctx, MainFile.Path, true, true);

        return Interpreter.ModuleResolver.GetScriptByAbsPath(MainFile.Abs);
    }

    public void Execute(string str, Action<Interpreter.Interpreter> setup = null) {
        if (!str.Contains("module \"main\";") && !str.Contains("module 'main';")) {
            str = $"module \"main\";\n{str}";
        }

        MainFile.Content = str;

        setup?.Invoke(Interpreter);


        try {
            // Ctx = Interpreter.ExecuteStandalone(Ctx);
            Interpreter.Execute(Ctx, MainFile.Path);
            // var module = ModuleResolver.Resolve(Ctx, MainFile.Path);
        }
        catch (Exception e) when (e is BaseLanguageException or CompilationException) {
            if (!FileSystem.IsPhysical) {
                File.WriteAllText(MainFilePath, str);
            }
            CSScriptingLang.Interpreter.Interpreter.Logger.Exception(e);
        }
    }

    public Value ExecuteSimpleExpression(string expr) {
        Assert.DoesNotThrow(() => {
            try {
                Execute(
                    $"""
                     module "main";
                     var result = {expr};
                     """
                );
            }
            catch (Exception e) when (e is BaseLanguageException) {
                CSScriptingLang.Interpreter.Interpreter.Logger.Exception(e);
            }
        });

        var result = Ctx.Variables["result"];

        return result?.Val;
    }

}


