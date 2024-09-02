using CSScriptingLang.Lexing;
using CSScriptingLang.RuntimeValues.Types;

namespace CSScriptingLang.Parsing.AST;

public interface IExpressionNode { }


[ASTNode]
public partial class RangeNode : BaseNode, IExpressionNode
{
    [VisitableNodeProperty]
    public BaseNode Expression { get; }

    public RangeNode(BaseNode expression) {
        Expression = expression;
    }

    public override string ToString(int indent = 0) {
        return $"{new string(' ', indent)}{GetType().Name}: {Expression}";
    }
}

[ASTNode]
public partial class ExpressionListNode : NodeList<IExpressionNode>
{
    public List<IExpressionNode> Expressions     => Nodes;
    public List<BaseNode>        ExpressionNodes => Nodes.OfType<BaseNode>().ToList();

    public int Count => Expressions.Count;

    public override string ToString(int indent = 0) {
        var str = $"{new string(' ', indent)}{GetType().Name} ";

        if (Expressions.Count == 0) {
            return str + "(empty)";
        }

        str += PrintNodes(indent);

        return str;
    }
}

[ASTNode]
public partial class VariableNode : BaseNode, IExpressionNode
{
    public string Name { get; }

    public VariableNode(string name) {
        Name = name;
    }

    public override string ToString(int indent = 0) {
        return $"{new string(' ', indent)}{GetType().Name}: {Name}";
    }
}

[ASTNode]
public partial class BinaryOperationNode : BaseNode, IExpressionNode
{
    [VisitableNodeProperty]
    public BaseNode Left { get; }

    public OperatorType Operator { get; }

    [VisitableNodeProperty]
    public BaseNode Right { get; }

    public BinaryOperationNode(BaseNode left, OperatorType op, BaseNode right) {
        Left     = left;
        Operator = op;
        Right    = right;
    }

    public override string ToString(int indent = 0) {
        return $@"{new string(' ', indent)}{GetType().Name}: {Operator.ToString()}
{Left.ToString(indent + 2)}
{Right.ToString(indent + 2)}";
    }
}

[ASTNode]
public partial class UnaryOperationNode : BaseNode, IExpressionNode
{
    public OperatorType Operator { get; }

    [VisitableNodeProperty]
    public BaseNode Operand { get; }

    public bool IsPostfix { get; } // True if the operation is postfix (e.g. i++ vs ++i)

    public UnaryOperationNode(OperatorType op, BaseNode operand, bool isPostfix = false) {
        Operator  = op;
        Operand   = operand;
        IsPostfix = isPostfix;
    }

    public override string ToString(int indent = 0) {
        return $@"{new string(' ', indent)}{GetType().Name}: {Operator.ToString()} {Operand.ToString()} {(IsPostfix ? "postfix" : "prefix")}";
    }
}

[ASTNode]
public partial class ObjectProperty : BaseNode
{
    public string Name { get; set; }

    [VisitableNodeProperty]
    public BaseNode Value { get; set; }

    public ObjectProperty(string name, BaseNode value) {
        Name  = name;
        Value = value;
    }
}

[ASTNode]
public partial class ObjectLiteralNode : BaseNode, IExpressionNode
{
    [VisitableNodeProperty]
    public List<ObjectProperty> Properties { get; } = new();

    public RuntimeTypeInfo_Object ObjectType { get; set; }

    public ObjectProperty AddProperty(string name, BaseNode value) {
        var prop = new ObjectProperty(name, value);
        Properties.Add(prop);
        return prop;
    }

    public override string ToString(int indent = 0) {
        var str = $"{new string(' ', indent)}{GetType().Name} ";

        if (Properties.Count == 0) {
            return str + "(empty)";
        }

        str += "{\n";

        foreach (var prop in Properties) {
            str += $"{prop.ToString(indent + 2)}\n";
        }

        str += $"{new string(' ', indent)}}}";

        return str;
    }
}

[ASTNode]
public partial class PropertyAccessNode : BaseNode, IExpressionNode
{
    [VisitableNodeProperty]
    public BaseNode Object { get; set; }

    public string Name { get; }

    public PropertyAccessNode(BaseNode obj, string name) {
        Object = obj;
        Name   = name;
    }

    public string GetPath() {
        if (Object is PropertyAccessNode prop) {
            return $"{prop.GetPath()}.{Name}";
        }

        if (Object is IndexAccessNode index) {
            return $"{index.GetPath()}[{Name}]";
        }

        return Name;
    }

    public override string ToString(int indent = 0) {
        return $"{new string(' ', indent)}{GetType().Name}: {Name} on {Object}";
    }
}

[ASTNode]
public partial class ArrayLiteralNode : BaseNode, IExpressionNode
{
    [VisitableNodeProperty]
    public List<BaseNode> Elements { get; } = new();

    public override string ToString(int indent = 0) {
        var str = $"{new string(' ', indent)}{GetType().Name} ";

        if (Elements.Count == 0) {
            return str + "(empty)";
        }
        
        str += "{\n";
        
        foreach (var element in Elements) {
            str += $"{element.ToString(indent + 2)}\n";
        }
        
        
        str += $"{new string(' ', indent)}}}";

        return str;
    }
}
[ASTNode]
public partial class IndexAccessNode : BaseNode, IExpressionNode
{
    [VisitableNodeProperty]
    public BaseNode Object { get; }

    [VisitableNodeProperty]
    public BaseNode Index { get; }

    public IndexAccessNode(BaseNode obj, BaseNode index) {
        Object = obj;
        Index  = index;
    }

    public string GetPath() {
        if (Object is PropertyAccessNode prop) {
            return $"{prop.GetPath()}[{Index}]";
        }

        if (Object is IndexAccessNode index) {
            return $"{index.GetPath()}[{Index}]";
        }

        return $"[{Index}]";
    }

    public override string ToString(int indent = 0) {
        return $"{new string(' ', indent)}{GetType().Name}: {Index} on {Object}";
    }
}