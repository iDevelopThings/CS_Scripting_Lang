using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.Interpreter.Execution.Statements;

namespace CSScriptingLang.Parsing.AST;

[ASTNode]
public partial class ProgramExpression : BlockExpression
{
    [VisitableNodeProperty]
    public ImportStatementsNode Imports { get; set; }

    [VisitableNodeProperty]
    public ModuleDeclarationNode ModuleDeclaration { get; set; }

    public bool IsModule { get; set; }

    public BlockExpression Body             => IsModule ? FirstOfType<InlineFunctionDeclaration>()?.Body : this;
    public bool            HasTopLevelAwait => Body.Nodes.Any(n => n is AwaitStatement);
    
    public ProgramExpression() { }
    public ProgramExpression(IEnumerable<BaseNode> statements) : base(statements) { }

    public void Combine(ProgramExpression other) {
        if (StartToken.Value == null && other.StartToken.Value != null)
            StartToken = other.StartToken;
        if (EndToken.Value == null && other.EndToken.Value != null)
            EndToken = other.EndToken;


        Nodes.AddRange(other.Nodes);

        if (other.Imports != null) {
            var imports = Imports ?? new ImportStatementsNode();

            foreach (var import in other.Imports.Nodes) {
                if (!imports.Nodes.Contains(import)) {
                    imports.Nodes.Add(import);
                }
            }

            if (imports.Nodes.Count == 0)
                Imports = null;
            else
                Imports = imports;
        }

        foreach (var node in AllNodes()) {
            node.Parent = this;
        }
    }
}