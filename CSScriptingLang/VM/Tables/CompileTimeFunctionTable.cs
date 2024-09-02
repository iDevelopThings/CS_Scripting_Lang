using CSScriptingLang.Parsing.AST;

namespace CSScriptingLang.VM.Tables;

public class CompileTimeFunctionTable
{
    public        CompileTimeFunctionTable Parent { get; set; }
    public static CompileTimeFunctionTable Global = new();

    public Dictionary<string, FunctionDeclarationNode> Functions = new();

    public CompileTimeFunctionTable(CompileTimeFunctionTable parent = null) {
        Parent = parent;
    }

    public CompileTimeFunctionTable AddChild() => new(this);

    public CompileTimeFunctionTable Register(string key, FunctionDeclarationNode value) {
        Functions.TryAdd(key, value);
        return this;
    }

    public bool TryGetFunction(string key, out FunctionDeclarationNode value) {
        if (Functions.TryGetValue(key, out var val)) {
            value = val;
            return true;
        }

        if (Parent != null)
            return Parent.TryGetFunction(key, out value);

        value = null;
        return false;
    }
}