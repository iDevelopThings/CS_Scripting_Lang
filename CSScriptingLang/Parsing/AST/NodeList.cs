using System.Collections;
using System.Runtime.CompilerServices;

namespace CSScriptingLang.Parsing.AST;

public interface INodeList
{
    public IEnumerable<BaseNode> NodesAsBaseNode { get; }
    public IEnumerable<TNode>    NodesOfType<TNode>() where TNode : BaseNode;
}

[ASTNode]
public partial class NodeList<T> : BaseNode, INodeList, IEnumerable<T>
{
    public List<T> Nodes { get; set; } = [];

    public IEnumerable<BaseNode> NodesAsBaseNode => Nodes.Cast<BaseNode>();

    public int Count => Nodes.Count;

    protected NodeList() { }
    protected NodeList(IEnumerable<T> nodes) {
        Nodes = nodes.ToList();
        SetStartEndTokens();
    }

    protected NodeList(IEnumerable<BaseNode> statements) {
        Nodes = statements.Cast<T>().ToList();
        SetStartEndTokens();
    }

    private void SetStartEndTokens() {
        if (Nodes.Count <= 0)
            return;

        if (Nodes.First() is BaseNode first) {
            StartToken = first.StartToken;
        }
        if (Nodes.Last() is BaseNode last) {
            EndToken = last.EndToken;
        }
    }

    public NodeList<T> Add(T node) {
        if (node == null) {
            Console.WriteLine("Warning: Attempted to add null node to program node.");
            return this;
        }

        Nodes.Add(node);

        SetStartEndTokens();

        OnNodeAdded(node);

        return this;
    }
    public virtual NodeList<T> Add(BaseNode node, [CallerMemberName] string caller = "") {
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

    public virtual void OnNodeAdded(T node) { }

    public IEnumerable<TNode> NodesOfType<TNode>() where TNode : BaseNode
        => Nodes.OfType<TNode>();

    public TNode FirstOfType<TNode>() where TNode : BaseNode
        => NodesOfType<TNode>().FirstOrDefault();

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

    public IEnumerator<T> GetEnumerator() {
        return Nodes.GetEnumerator();
    }
    IEnumerator IEnumerable.GetEnumerator() {
        return ((IEnumerable) Nodes).GetEnumerator();
    }

    public T this[int index] => Nodes[index];
}