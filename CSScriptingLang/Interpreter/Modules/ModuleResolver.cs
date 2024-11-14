using CSScriptingLang.Core;
using CSScriptingLang.Core.Async;
using CSScriptingLang.Core.Diagnostics;
using CSScriptingLang.Core.FileSystem;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.RuntimeValues.Values;
using CSScriptingLang.Utils;
using CSScriptingLang.Core.Logging;
using CSScriptingLang.IncrementalParsing.Syntax.SyntaxNodes;
using CSScriptingLang.Parsing.AST.NamedSymbol;

namespace CSScriptingLang.Interpreter.Modules;

public record ModulePath
{
    public enum ModulePathType
    {
        Script,
        Directory,
    }

    public ModulePathType Type { get; set; }

    public string RawPath { get; set; }

    public string Directory   { get; set; }
    public string RelativeDir { get; set; }

    public string ScriptPath { get; set; }
    public bool   IsRoot     { get; set; }

    public ModulePath(InterpreterFileSystem fileSystem, string path) {
        RawPath = fileSystem.RelativePath(path)!;

        // if the path is a script file, ie has `Script.Extension` then we'll remove it
        var isScript = Path.GetExtension(RawPath) == Script.Extension;
        if (!isScript && fileSystem.Exists(Path.ChangeExtension(RawPath, Script.Extension))) {
            RawPath  = Path.ChangeExtension(RawPath, Script.Extension);
            isScript = true;
        }

        if (isScript) {
            ScriptPath  = RawPath;
            Directory   = Path.GetDirectoryName(fileSystem.AbsolutePath(RawPath));
            RelativeDir = fileSystem.RelativePath(Directory);
            Type        = ModulePathType.Script;
            IsRoot      = Directory == fileSystem.Root;

            return;
        }

        // Otherwise we'll assume it's a directory
        ScriptPath = Path.Combine(RawPath!, "index" + Script.Extension);

        Directory   = Path.GetDirectoryName(ScriptPath);
        RelativeDir = fileSystem.RelativePath(Directory);
        Type        = ModulePathType.Directory;

    }

    public IEnumerable<InterpreterFile> Files(InterpreterFileSystem fileSystem) {
        if (Type == ModulePathType.Directory) {
            return fileSystem.GetAllAtPath(RawPath);
        }

        return new[] {fileSystem.GetFile(ScriptPath)};
    }
}

public delegate string          ResolverDelegate(string     name, IEnumerable<string> searchDirectories);
public delegate InterpreterFile ModuleLoaderDelegate(string resolvedName);

public class ModuleResolver
{
    public static Logger Logger = Logs.Get<ModuleResolver>(LogLevel.Warning);

    public ClassScopedTimerInst<ModuleResolver> Timer = ClassScopedTimerInst<ModuleResolver>.Create(Logger)
       .SetColorFn(n => n.BoldBrightBlue())
       .SetName("ModuleResolver");

    public ResolverDelegate     Resolver { get; set; }
    public ModuleLoaderDelegate Loader   { get; set; }

    public List<string> SearchDirectories { get; set; } = ["./"];

    private static int ScriptIdCounter = 0;

    private InterpreterFileSystem FileSystem;

    public static Dictionary<int, Script> ScriptIdMap;
    public static NamedSymbolContainer    NamedSymbols;

    public Dictionary<string, Module> Modules;

    public IEnumerable<Script>        AllScripts       => Modules.Values.SelectMany(m => m.Scripts);
    public Dictionary<string, Script> ScriptsByAbsPath => AllScripts.ToDictionary(s => s.File.Abs);

    public Module MainModule { get; set; }
    public Script MainScript { get; set; }

    public ModuleResolver(InterpreterFileSystem fs) {
        FileSystem      = fs;
        ScriptIdMap     = new();
        NamedSymbols    = new();
        Modules         = new();
        ScriptIdCounter = 0;

        Resolver = (name, searchDirectories) => {
            foreach (var dir in searchDirectories) {
                var path = FileSystem.Combine(dir, name);
                if (FileSystem.Exists(FileSystem.Combine(dir, name))) {
                    return FileSystem.RelativePath(path);
                }
                if (FileSystem.Exists(name)) {
                    return FileSystem.RelativePath(name);
                }
            }

            return null;
        };

        Loader = path => FileSystem.GetFile(path);
    }

    public (Value exports, Script script) ResolveEntryScript(ExecContext ctx, string path) {
        var exports = Resolve(ctx, path, true);
        return (exports, exports.DataObject as Script);
    }
    public Value Resolve(ExecContext ctx, string path, bool isEntryModule = false, bool ignoreCache = false) {
        ModulePath modulePath = new(FileSystem, path);

        var module = GetOrCreateModule(modulePath.IsRoot ? "main" : modulePath.RelativeDir);

        var modulesCache = ctx.GetOrCreateVariable("__modules", () => Value.Object(ctx));

        var searchDirs = (IEnumerable<string>) SearchDirectories ?? Array.Empty<string>();
        if (modulePath.Type == ModulePath.ModulePathType.Directory) {
            var dir = modulePath.Directory;
            searchDirs = new[] {dir}.Concat(searchDirs);

            var dirCache = modulesCache[modulePath.RelativeDir];
            if (dirCache.Type == RTVT.Object && !ignoreCache) {
                return dirCache;
            }

            dirCache           = Value.Object(ctx);
            // dirCache.Prototype = Value.Null();

            var dirExports = Value.Object(ctx);
            // dirExports.Prototype = Value.Null();

            var dirScripts = Value.Object(ctx);
            // dirExports.Prototype = Value.Null();

            dirCache["modules"] = dirScripts;
            dirCache["exports"] = dirExports;

            modulesCache[modulePath.RelativeDir] = dirCache;

            foreach (var file in modulePath.Files(FileSystem)) {
                if (dirCache["modules"][file.Rel].Type == RTVT.Object && !ignoreCache) {
                    continue;
                }

                var fileExports = Resolve(ctx, file.Rel, isEntryModule);
                dirCache["modules"][file.Rel] = fileExports;

                foreach (var kvp in fileExports.As.Object()) {
                    dirExports[kvp.Key] = kvp.Value;
                }
            }

            return dirCache;
        }


        var resolvedPath = Resolver(modulePath.ScriptPath, searchDirs);

        var cachedExports = modulesCache[resolvedPath];
        if (cachedExports.Type == RTVT.Object && !ignoreCache) {
            return cachedExports;
        }

        var exports = ProcessExports(ctx, modulesCache, resolvedPath, module, modulePath, isEntryModule);

        return exports;
    }
    private Value ProcessExports(
        ExecContext ctx,
        Value       modulesCache,
        string      resolvedPath,
        Module      module,
        ModulePath  modulePath,
        bool        isEntryModule = false
    ) {
        var exports = Value.Object(ctx);
        // exports.Prototype = Value.Null();

        modulesCache[resolvedPath] = exports;

        try {
            var src = Loader(resolvedPath);

            var script = new Script(src, ScriptIdCounter++);
            script.IsWrappedModule = !isEntryModule;
            script.IsEntryModule   = isEntryModule;
            module.AddScript(script);

            ScriptIdMap[script.Id] = script;

            exports.DataObject = script;

            if (isEntryModule)
                script.ParseEntryModule(ctx);
            else
                script.ParseWrappedModule(ctx);


            if (!isEntryModule && script.Program != null) {
                var moduleFn = script.Program.FirstOfType<InlineFunctionDeclaration>();
                if (moduleFn == null) {
                    throw new Exception("Module does not have a module declaration");
                }
                var fn = ctx.MakeFunction(moduleFn);
                // function(object exports, object require, object module, string fileName, string dirName)
                var value = ctx.Call(
                    fn,
                    null,
                    /* exports */ exports,
                    // /* require */ null,
                    // /* module */ null,
                    /* fileName */ resolvedPath,
                    /* dirName */ modulePath.Directory
                );

                if (value != null && value.DataObject is ScriptTask task) {
                    Console.Write("");
                } else if (script.Program.HasTopLevelAwait) { }
            }
            if (!isEntryModule && script.SyntaxTree?.SyntaxRoot != null) {
                var moduleFn = script.SyntaxTree?.SyntaxRoot?.Block.ChildNode<FunctionDecl>();
                if (moduleFn == null) {
                    throw new Exception("Module does not have a module declaration");
                }
                var fn = ctx.MakeFunction(moduleFn);
                // function(object exports, object require, object module, string fileName, string dirName)
                var value = ctx.Call(
                    fn,
                    null,
                    /* exports */ exports,
                    // /* require */ null,
                    // /* module */ null,
                    /* fileName */ resolvedPath,
                    /* dirName */ modulePath.Directory
                );
            }

            Logger.Debug($"Resolved module {resolvedPath}");

            if (module.IsMainModule) {
                MainScript            ??= module.TryGetMainScript();
                MainModule.MainScript ??= MainScript;
            }

            {
                using var _ = ctx.SwitchModule(module);
                foreach (var symbol in script.Declarations.Exports) {
                    ctx.Variables.Set(symbol.Name, symbol);
                    exports[symbol.Name] = symbol.Val;
                }

                script.Exports = exports;
            }
        }
        catch (FatalDiagnosticException) {
            throw;
        }
        catch (Exception e) {
            if (e is FatalDiagnosticException)
                return exports;

            Logger.Exception(e);
            throw;
        }
        return exports;
    }

    public static Script CreateVirtualScript(
        string                content,
        bool                  isEntryModule = false,
        InterpreterFileSystem fs            = null
    ) {
        fs ??= InterpreterConfig.FileSystem;

        var f = fs.AddVirtualFile(content);
        var s = new Script(f, ScriptIdCounter++);
        s.IsWrappedModule = !isEntryModule;
        s.IsWrappedModule = isEntryModule;

        return s;
    }

    public void SetMainScript(Script script) {
        if (script == null)
            return;

        MainModule            = script.Module;
        MainModule.MainScript = script;
    }

    private Module GetOrCreateModule(string name) {
        if (Modules.TryGetValue(name, out var module)) {
            return module;
        }

        module        = new Module(name);
        Modules[name] = module;

        if (name == "main") {
            module.IsMainModule = true;
            MainModule          = module;
        }

        return module;
    }


    public Module Get(string    moduleName)                    => Modules[moduleName];
    public bool   TryGet(string moduleName, out Module module) => Modules.TryGetValue(moduleName, out module);
    public bool   Has(string    name) => Modules.ContainsKey(name);

    public static Script GetScriptById(int         id)   => ScriptIdMap?.GetValueOrDefault(id);
    public        Script GetScriptByAbsPath(string path) => ScriptsByAbsPath.GetValueOrDefault(path);
}