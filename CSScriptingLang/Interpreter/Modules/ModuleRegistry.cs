using CSScriptingLang.Core.FileSystem;
using CSScriptingLang.Parsing.AST;

namespace CSScriptingLang.Interpreter.Modules;

public class ModuleRegistry
{
    public InterpreterExecutionContext Context { get; set; }

    public FileSystem FileSystem => Context.Interpreter.FileSystem;

    private Dictionary<string, Module> _modules = new();

    private Dictionary<string, FunctionDeclarationNode> AllFunctions { get; } = new();
    private Dictionary<string, VariableDeclarationNode> AllVariables { get; } = new();

    public int NumModules => _modules.Count;

    public ModuleRegistry(InterpreterExecutionContext context) {
        Context = context;

        RegisterModule("global", GlobalModule.Instance);

        GlobalModule.Instance.RegisterGlobals();
    }


    private Module RegisterModule(string name, Module module) {
        module.Registry = this;

        module.OnFunctionRegistered += (mod, n, decl) => {
            AllFunctions[mod.PrefixName(n)] = decl;
        };
        module.OnVariableRegistered += (mod, n, decl) => {
            AllVariables[mod.PrefixName(n)] = decl;
        };

        module.SetDeclarations();
        if (module.Program == null) {
            module.OnProgramSet += program => {
                module.SetDeclarations();
            };
        }

        _modules[name] = module;

        return module;
    }
    public Module RegisterModule(string name, ProgramNode program) {
        var module = new Module(name) {
            Program  = program,
            Registry = this
        };
        return RegisterModule(name, module);
    }

    public Module LoadModule(string name) {
        List<IVirtualFile> scripts = new();

        if (FileSystem.DirectoryExists(name)) {
            var dir = FileSystem.GetDirectory(name);
            scripts = dir.Files(true).Where(f => f.Ext == ".js").ToList();
        } else if (FileSystem.FileExists($"{name}.js")) {
            scripts.Add(FileSystem.GetFile($"{name}.js"));
        }

        if (scripts.Count == 0) {
            throw new Exception($"Module '{name}' not found.");
        }

        var module = new Module(name) {
            IsCompileable = true,
        };
        foreach (var scriptFile in scripts) {
            var script = new Script {
                File   = scriptFile,
                Module = module
            };
            module.Scripts.Add(script.File.NameWithoutExt, script);
        }

        return RegisterModule(name, module);
    }

    public bool HasModule(string name) {
        return _modules.ContainsKey(name);
    }
    public bool TryGetModule(string name, out Module module) {
        return _modules.TryGetValue(name, out module);
    }
    public Module GetModule(string name) {
        if (_modules.TryGetValue(name, out var module)) {
            return module;
        }

        throw new Exception($"Module '{name}' not found.");
    }

    public IEnumerable<Module> GetModules() {
        return _modules.Values;
    }

    public bool TryGetFunction(string name, out FunctionDeclarationNode decl) {
        if (AllFunctions.TryGetValue(name, out decl)) {
            return true;
        }

        foreach (var fn in AllFunctions) {
            if (fn.Key.EndsWith(name)) {
                decl = fn.Value;
                return true;
            }
        }

        return false;
    }
    public bool TryGetVariable(string name, out VariableDeclarationNode decl) {
        if (AllVariables.TryGetValue(name, out decl)) {
            return true;
        }

        foreach (var var in AllVariables) {
            if (var.Key.EndsWith(name)) {
                decl = var.Value;
                return true;
            }
        }

        return false;
    }

    public IEnumerable<KeyValuePair<string, FunctionDeclarationNode>> Functions() {
        foreach (var pair in AllFunctions) {
            yield return pair;
        }
    }
    public IEnumerable<KeyValuePair<string, VariableDeclarationNode>> Variables() {
        foreach (var pair in AllVariables) {
            yield return pair;
        }
    }
}