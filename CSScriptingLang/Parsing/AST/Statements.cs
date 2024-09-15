using CSScriptingLang.Interpreter.Execution.Expressions;

namespace CSScriptingLang.Parsing.AST;

[ASTNode]
public partial class ProgramExpression : BlockExpression
{
    [VisitableNodeProperty]
    public ImportStatementsNode Imports { get; set; }

    [VisitableNodeProperty]
    public ModuleDeclarationNode ModuleDeclaration { get; set; }

    public ProgramExpression() { }
    public ProgramExpression(IEnumerable<BaseNode> statements) : base(statements) { }


    public override string ToString(int indent = 0) {
        var str = $"{new string(' ', indent)}{GetType().Name} {{\n";

        if (Imports != null)
            str += Imports.ToString(indent + 2) + "\n";

        str += PrintNodes(indent + 2, "\n");

        str += $"\n{new string(' ', indent)}}}";

        return str;
    }

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