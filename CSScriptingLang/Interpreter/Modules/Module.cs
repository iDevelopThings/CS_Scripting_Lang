using CSScriptingLang.Parsing.AST;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.RuntimeValues.Values;

namespace CSScriptingLang.Interpreter.Modules;

public class ModuleDeclarations : ModuleScriptDeclarations
{
    public virtual Dictionary<string, RuntimeTypeInfo_Struct>      StructsByName                  { get; set; } = new();
    public virtual Dictionary<string, RuntimeType>                 InterfacesByName               { get; set; } = new();
    public virtual Dictionary<string, RuntimeTypeInfo_Signal>      SignalsByName                  { get; set; } = new();
    public virtual Dictionary<string, Value>                       FunctionsByName                { get; set; } = new();
    public virtual Dictionary<string, DefDeclaration_FunctionNode> DefFunctionsDeclarationsByName { get; set; } = new();
    public virtual Dictionary<string, VariableSymbol>              VariablesByName                { get; set; } = new();

    public virtual void Add(ModuleScriptDeclarations scriptDeclarations) {
        TopLevelDeclarations.UnionWith(scriptDeclarations.TopLevelDeclarations);

        StructTypes.UnionWith(scriptDeclarations.StructTypes);
        StructsByName = StructTypes.ToDictionary(structType => structType.Name);

        InterfaceTypes.UnionWith(scriptDeclarations.InterfaceTypes);
        InterfacesByName = InterfaceTypes.ToDictionary(interfaceType => interfaceType.Name);

        SignalTypes.UnionWith(scriptDeclarations.SignalTypes);
        SignalsByName = SignalTypes.ToDictionary(signalType => signalType.Name);

        FunctionTypes.UnionWith(scriptDeclarations.FunctionTypes);
        FunctionsByName = FunctionTypes.ToDictionary(functionType => functionType.As.Fn().Name);

        DefFunctionDeclarations.UnionWith(scriptDeclarations.DefFunctionDeclarations);
        DefFunctionsDeclarationsByName = DefFunctionDeclarations.ToDictionary(functionType => functionType.Name);

        VariableDeclarations.UnionWith(scriptDeclarations.VariableDeclarations);
        VariablesByName = VariableDeclarations.ToDictionary(variable => variable.Name);
    }
}

public class Module
{
    public string Name          { get; set; }
    public string DirectoryPath { get; set; }

    public bool IsMainModule { get; set; }

    public List<Script> Scripts { get; set; } = new();

    public ModuleDeclarations Declarations = new();

    public Script MainScript { get; set; }
    public Script TryGetMainScript() {
        // Try to find the first with a `main` function
        var s = Scripts.FirstOrDefault(script => script.IsMain);
        if (s != null) {
            return s;
        }

        // Otherwise next we'll try to use an `index` script
        s = Scripts.FirstOrDefault(script => script.NameWithoutExtension.ToLower() == "index");
        if (s != null) {
            return s;
        }

        // Otherwise we'll just use the first script
        return Scripts.FirstOrDefault();
    }

    public Module(string name) {
        Name         = name;
        IsMainModule = name.ToLower() == "main";
    }

    public void AddScript(Script script) {
        Scripts.Add(script);
        script.Module = this;

        TrySetOuterMostDirectory();
    }

    public void TrySetOuterMostDirectory() {
        DirectoryPath = Scripts.Select(script => script.FilePath)
           .OrderByDescending(path => path.Length)
           .FirstOrDefault();
    }

    public Script GetScriptByPath(string filePath)
        => Scripts.FirstOrDefault(script => script.FilePath == filePath);

    public Script GetScriptByName(string scriptName)
        => Scripts.FirstOrDefault(script => script.Name == scriptName);
}