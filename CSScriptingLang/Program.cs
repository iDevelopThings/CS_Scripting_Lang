using CSScriptingLang.Core.FileSystem;
using CSScriptingLang.Interpreter;


class Program
{
    public static FileSystem  FileSystem  { get; set; }
    public static Interpreter Interpreter { get; set; }

    public static void Main(string[] args) {
        FileSystem = new FileSystem(Path.Join(Directory.GetCurrentDirectory(), "CSScriptingLang", "TestingScripts"), true);
        Interpreter = new Interpreter(FileSystem);

        var mainModule = Interpreter.ModuleRegistry.LoadModule(args[0] ?? "main");
        
        try {
            Interpreter.Execute(mainModule);
        }
        catch (Exception e) {
            Console.WriteLine(e.Message);
        }
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