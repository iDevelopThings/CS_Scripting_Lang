using System.Reflection;
using CSScriptingLang.Interpreter;
using CSScriptingLang.Utils;

namespace CSScriptingLang.Parsing.AST;

public interface IDeclarationNode { }
public interface ITopLevelDeclarationNode : IDeclarationNode { }

[ASTNode]
public partial class VariableDeclarationNode : BaseNode, ITopLevelDeclarationNode
{
    [VisitableNodeProperty]
    public AssignmentNode Assignment { get; }

    public string VariableName => Assignment.VariableName;
    public bool   IsConst      => Assignment.IsConst;
    public BaseNode Value {
        get => Assignment.Value;
        set => Assignment.Value = value;
    }

    public VariableDeclarationNode(AssignmentNode assignment) {
        Assignment = assignment;
    }
}

[ASTNode]
public partial class ArgumentDeclarationNode : BaseNode
{
    public string Name { get; set; }
    public string Type { get; set; }

    public bool IsVarArgs   { get; set; }
    public Type VarArgsType => IsVarArgs ? typeof(object[]) : null;
    public Type NativeType  { get; set; }
    public int  Index       { get; set; }
    public bool IsNative    { get; set; }

    public ArgumentDeclarationNode(string name, string type) {
        Name = name;
        Type = type;
    }

    public static ArgumentDeclarationNode VarArgs(string name) => new(name, "object[]") {IsVarArgs = true};

    public override string ToString(int indent = 0) {
        return $"{new string(' ', indent)}{GetType().Name}: {Name} ({Type})";
    }
}

[ASTNode]
public partial class ArgumentListDeclarationNode : NodeList<ArgumentDeclarationNode>
{
    [VisitableNodeProperty]
    public List<ArgumentDeclarationNode> Arguments => Nodes;

    public bool HasVarArgs   { get; set; }
    public int  VarArgsIndex { get; set; }

    public ArgumentListDeclarationNode() { }
    public ArgumentListDeclarationNode(IEnumerable<ArgumentDeclarationNode> arguments) : base(arguments) { }

    public int Count => Arguments.Count;

    public int GetValidArgumentCount(int callArgCount) {
        if (HasVarArgs) {
            return callArgCount;
        }

        return Math.Min(callArgCount, Arguments.Count);
    }

    public bool Get(int index, out ArgumentDeclarationNode node) {
        if (index < 0) {
            node = null;
            return false;
        }

        if(HasVarArgs && index >= VarArgsIndex) {
            node = Arguments[VarArgsIndex];
            return true;
        }
        
        if (index >= Arguments.Count) {
            if (HasVarArgs) {
                node = Arguments[VarArgsIndex];
                return true;
            }

            node = null;
            return false;
        }

        node = Arguments[index];
        return true;
    }

    public override void OnNodeAdded(ArgumentDeclarationNode node) {
        base.OnNodeAdded(node);
        
        if (node.IsVarArgs && !HasVarArgs) {
            HasVarArgs   = true;
            VarArgsIndex = node.IsNative ? node.Index - 1 : node.Index;
        }

    }

    public override string ToString(int indent = 0) {
        var str = $"{new string(' ', indent)}{GetType().Name} ";

        if (Arguments.Count == 0) {
            return str + "(empty)";
        }

        str += PrintNodes(indent);

        return str;
    }


    public ArgumentDeclarationNode this[int index] => Arguments[index];
    public IEnumerator<ArgumentDeclarationNode> GetEnumerator() => Arguments.GetEnumerator();
}

[ASTNode]
public partial class TupleListDeclarationNode : NodeList<BaseNode>
{
    public TupleListDeclarationNode() { }
    public TupleListDeclarationNode(IEnumerable<BaseNode> arguments) : base(arguments) { }

    public override string ToString(int indent = 0) {
        var str = $"{new string(' ', indent)}{GetType().Name} ";

        if (Nodes.Count == 0) {
            return str + "(empty)";
        }

        str += PrintNodes(indent);

        return str;
    }

    public BaseNode this[int index] => Nodes[index];
    public IEnumerator<BaseNode> GetEnumerator() => Nodes.GetEnumerator();
}

[ASTNode]
public partial class InlineFunctionDeclarationNode : BaseNode
{
    [VisitableNodeProperty]
    public ArgumentListDeclarationNode Parameters { get; set; } = new();

    [VisitableNodeProperty]
    public BlockNode Body { get; set; }

    public bool HasReturnStatementDefined => Body?.Statements.Any(x => x is ReturnStatementNode) ?? false;

    public bool IsNative { get; set; }

    private Action<Interpreter.Interpreter, FunctionExecutionFrame> _nativeFunction;
    public Action<Interpreter.Interpreter, FunctionExecutionFrame> NativeFunction {
        get => _nativeFunction;
        set {
            _nativeFunction = value;
            IsNative        = value != null;
        }
    }

    public InlineFunctionDeclarationNode() { }
    public InlineFunctionDeclarationNode(ArgumentListDeclarationNode parameters, BlockNode body) {
        Parameters = parameters;
        Body       = body;
    }


    public override string ToString(int indent = 0) {
        var str = $"{new string(' ', indent)}{GetType().Name}: " +
                  $"({Parameters.ToString(0)}) {{\n" +
                  $"{Body.ToString(indent + 2)}\n" +
                  $"{new string(' ', indent)}}}";

        return str;
    }
}

[ASTNode]
public partial class FunctionDeclarationNode : InlineFunctionDeclarationNode, ITopLevelDeclarationNode
{
    public string Name { get; set; }

    public FunctionDeclarationNode(string name) : base() {
        Name = name;
    }
    public FunctionDeclarationNode(string name, ArgumentListDeclarationNode parameters, BlockNode body) : base(parameters, body) {
        Name = name;
    }

    public override string ToString(int indent = 0) {
        var str = $"{new string(' ', indent)}{GetType().ToShortName()}: {Name}" +
                  $"({Parameters.ToString(0)}) {{\n" +
                  $"{Body.ToString(indent + 2)}\n" +
                  $"{new string(' ', indent)}}}";

        return str;
    }
    
    public FunctionDeclarationNode SetNative(bool isNative) {
        IsNative = isNative;
        return this;
    }
}

[ASTNode]
public partial class NativeBoundFunctionDeclarationNode : FunctionDeclarationNode
{
    public MethodInfo MethodInfo { get; set; }

    public NativeBoundFunctionDeclarationNode(string name, MethodInfo methodInfo) : base(name) {
        MethodInfo = methodInfo;
        IsNative   = true;
    }
    public NativeBoundFunctionDeclarationNode(MethodInfo methodInfo) : base(methodInfo.Name) {
        MethodInfo = methodInfo;
        IsNative   = true;
    }

    public override string ToString() {
        return $"{GetType().ToShortName()}: {Name} (NativeBinding)";
    }
}