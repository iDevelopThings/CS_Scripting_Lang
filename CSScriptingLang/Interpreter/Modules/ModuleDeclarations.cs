using CSScriptingLang.IncrementalParsing.Syntax.SyntaxNodes;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.RuntimeValues.Prototypes;
using CSScriptingLang.RuntimeValues.Values;

namespace CSScriptingLang.Interpreter.Modules;

public class ModuleScriptDeclarations
{
    public HashSet<ITopLevelDeclarationNode> TopLevelDeclarations { get; set; } = new();

    public HashSet<StructPrototype>             StructTypes             { get; set; } = new();
    public HashSet<SignalPrototype>             SignalTypes             { get; set; } = new();
    public HashSet<EnumPrototype>               EnumTypes               { get; set; } = new();
    public HashSet<Value>                       FunctionTypes           { get; set; } = new();
    public HashSet<DefDeclaration_FunctionNode> DefFunctionDeclarations { get; set; } = new();
    public HashSet<FunctionDecl>                DefFunctionDecls        { get; set; } = new();

    public HashSet<VariableSymbol> VariableDeclarations { get; set; } = new();
    public HashSet<VariableSymbol> Exports              { get; set; } = new();
    public HashSet<VariableSymbol> PrivateExports       { get; set; } = new();

    public bool IsExported(string name) {
        if (Exports.Any(export => export.Name == name)) {
            return true;
        }
        return false;
    }

    public bool IsExport(string name) {
        return Exports.Any(export => export.Name == name) || PrivateExports.Any(export => export.Name == name);
    }
}