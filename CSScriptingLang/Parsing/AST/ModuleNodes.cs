using CSScriptingLang.Interpreter.Execution.Expressions;

namespace CSScriptingLang.Parsing.AST;

[ASTNode]
public partial class ModuleDeclarationNode : BaseNode
{
    [VisitableNodeProperty]
    public StringExpression Name { get; set; }

    public ModuleDeclarationNode() { }

}

[ASTNode]
public partial class ImportStatementsNode : NodeList<ImportStatementNode>
{
    public ImportStatementsNode() { }
    public ImportStatementsNode(IEnumerable<ImportStatementNode> statements) : base(statements) { }

}

[ASTNode]
public partial class ImportStatementNode : BaseNode
{
    [VisitableNodeProperty]
    public StringExpression Path { get; }

    public ImportStatementNode(StringExpression path) {
        Path = path;
    }

}