using CSScriptingLang.Interpreter.Execution.Expressions;

namespace CSScriptingLang.Parsing.AST;

[ASTNode]
public partial class ModuleDeclarationNode : BaseNode
{
    [VisitableNodeProperty]
    public StringExpression Name { get; set; }

    public ModuleDeclarationNode() { }

    public override string ToString(int indent = 0) {
        var str = $"{new string(' ', indent)}{GetType().Name}: {{\n";

        str += $"{new string(' ', indent + 2)}Name: {Name.NativeValue}\n";

        str += $"\n{new string(' ', indent)}}}";

        return str;
    }
}

[ASTNode]
public partial class ImportStatementsNode : NodeList<ImportStatementNode>
{
    public ImportStatementsNode() { }
    public ImportStatementsNode(IEnumerable<ImportStatementNode> statements) : base(statements) { }

    public override string ToString(int indent = 0) {
        var str = $"{new string(' ', indent)}{GetType().Name}: {{\n";

        str += PrintNodes(indent + 2, "\n");

        str += $"\n{new string(' ', indent)}}}";

        return str;
    }
}

[ASTNode]
public partial class ImportStatementNode : BaseNode
{
    [VisitableNodeProperty]
    public StringExpression Path { get; }

    public ImportStatementNode(StringExpression path) {
        Path = path;
    }

    public override string ToString(int indent = 0) {
        return $"{new string(' ', indent)}{GetType().Name}: {Path.ToString()}";
    }
}