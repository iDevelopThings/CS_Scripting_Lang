using CSScriptingLang;
using CSScriptingLang.Interpreter;
using CSScriptingLang.Lexing;
using CSScriptingLang.Parsing;
using CSScriptingLang.VM;
using SymbolTable = CSScriptingLang.VM.Tables.SymbolTable;

namespace LanguageTests.InterpreterTests;

public class BaseCompilerTest : IDisposable
{
    public Lexer            lexer;
    public Parser           parser;
    public ByteCodeCompiler compiler;
    public VirtualMachine   vm;
    public Interpreter      interpreter;

    public BaseCompilerTest() {
        SymbolTable.Init();
    }

    public BaseCompilerTest(string directoryPath) {
        SymbolTable.Init();

        var unit = CompilationUnit.FromDirectory(directoryPath);
        var main = unit.GetMainScript();

        vm          = new VirtualMachine();
        interpreter = new Interpreter();

        InitWith(main.Source);
    }

    public void InitWith(string source) {
        lexer  = new Lexer(source);
        parser = new Parser(lexer);
        // compiler = new ByteCodeCompiler(parser.Program, lexer.ErrorWriter);
    }

    public void Execute(string source) {
        InitWith(source);
        Execute();
    }

    public Interpreter StandaloneExecute(string source) {
        var l = new Lexer(source);
        var p = new Parser(l);

        Console.WriteLine(p.Program.ToString(0));
        Console.WriteLine(new string('-', 20));

        var i = new Interpreter();
        i.Load(p.Program);
        i.Execute();

        return i;
    }

    public (Interpreter, Symbol) ExecuteSingleExpression(string expr) {
        var interp = StandaloneExecute($@"
            var result = {expr};
        ");

        return (interp, interp.Symbols["result"]);
    }

    public void Execute() {

        Console.WriteLine(parser.Program.ToString(0));
        Console.WriteLine(new string('-', 20));

        // if (InterpreterExecutionContext.ContextStack.Count != 0) {
        // InterpreterExecutionContext.Pop();
        // }

        interpreter = new Interpreter();
        interpreter.Load(parser.Program);
        interpreter.Execute();

        // vm = new VirtualMachine();
        // vm.Load(compiler.Instructions);
        // vm.ExecuteSafe();
    }

    public void Dispose() {
        if (interpreter != null) {
            if (interpreter.ContextStack.Count != 0) {
                interpreter.PopFrame();
            }
        }
    }
}