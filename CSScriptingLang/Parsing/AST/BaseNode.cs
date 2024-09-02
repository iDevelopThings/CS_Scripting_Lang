using System.Runtime.CompilerServices;
using CSScriptingLang.Lexing;

namespace CSScriptingLang.Parsing.AST;

public interface IPrintableNode
{
    public string ToString(int indent = 0);
}

[ASTNode]
public abstract partial class BaseNode : IPrintableNode
{
    public Token StartToken { get; set; }
    public Token EndToken   { get; set; }

    public BaseNode Parent { get; set; }

    public NodeCursor Cursor => new(this);

    public T ParentAs<T>() where T : BaseNode => Parent as T;

    /*public bool TryFindFirstChild<T>(out T child) where T : BaseNode {
        foreach (var node in AllNodes()) {
            if (node is T t) {
                child = t;
                return true;
            }
        }

        child = default;
        return false;
    }

    public bool TryGetFirstParent<T>(out T parent) where T : BaseNode {
        var p = Parent;
        while (p != null) {
            if (p is T t) {
                parent = t;
                return true;
            }

            p = p.Parent;
        }

        parent = default;
        return false;
    }

    public T FirstParent<T>() where T : BaseNode {
        if (TryGetFirstParent(out T parent))
            return parent;
        throw new InvalidOperationException($"Could not find parent of type {typeof(T).Name}.");
    }
*/
    
    public virtual string ToString(int indent = 0) {
        return $"{new string(' ', indent)}{GetType().Name}";
    }
    public virtual void Accept(IAstVisitor visitor) {
        // visitor.Visit(this);
    }
    public virtual IEnumerable<BaseNode> AllNodes() {
        yield break;
    }
}

[ASTNode]
public partial class NodeList<T> : BaseNode
{
    public List<T> Nodes { get; set; } = [];

    protected NodeList() { }
    protected NodeList(IEnumerable<T> nodes) {
        Nodes = nodes.ToList();
    }
    protected NodeList(IEnumerable<BaseNode> statements) {
        Nodes = statements.Cast<T>().ToList();
    }

    public NodeList<T> Add(T node) {
        if (node == null) {
            Console.WriteLine("Warning: Attempted to add null node to program node.");
            return this;
        }

        Nodes.Add(node);
        
        OnNodeAdded(node);

        return this;
    }
    public NodeList<T> Add(BaseNode node, [CallerMemberName] string caller = "") {
        switch (node) {
            case null:
                Console.WriteLine($"[{caller}] Attempted to add null node to NodeList<{typeof(T).Name}>.");
                return this;
            case T t:
                Add(t);
                break;
            default:
                Console.WriteLine($"[{caller}] Attempted to add node of type {node.GetType().Name} to NodeList<{typeof(T).Name}>.");
                break;
        }

        return this;
    }

    public static implicit operator List<T>(NodeList<T> nodeList) {
        return nodeList.Nodes;
    }
    public static NodeList<T> operator +(NodeList<T> nodeList, T node)
        => nodeList.Add(node);

    public string PrintNodes(int indent = 0, string sepChar = ", ") {
        var nodeStrings = new List<string>();
        foreach (var node in Nodes) {
            if (node is IPrintableNode printableNode)
                nodeStrings.Add(printableNode.ToString(indent));
            else
                nodeStrings.Add($"{new string(' ', indent)}{node}");
        }

        return string.Join(sepChar, nodeStrings);
    }
    
    public virtual void OnNodeAdded(T node) { }

    public override IEnumerable<BaseNode> AllNodes() {
        if (Nodes != null) {
            foreach (var node in Nodes) {
                if (node is BaseNode baseNode) {
                    yield return baseNode;
                }
            }
        }

        foreach (var node in base.AllNodes()) {
            yield return node;
        }
    }
}