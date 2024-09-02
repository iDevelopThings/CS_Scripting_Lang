namespace CSScriptingLang.Parsing.AST;

[ASTNode]
public partial class ProgramNode : BlockNode
{
    [VisitableNodeProperty]
    public ImportStatementsNode Imports { get; set; } = new();

    public ProgramNode() { }
    public ProgramNode(IEnumerable<BaseNode> statements) : base(statements) { }

    public List<FunctionDeclarationNode> Functions => Nodes.OfType<FunctionDeclarationNode>().ToList();

    public override string ToString(int indent = 0) {
        var str = $"{new string(' ', indent)}{GetType().Name} {{\n";

        str += Imports.ToString(indent + 2) + "\n";

        str += PrintNodes(indent + 2, "\n");

        str += $"\n{new string(' ', indent)}}}";

        return str;
    }

    public void Combine(ProgramNode other) {
        Nodes.AddRange(other.Nodes);

        foreach (var import in other.Imports.Nodes) {
            if (!Imports.Nodes.Contains(import)) {
                Imports.Nodes.Add(import);
            }
        }
        
        foreach (var node in AllNodes()) {
            node.Parent = this;
        }
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
    public StringNode Path { get; }

    public ImportStatementNode(StringNode path) {
        Path = path;
    }

    public override string ToString(int indent = 0) {
        return $"{new string(' ', indent)}{GetType().Name}: {Path.ToString()}";
    }
}

[ASTNode]
public partial class BlockNode : NodeList<BaseNode>
{
    public List<BaseNode> Statements => Nodes;

    [VisitableNodeProperty]
    public ReturnStatementNode ReturnNode { get; set; }
    
    public List<DeferStatementNode> DeferStatements = new();

    public BlockNode() { }
    public BlockNode(IEnumerable<BaseNode> statements) : base(statements) { }

    public override string ToString(int indent = 0) {
        var str = $"{new string(' ', indent)}{GetType().Name} {{\n";

        str += PrintNodes(indent + 2, "\n");

        str += $"\n{new string(' ', indent)}}}";

        return str;
    }
}

[ASTNode]
public partial class AssignmentNode : BaseNode
{
    public string VariableName { get; }

    [VisitableNodeProperty]
    public BaseNode Variable { get; set; }

    [VisitableNodeProperty]
    public BaseNode Value { get; set; }

    public bool IsConst => Value is IConstantNode;

    public AssignmentNode(string variableName, BaseNode value) {
        VariableName = variableName;
        Value        = value;
    }

    public AssignmentNode(BaseNode variable, BaseNode value) {
        Variable = variable;
        Value    = value;
    }

    public override string ToString(int indent = 0) {
        return $"{new string(' ', indent)}{GetType().Name}: {(Variable != null ? Variable : VariableName)} = {Value.ToString(0)}";
    }
}

[ASTNode]
public partial class FunctionCallNode : BaseNode
{
    public string Name;

    [VisitableNodeProperty]
    public ExpressionListNode Arguments { get; set; } = new();

    // Handles `obj.method()` | `obj['method']()` etc
    [VisitableNodeProperty]
    public BaseNode Variable { get; set; }

    public string VariableName;

    public FunctionCallNode(string name, ExpressionListNode arguments) {
        Name      = name;
        Arguments = arguments;
    }

    public FunctionCallNode(BaseNode variable, ExpressionListNode arguments) {
        Variable     = variable;
        VariableName = (variable as VariableNode)?.Name;
        // Name      = "Error";
        Arguments = arguments;
    }


    public override string ToString(int indent = 0) {
        var str = $"{new string(' ', indent)}{GetType().Name}: {Name}(";

        str += Arguments.ToString(0);

        str += ")";


        return str;
    }
}

[ASTNode]
public partial class IfStatementNode : BaseNode
{
    [VisitableNodeProperty]
    public BaseNode Condition { get; }

    [VisitableNodeProperty]
    public BlockNode ThenBranch { get; }

    [VisitableNodeProperty]
    public BaseNode ElseBranch { get; }

    public IfStatementNode(BaseNode condition, BlockNode thenBranch, BlockNode elseBranch = null) {
        Condition  = condition;
        ThenBranch = thenBranch;
        ElseBranch = elseBranch;
    }

    public override string ToString(int indent = 0) {
        var str = $"{new string(' ', indent)}{GetType().Name} {{\n";

        str += $"Condition: {Condition.ToString(indent + 2)}\n";
        str += $"Then: {(ThenBranch != null ? ThenBranch.ToString(indent + 2) : "null")}\n";
        str += $"Else: {(ElseBranch != null ? ElseBranch.ToString(indent + 2) : "null")}\n";

        str += $"{new string(' ', indent)}}}";

        return str;
    }
}

[ASTNode]
public partial class ForLoopNode : BaseNode
{
    // let i = 0
    [VisitableNodeProperty]
    public BaseNode Initialization { get; }

    // i < 10
    [VisitableNodeProperty]
    public BaseNode Condition { get; }

    // i++
    [VisitableNodeProperty]
    public BaseNode Increment { get; }

    [VisitableNodeProperty]
    public BlockNode Body { get; }


    public ForLoopNode(BaseNode initialization, BaseNode condition, BaseNode increment, BlockNode body) {
        Initialization = initialization;
        Condition      = condition;
        Increment      = increment;
        Body           = body;
    }

    public override string ToString(int indent = 0) {
        var str = $"{new string(' ', indent)}{GetType().Name} {{\n";

        str += $"Initialization: {Initialization.ToString(indent + 2)}\n";
        str += $"Condition: {Condition?.ToString(indent + 2)}\n";
        str += $"Increment: {Increment?.ToString(indent + 2)}\n";
        str += $"{Body.ToString(indent + 2)}\n";

        str += $"{new string(' ', indent)}}}";

        return str;
    }
}

[ASTNode]
public partial class ForRangeNode : BaseNode
{
    [VisitableNodeProperty]
    public TupleListDeclarationNode Indexers { get; set; }

    [VisitableNodeProperty]
    public RangeNode Range { get; set; }

    [VisitableNodeProperty]
    public BlockNode Body { get; set; }

    public ForRangeNode() {
        Indexers = new();
        Body     = new();
    }
    public ForRangeNode(TupleListDeclarationNode indexers, RangeNode range, BlockNode body) {
        Indexers = indexers;
        Range    = range;
        Body     = body;
    }

    public override string ToString(int indent = 0) {
        var str = $"{new string(' ', indent)}{GetType().Name} {{\n";

        str += $"Indexers: {Indexers.ToString(indent + 2)}\n";
        str += $"Range: {Range.ToString(indent + 2)}\n";
        str += $"{Body.ToString(indent + 2)}\n";

        str += $"{new string(' ', indent)}}}";

        return str;
    }
}

[ASTNode]
public partial class ReturnStatementNode : BaseNode
{
    [VisitableNodeProperty]
    public BaseNode ReturnValue { get; }

    public ReturnStatementNode(BaseNode returnValue) {
        ReturnValue = returnValue;
    }

    public override string ToString(int indent = 0) {
        return $"{new string(' ', indent)}{GetType().Name}: {ReturnValue?.ToString(0)}";
    }
}

[ASTNode]
public partial class DeferStatementNode : BaseNode
{
    [VisitableNodeProperty]
    public BaseNode Expression { get; }
    
    public DeferStatementNode(BaseNode expression) {
        Expression = expression;
    }
    
    public override string ToString(int indent = 0) {
        return $"{new string(' ', indent)}{GetType().Name}: {Expression?.ToString(0)}";
    }
}