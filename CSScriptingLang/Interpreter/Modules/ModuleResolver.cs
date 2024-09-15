using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Lexing;
using CSScriptingLang.Parsing;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.RuntimeValues.Values;
using CSScriptingLang.Utils;
using Engine.Engine.Logging;

namespace CSScriptingLang.Interpreter.Modules;

public class CombinedModuleDeclarations
{
    private readonly ModuleResolver _resolver;
    public CombinedModuleDeclarations(ModuleResolver moduleResolver) {
        _resolver = moduleResolver;
    }
    public Dictionary<string, RuntimeTypeInfo_Struct>      Structs                  => _resolver.Modules.Values.SelectMany(m => m.Declarations.StructTypes).ToDictionary(s => s.Name);
    public Dictionary<string, RuntimeType>                 Interfaces               => _resolver.Modules.Values.SelectMany(m => m.Declarations.InterfaceTypes).ToDictionary(i => i.Name);
    public Dictionary<string, RuntimeTypeInfo_Signal>      Signals                  => _resolver.Modules.Values.SelectMany(m => m.Declarations.SignalTypes).ToDictionary(s => s.Name);
    public Dictionary<string, Value>                       Functions                => _resolver.Modules.Values.SelectMany(m => m.Declarations.FunctionTypes).ToDictionary(f => f.As.Fn().Name);
    public Dictionary<string, DefDeclaration_FunctionNode> DefFunctionsDeclarations => _resolver.Modules.Values.SelectMany(m => m.Declarations.DefFunctionDeclarations).ToDictionary(f => f.Name);
    public Dictionary<string, VariableSymbol>              Variables                => _resolver.Modules.Values.SelectMany(m => m.Declarations.VariableDeclarations).ToDictionary(v => v.Name);
}

public class ModuleResolver
{
    public static Logger Logger = Logs.Get<ModuleResolver>(LogLevel.Warning);

    public ClassScopedTimerInst<ModuleResolver> Timer = ClassScopedTimerInst<ModuleResolver>.Create(Logger)
       .SetColorFn(n => n.BoldBrightBlue())
       .SetName("ModuleResolver");

    private static int ScriptIdCounter = 0;

    private InterpreterFileSystem FileSystem { get; set; }

    public static Dictionary<int, Script> ScriptIdMap { get; set; } = new();

    public Dictionary<string, Module> Modules           { get; set; } = new();
    public Dictionary<string, Module> ModulesByFilePath { get; set; } = new();

    public IEnumerable<Script>        AllScripts       => Modules.Values.SelectMany(m => m.Scripts);
    public Dictionary<string, Script> ScriptsByAbsPath => AllScripts.ToDictionary(s => s.File.Abs);

    // File Path -> ModuleTemporaryData
    private Dictionary<string, Script> LoadCache   { get; set; } = new();
    private Queue<Script>              ToLoadQueue { get; set; } = new();

    public IEnumerable<Module> ModulesByDepth => Modules.Values.OrderByDescending(m => m.DirectoryPath.Length);

    public static CombinedModuleDeclarations Declarations;

    public Module MainModule { get; set; }
    public Script MainScript { get; set; }

    public ModuleResolver(InterpreterFileSystem fs) {
        FileSystem   = fs;
        Declarations = new CombinedModuleDeclarations(this);
    }

    public void FindAllFromPath(string path) {
        // var pattern = $"**/*{Script.Extension}";
        // if (!string.IsNullOrEmpty(path)) {
        //     pattern = $"{path}/{pattern}";
        // }

        var files = FileSystem.AllAtPath(path).ToList();

        // var files2 = FileSystem.List(Path.Combine(path, $"**/*{Script.Extension}")).ToList();
        foreach (var file in files) {
            QueueFile(file);
        }

    }

    public bool QueueFile(InterpreterFile file) {
        if (LoadCache.ContainsKey(file.Abs)) {
            return false;
        }

        var modData = new Script(file, ScriptIdCounter++);

        ScriptIdMap[modData.Id] = modData;

        LoadCache[file.Rel] = modData;
        ToLoadQueue.Enqueue(modData);

        return true;
    }

    public void LoadFile(InterpreterFile file) {
        if (!QueueFile(file))
            return;

        ProcessLoadQueue(Interpreter.Ctx);
    }

    public void Load(ExecContext ctx) {
        using var _ = Timer.NewWith("Load Modules");

        // Loads all modules from the current directory
        FindAllFromPath("");
        ProcessLoadQueue(ctx);
    }

    public void ProcessLoadQueue(ExecContext ctx) {
        var scripts = new HashSet<Script>();

        ProcessLoadQueue(scripts);
        ProcessLoadedScripts(scripts);

        foreach (var mod in Modules.Values) {
            ModulesByFilePath[mod.DirectoryPath] = mod;
        }

        ParseModuleTrees(ctx);

        if (Modules.Values.Count == 0) {
            return;
        }

        MainModule = Modules.Values.FirstOrDefault(m => m.IsMainModule);

        MainModule!.MainScript = MainModule.TryGetMainScript();

        MainScript = MainModule?.MainScript;

    }

    public void SetMainScript(Script script) {
        if (script == null)
            return;

        MainModule            = script.Module;
        MainModule.MainScript = script;
    }

    private void ProcessLoadQueue(HashSet<Script> scripts) {
        while (ToLoadQueue.Count > 0) {
            var data = ToLoadQueue.Dequeue();

            if (scripts.Contains(data)) {
                continue;
            }

            var parser = new ScriptImportsParser(data);
            parser.Parse();

            if (data.AstData?.ImportStatements != null) {
                foreach (var import in data.AstData.ImportStatements.Nodes) {
                    FindAllFromPath(Path.Combine(Path.GetDirectoryName(data.FilePath)!, import.Path.NativeValue));
                }
            }

            scripts.Add(data);
        }
    }

    private void ProcessLoadedScripts(HashSet<Script> scripts) {
        foreach (var script in scripts) {
            if (!script.AstData.DidParseImports) {
                throw new Exception($"Failed to parse imports for module {script.FilePath}");
            }

            if (string.IsNullOrEmpty(script.AstData.ModuleName)) {
                throw new DeclarationException($"Module {script.FilePath} does not have a module declaration", null, script);
            }

            var module = GetOrCreateModule(script.AstData.ModuleName);
            module.AddScript(script);
        }
    }

    private void ParseModuleTrees(ExecContext ctx) {
        using var _ = Timer.NewWith("Parse Module Trees");

        foreach (var script in AllScripts) {
            using var __ = Timer.NewWith($"Process Script({script.RelativePath})");

            ctx.Module = script.Module;

            script.Parse();
            script.RegisterDeclarations(ctx);
        }

        // ctx.Module = MainModule;

        // BaseValue.LoadNativeBindings();
    }


    private Module GetOrCreateModule(string name) {
        if (Modules.TryGetValue(name, out var module)) {
            return module;
        }

        module        = new Module(name);
        Modules[name] = module;

        return module;
    }

    public Module Get(string    moduleName)                    => Modules[moduleName];
    public bool   TryGet(string moduleName, out Module module) => Modules.TryGetValue(moduleName, out module);
    public bool   Has(string    name) => Modules.ContainsKey(name);

    public static Script GetScriptById(int id) => ScriptIdMap?.GetValueOrDefault(id);
}