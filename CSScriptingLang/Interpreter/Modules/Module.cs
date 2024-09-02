using CSScriptingLang.Parsing;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.RuntimeValues;
using CSScriptingLang.Utils;

namespace CSScriptingLang.Interpreter.Modules;

public class Module : BaseNode
{
    public string Name { get; }

    public ModuleRegistry Registry { get; set; }

    public Dictionary<string, Script> Scripts { get; } = new();

    private Dictionary<string, FunctionDeclarationNode> Functions { get; } = new();
    private Dictionary<string, VariableDeclarationNode> Variables { get; } = new();

    public Action<Module, string, FunctionDeclarationNode> OnFunctionRegistered;
    public Action<Module, string, VariableDeclarationNode> OnVariableRegistered;

    public  Action<ProgramNode> OnProgramSet;
    private ProgramNode         _program;
    public ProgramNode Program {
        get => _program;
        set {
            _program = value;
            OnProgramSet?.Invoke(value);
        }
    }

    public IEnumerable<ITopLevelDeclarationNode> Declarations => Program?.Cursor.All.Of<ITopLevelDeclarationNode>() ?? [];

    public bool IsCompileable { get; set; }
    public bool Compiled      { get; set; }

    public Module(string name) {
        Name = name;
    }

    public virtual void RegisterGlobals() { }

    public void Register(FunctionDeclarationNode function) {
        Functions[function.Name] = function;
        OnFunctionRegistered?.Invoke(this, function.Name, function);
    }
    public void Register(VariableDeclarationNode variable) {
        Variables[variable.VariableName] = variable;
        OnVariableRegistered?.Invoke(this, variable.VariableName, variable);
    }
    public string PrefixName(string ToPrefix) {
        return $"{Name}.{ToPrefix}";
    }

    public bool HasVariable(string name) => Variables.ContainsKey(name);
    public bool HasFunction(string name) => Functions.ContainsKey(name);

    public bool                    GetVariable(string name, out VariableDeclarationNode variable) => Variables.TryGetValue(name, out variable);
    public VariableDeclarationNode GetVariable(string name) => Variables[name];

    public bool                    GetFunction(string name, out FunctionDeclarationNode function) => Functions.TryGetValue(name, out function);
    public FunctionDeclarationNode GetFunction(string name) => Functions[name];
    public void SetDeclarations() {
        if (Program == null)
            return;

        foreach (var declNode in Program.Cursor.All.Of<ITopLevelDeclarationNode>()) {
            if (declNode is FunctionDeclarationNode fn) {
                Register(fn);
            } else if (declNode is VariableDeclarationNode var) {
                Register(var);
            }
        }
    }
    public void Compile() {
        using var _ = ScopeTimer.NewWith($"Compiling module '{Name.BoldBrightWhite()}'");

        if (!IsCompileable)
            return;
        if (Compiled)
            return;

        Compiled = true;

        var program = new ProgramNode();

        foreach (var pair in Scripts) {
            var parser = new Parser(pair.Value);
            pair.Value.Program = parser.Program;
            pair.Value.Parser  = parser;
        }

        foreach (var script in Scripts.Values) {
            program.Combine(script.Program);
        }

        program.Parent = this;
        Program        = program;
    }
}

public class GlobalModule : Module
{
    public static GlobalModule Instance { get; } = new();

    public GlobalModule() : base("global") { }

    public override void RegisterGlobals() {
        Register(new FunctionDeclarationNode("print") {
            IsNative = true,
            NativeFunction = (interpreter, frame) => {
                var argCount = frame.Args.Count;
                if (argCount == 0) {
                    Console.WriteLine();
                    return;
                }

                if (frame.Args[0].Type.Type == RTVT.String && argCount == 1) {
                    Console.WriteLine(frame.Args[0].Value.Inspect());
                    return;
                }

                if (frame.Args[0].Type.Type == RTVT.String) {
                    var paramsObj = frame.Args.Skip(1)
                       .Select(arg => arg.Value.Inspect())
                       .ToArray();
                    var str       = frame.Args[0].Value.Inspect();
                    var formatted = string.Format(str, paramsObj);
                    Console.WriteLine(formatted);
                    return;
                }

                for (int i = 0; i < argCount; i++) {
                    Console.Write(frame.Args[i].Value.Inspect());
                    if (i < argCount - 1)
                        Console.Write(" ");
                }

                if (argCount > 0)
                    Console.WriteLine();
            }
        });

    }
}