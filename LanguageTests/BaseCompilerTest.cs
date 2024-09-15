using System.Diagnostics;
using CSScriptingLang.Core.FileSystem;
using CSScriptingLang.Interpreter;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.Interpreter.Modules;
using CSScriptingLang.Lexing;
using CSScriptingLang.Parsing;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.RuntimeValues.Values;

namespace LanguageTests;

public class BaseCompilerTest
{
    public InterpreterFileSystem FileSystem  { get; set; }
    public Interpreter           Interpreter { get; set; }
    public ExecContext           Ctx         { get; set; }

    public ModuleResolver ModuleResolver => Interpreter.ModuleResolver;
    public VariablesStack Variables      => Ctx.Variables;
    public FunctionsStack Functions      => Ctx.Functions;

    public string          MainFilePath { get; set; }
    public InterpreterFile MainFile     { get; set; }

    private bool _trackFunctionFrames;
#pragma warning disable CS0414 // Field is assigned but its value is never used
    private bool _logScopeEvents = false;
#pragma warning restore CS0414 // Field is assigned but its value is never used

    public List<Frame> FunctionFramesPushed { get; } = new();
    public List<Frame> FunctionFramesPopped { get; } = new();

    public BaseCompilerTest() {
        Directory.SetCurrentDirectory("F:\\c#\\CSScriptingLang\\LanguageTests");

        ErrorWriter.FatalErrorHandlingMethod = FatalErrorHandlingMethodType.ThrowException;

        InterpreterEvents.OnFunctionFramePushed += frame => {
            if (_trackFunctionFrames) {
                // if (_logScopeEvents)
                //     Console.WriteLine($"Function Frame Pushed: {frame.Name}");
                FunctionFramesPushed.Add(frame);
            }
        };
        InterpreterEvents.OnFunctionFramePopped += frame => {
            if (_trackFunctionFrames) {
                // if (_logScopeEvents)
                //     Console.WriteLine($"Function Frame Popped: {frame.Name}");
                FunctionFramesPopped.Add(frame);
            }
        };
    }

    public BaseCompilerTest TrackFunctionFrames(bool value = true) {
        _trackFunctionFrames = value;
        return this;
    }

    public Frame GetPushedFrame(string name) {
        return FunctionFramesPushed.FirstOrDefault(f => f.Name == name);
    }
    public Frame GetPoppedFrame(string name) {
        return FunctionFramesPopped.FirstOrDefault(f => f.Name == name);
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
    public void SetupBase() {
        TypeTable.Current = new TypeTable(null, true);

        FunctionFramesPushed.Clear();
        FunctionFramesPopped.Clear();

        SetupCompiler();
    }

    public void SetupCompiler(bool isPhysical = false, string rootPath = "./") {
        var scriptsDir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), rootPath, ".temp"));
        rootPath = rootPath == "./" ? scriptsDir : rootPath;

        if (rootPath == scriptsDir) {
            if (!Directory.Exists(scriptsDir)) {
                Directory.CreateDirectory(scriptsDir);
            }
        }

        if (!isPhysical) {
            // Delete all files in the directory
            foreach (var file in Directory.GetFiles(scriptsDir)) {
                File.Delete(file);
            }

            MainFilePath = Path.Combine(rootPath, $"{TestContext.CurrentContext.Test.ClassName}{TestContext.CurrentContext.Test.MethodName}.js");
            if (!File.Exists(MainFilePath)) {
                File.WriteAllText(MainFilePath, "");
            }
        }

        FileSystem = new InterpreterFileSystem(rootPath, isPhysical);

        if (!isPhysical) {
            MainFile = FileSystem.AddFile(MainFilePath, "");
        }

        Interpreter = new Interpreter(FileSystem);
        Ctx         = Interpreter.GetNewExecContext();

    }

    public ProgramExpression Parse(string source, bool printParseTree = true) {
        var standaloneParser = new StandaloneParser(source);
        var program          = standaloneParser.Parse(false);

        if (printParseTree) {
            Console.WriteLine(program.ToString(0));
            Console.WriteLine(new string('-', 20));
        }

        return program;
    }

    public Expression ParseExpression(string source) {
        var standaloneParser = new StandaloneParser(source);
        var program          = standaloneParser.ParseExpressionNodes();

        return program.First();
    }


    public Interpreter Execute(string str, bool printParseTree = true, string moduleName = "main", Action<Interpreter> setup = null) {
        if (!str.Contains("module \"main\";") && !str.Contains("module 'main';")) {
            str = $"module \"{moduleName}\";\n{str}";
        }

        // Assert.DoesNotThrow(() => {
        try {
            ExecuteNewModuleSystem(str, printParseTree, moduleName, setup);
        }
        catch (Exception e) when (e is BaseLanguageException || e is CompilationException) {
            if (!FileSystem.IsPhysical) {
                File.WriteAllText(MainFilePath, str);
            }

            Interpreter.Logger.Exception(e);

        }
        // });

        return Interpreter;
    }

    public Interpreter ExecuteFromDiskModules(string source, string diskRootPath = "./", string mainModuleName = "main", Action<Interpreter> setup = null) {
        Assert.DoesNotThrow(() => {
            try {
                SetupCompiler(true, diskRootPath);

                if (!string.IsNullOrWhiteSpace(source)) {
                    Interpreter.FileSystem.AddFile($"{mainModuleName}.js", source);
                }

                Ctx = new ExecContext();

                ModuleResolver.Load(Ctx);

                var mModule = ModuleResolver.MainModule;
                var mScript = ModuleResolver.MainScript;

                Assert.Multiple(() => {
                    Assert.That(mModule, Is.Not.Null);
                    Assert.That(mScript, Is.Not.Null);
                });

                setup?.Invoke(Interpreter);

                Ctx = Interpreter.Execute(Ctx);
            }
            catch (Exception e) when (e is BaseLanguageException) {
                // TestContext.Error.WriteLine(e.Message);
                // throw new InterpreterTestExecutionException(e);
                Interpreter.Logger.Exception(e);
            }
        });


        return Interpreter;
    }

    public Interpreter ExecuteNewModuleSystem(string source, bool printParseTree = true, string moduleName = "main", Action<Interpreter> setup = null) {
        if (FileSystem.IsPhysical) {
            SetupCompiler(false);
        } else {
            MainFile.Content = source;
        }

        ModuleResolver.Load(Ctx);

        Ctx.Module = ModuleResolver.MainModule;

        setup?.Invoke(Interpreter);

        Ctx = Interpreter.ExecuteStandalone(Ctx);

        return Interpreter;
    }


    public Value ExecuteSimpleExpression(string expr) {
        Assert.DoesNotThrow(() => {
            try {
                ExecuteNewModuleSystem(
                    $"""
                     module "main";
                     var result = {expr};
                     """
                );
            }
            catch (Exception e) when (e is BaseLanguageException) {
                Interpreter.Logger.Exception(e);
            }
        });

        var result = Ctx.Variables["result"];

        return result?.Val;
    }


}