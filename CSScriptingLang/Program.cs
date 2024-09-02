using CSScriptingLang;
using CSScriptingLang.Interpreter;
using CSScriptingLang.Lexing;
using CSScriptingLang.Parsing;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.VM;
using SymbolTable = CSScriptingLang.VM.Tables.SymbolTable;


class Program
{
    public static Lexer            lexer;
    public static Parser           parser;
    public static ByteCodeCompiler compiler;
    public static VirtualMachine   vm;
    public static Interpreter      interpreter;

    [STAThread]
    public static void Main(string[] args) {

        SymbolTable.Init();

        // var visitor = new ASTPrintingVisitor();
        // visitor.VisitProgramNode(parser.Program);
        // vm = new VirtualMachine();

        var unit = CompilationUnit.FromDirectory("./CSScriptingLang/TestingScripts");
        var main = unit.GetMainScript();


        lexer  = new Lexer(main.Source);
        parser = new Parser(lexer);

        interpreter = new Interpreter();
        interpreter.Load(parser.Program);
        interpreter.Execute();

        Console.WriteLine(new string('-', 20));

        /*
        compiler = new ByteCodeCompiler(parser.Program, lexer.ErrorWriter);

        vm.Load(compiler.Instructions);

        // Console.WriteLine(parser.Program.ToString(0));
        // Console.WriteLine(new string('-', 20));
        // compiler.Dump();
        // Console.WriteLine(compiler.ToString());
        // Console.WriteLine(new string('-', 20));

        vm.ExecuteSafe();
        */

    }
   
}


/*
Application.Init();
var window = new CliWindow(
    compiler
);
Application.Run(window);
public class CliWindow : Window
{
    public ByteCodeCompiler compiler;

    public CliWindow(ByteCodeCompiler _compiler) {
        compiler = _compiler;

        var listview = new ListView() {
            X      = 40,
            Y      = 0,
            Width  = Dim.Percent(25),
            Height = Dim.Fill(),
            Source = new ListWrapper(compiler.Instructions)
        };

        Add(listview);

        var btn = new Button {
            Text      = "Idk",
            Y         = Pos.Center(),
            X         = Pos.Center(),
            IsDefault = true
        };
        btn.Accept += (sender, args) =>
        {
            MessageBox.ErrorQuery("Error", "This is an error message", "Ok");
        };
        Add(btn);
    }
}
*/