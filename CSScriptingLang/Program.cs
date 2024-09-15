using CommandLine;
using CSScriptingLang.Core;
using CSScriptingLang.Core.FileSystem;
using CSScriptingLang.Interpreter;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.REPL;
using CSScriptingLang.Lexing;
using CSScriptingLang.Utils;
using Spectre.Console;


public class BaseOptions
{
    public static string DefaultWorkingDirectory() {
        return System.IO.Path.Join(Directory.GetCurrentDirectory(), "CSScriptingLang", "TestingScripts");
    }
    
    [Option('p', "wd", Required = false, HelpText = "Set the project directory.")]
    public string Path { get; set; }

    [Option('m', "module", Required = false, HelpText = "Sets the module/script to run.")]
    public string Module { get; set; }

    public void Apply() {
        
        // check if `opts.Path` is an absolute path, ie starts with a drive letter
        if (System.IO.Path.IsPathRooted(Path)) {
            if (!Directory.Exists(Path)) {
                Console.WriteLine($"Path '{Path}' does not exist");
                return;
            }
        } else {
            Path = System.IO.Path.Join(Directory.GetCurrentDirectory(), Path);
        }

        Path   ??= System.IO.Path.GetFullPath(DefaultWorkingDirectory());
        Module ??= "main";

        InterpreterConfig.Apply(this);
    }
}

[Verb("run", true, ["r"], HelpText = "Run a script")]
public class RunOptions : BaseOptions
{
    [Option("debug:func", HelpText = "Enable function call debugging.", Required = false)]
    public bool FunctionCallDebugging { get; set; }
    
    [Option("debug:funcs", HelpText = "Only debug the specified function calls.", Required = false)]
    public IEnumerable<string> FunctionCallsToDebug { get; set; }
    
    [Option("debug:funcs_exclude", HelpText = "Exclude the specified function calls from debugging.", Required = false)]
    public IEnumerable<string> FunctionCallsToExclude { get; set; }
    
    [Option("fatal", Required = false, HelpText = "Set the fatal handling method.")]
    public FatalErrorHandlingMethodType FatalErrorHandlingMethod { get; set; } = FatalErrorHandlingMethodType.Exit;

}

[Verb("repl", HelpText = "Start the REPL")]
public class ReplOptions : BaseOptions
{
    [Option("fatal", Required = false, HelpText = "Set the fatal handling method.")]
    public FatalErrorHandlingMethodType FatalErrorHandlingMethod { get; set; } = FatalErrorHandlingMethodType.ThrowException;
}

class ProgramEntry
{
    public static InterpreterFileSystem    FileSystem  { get; set; }
    public static Interpreter   Interpreter { get; set; }
    public static ExecContext   Ctx         { get; set; }
    public static ReplProcessor REPL        { get; set; }

    public static void Main(string[] args) {

        // var code = AnsiConsole.Ask<string>("> ");

        Parser.Default.ParseArguments<RunOptions, ReplOptions>(args)
           .WithParsed<RunOptions>(RunModule)
           .WithParsed<ReplOptions>(StartRepl)
           .WithNotParsed(HandleParseError);
    }

    private static void RunModule(RunOptions opts) {
        opts.Apply();
        
        FileSystem  = new InterpreterFileSystem(opts.Path, true);
        Interpreter = new Interpreter(FileSystem);
        Ctx         = Interpreter.GetNewExecContext();

        try {
            // var mainModule = Interpreter.ModuleRegistry.LoadModule(opts.Module);
            Interpreter.Execute(Ctx, () => {
                Interpreter.Logger.Info("Executing");
                Interpreter.Logger.Info($" - module={Interpreter.Module.Name} Script({Interpreter.Module.MainScript?.Name})");
                Interpreter.Logger.Info($" - script={Interpreter.Module.MainScript?.FilePath}:0");
            });
        }
        catch (BaseLanguageException e) {
            Interpreter.Logger.Exception(e);
        }
    }

    private static void StartRepl(ReplOptions opts) {
        FileSystem  = new InterpreterFileSystem(Directory.GetCurrentDirectory(), false);
        Interpreter = new Interpreter(FileSystem);

        REPL = new ReplProcessor(Interpreter);
        REPL.Start().Wait();
    }


    private static void HandleParseError(IEnumerable<Error> errs) {
        Console.WriteLine("Error parsing arguments");
        foreach (var err in errs) {
            Console.WriteLine(err);
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