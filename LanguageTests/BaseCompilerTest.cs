using CSScriptingLang.Core.FileSystem;
using CSScriptingLang.Interpreter;
using CSScriptingLang.Lexing;
using CSScriptingLang.Parsing;
using CSScriptingLang.Parsing.AST;

namespace LanguageTests;

public class BaseCompilerTest
{
    public FileSystem  FileSystem  { get; set; }
    public Interpreter Interpreter { get; set; }

    public InterpreterExecutionContext Context => Interpreter.Context;
    public SymbolTable                 Symbols => Interpreter.Symbols;

    private bool _trackFunctionFrames;
    private bool _logScopeEvents = false;

    public List<FunctionExecutionFrame> FunctionFramesPushed { get; } = new();
    public List<FunctionExecutionFrame> FunctionFramesPopped { get; } = new();

    public BaseCompilerTest() {
        InterpreterEvents.OnExecutionScopePushed += context => {
            if (_logScopeEvents)
                Console.WriteLine($"Scope Pushed: {context.Module.Name}");
        };
        InterpreterEvents.OnExecutionScopePopped += context => {
            if (_logScopeEvents)
                Console.WriteLine($"Scope Popped: {context.Module.Name}");
        };
        InterpreterEvents.OnFunctionFramePushed += frame => {
            if (_trackFunctionFrames) {
                if (_logScopeEvents)
                    Console.WriteLine($"Function Frame Pushed: {frame.Name}");
                FunctionFramesPushed.Add(frame);
            }
        };
        InterpreterEvents.OnFunctionFramePopped += frame => {
            if (_trackFunctionFrames) {
                if (_logScopeEvents)
                    Console.WriteLine($"Function Frame Popped: {frame.Name}");
                FunctionFramesPopped.Add(frame);
            }
        };
    }

    public BaseCompilerTest TrackFunctionFrames(bool value = true) {
        _trackFunctionFrames = value;
        return this;
    }

    public FunctionExecutionFrame GetPushedFrame(string name) {
        return FunctionFramesPushed.FirstOrDefault(f => f.Name == name);
    }
    public FunctionExecutionFrame GetPoppedFrame(string name) {
        return FunctionFramesPopped.FirstOrDefault(f => f.Name == name);
    }

    public void SetupCompiler(bool isPhysical = false, string rootPath = "./") {
        FileSystem  = new FileSystem(rootPath, isPhysical);
        Interpreter = new Interpreter(FileSystem);
    }

    [SetUp]
    public void SetupBase() {
        FunctionFramesPushed.Clear();
        FunctionFramesPopped.Clear();

        SetupCompiler();
    }

    public (Lexer, Parser, ProgramNode) Parse(string source, bool printParseTree = true) {
        var l = new Lexer(source);
        var p = new Parser(l);

        if (printParseTree) {
            Console.WriteLine(p.Program.ToString(0));
            Console.WriteLine(new string('-', 20));
        }

        return (l, p, p.Program);
    }

    public Interpreter Execute(string source, bool printParseTree = true, string moduleName = "main", Action<Interpreter> setup = null) {
        try {
            return ExecuteModules(source, printParseTree, moduleName, setup);
        }
        catch (Exception e) {
            throw;
        }
    }

    public Interpreter ExecuteFromDiskModules(string source, string diskRootPath, string mainModuleName = "main", Action<Interpreter> setup = null) {
        SetupCompiler(true, diskRootPath);

        if (!string.IsNullOrWhiteSpace(source)) {
            Interpreter.FileSystem.CreateFile($"{mainModuleName}.js", source);
        }

        var mainModule = Interpreter.ModuleRegistry.LoadModule(mainModuleName);

        setup?.Invoke(Interpreter);
        Interpreter.Execute(mainModule);

        return Interpreter;
    }

    public Interpreter ExecuteModules(string source, bool printParseTree = true, string moduleName = "main", Action<Interpreter> setup = null) {
        if (FileSystem.IsPhysical) {
            SetupCompiler(false);
        }

        Interpreter.FileSystem.CreateFile($"{moduleName}.js", source);

        var mainModule = Interpreter.ModuleRegistry.LoadModule(moduleName);

        setup?.Invoke(Interpreter);
        Interpreter.Execute(mainModule);

        return Interpreter;
    }

    public (Interpreter, Symbol) ExecuteSingleExpression(string expr, bool printParseTree = true) {
        var interp = Execute($@"
            var result = {expr};
        ", printParseTree);

        return (interp, interp.Symbols["result"]);
    }
}