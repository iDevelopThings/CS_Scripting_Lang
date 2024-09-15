using CSScriptingLang.Lexing;
using CSScriptingLang.Utils;

namespace CSScriptingLang.Parsing.AST;

public struct NodeCursor
{
    public BaseNode Node { get; set; }
    public NodeCursor(BaseNode node) {
        Node = node;
    }

    public static implicit operator BaseNode(NodeCursor cursor) => cursor.Node;
    public static implicit operator NodeCursor(BaseNode node)   => new(node);

    public NodeCursorFirst First => new(Node);
    public NodeCursorAll   All   => new(Node);

    public IEnumerable<Token> Tokens() {
        if (Node.StartToken.Value == null || Node.EndToken.Value == null)
            throw new Exception($"Node({Node?.GetType().ToShortName()}) does not have start and end tokens.");

        var start = Node.StartToken;
        var end   = Node.EndToken;

        var processed = new HashSet<Token>();

        IEnumerable<Token> AllTokens(BaseNode current) {


            IEnumerable<Token> GetAllChildrenTokens(BaseNode node) {
                yield return current.StartToken;

                foreach (var child in node.AllNodes()) {
                    if (child.StartToken.Value != null && child.EndToken.Value != null) {
                        yield return child.StartToken;
                        yield return child.EndToken;
                    } else {
                        throw new Exception($"Node({child?.GetType().ToShortName()}) does not have start and end tokens.");
                    }

                    foreach (var token in GetAllChildrenTokens(child)) {
                        yield return token;
                    }
                }

                yield return end;
            }

            foreach (var token in GetAllChildrenTokens(current)) {
                if (processed.Add(token))
                    yield return token;
            }

        }

        var tokens = AllTokens(Node).ToList();
        tokens.Sort((a, b) => a.Range.Start.CompareTo(b.Range.Start));
        return tokens;
    }

    public class FailedToFindTokenRangeException : Exception
    {
        public FailedToFindTokenRangeException(string message) : base(message) { }
    }

    public List<Token> AllTokens() {
        var tokens = new List<Token> {Node.StartToken};
        var nodes  = Node.AllNodes().ToList();
        foreach (var child in nodes) {
            if (child.StartToken?.Value != null && child.EndToken?.Value != null) {
                tokens.Add(child.StartToken);
                tokens.Add(child.EndToken);
            }
        }

        tokens.Add(Node.EndToken);

        return tokens;
    }

    public (Token, Token) FindTokenRange() {
        var minToken = Node.StartToken;
        var maxToken = Node.EndToken;

        var min = minToken.Range.Start;
        var max = maxToken.Range.End;

        var toks = AllTokens();

        foreach (var tok in toks) {

            if (tok.Range.Start < min) {
                min      = tok.Range.Start;
                minToken = tok;
            }

            if (tok.Range.End > max) {
                max      = tok.Range.End;
                maxToken = tok;
            }
        }

        // Console.WriteLine($"All tokens: {allToks.Count}");

        return (minToken, maxToken);
    }
}

public struct NodeCursorFirst
{
    public BaseNode Node { get; set; }
    public NodeCursorFirst(BaseNode node) {
        Node = node;
    }

    // Find the first node of type T in the tree.
    public T Of<T>() where T : BaseNode => Child<T>();

    public bool Parent(Type type, out BaseNode parent) {
        parent = Parent(type);
        return parent != null;
    }

    public IEnumerable<BaseNode> Parents(Type type = null) {
        var p = Node.Parent;
        while (p != null) {

            foreach (var node in p.AllNodes()) {
                if (type == null || node.GetType() == type)
                    yield return node;
            }

            if (type == null || p.GetType() == type)
                yield return p;

            p = p.Parent;
        }
    }
    public IEnumerable<T> Parents<T>() where T : BaseNode {
        foreach (var node in Parents(typeof(T))) {
            if (node is T t)
                yield return t;
        }
    }

    public BaseNode Parent(Type type = null) => Parents(type).FirstOrDefault();

    public bool Parent<T>(out T parent) where T : BaseNode {
        if (Parent(typeof(T)) is T t) {
            parent = t;
            return true;
        }

        parent = default;
        return false;
    }
    public T Parent<T>() where T : BaseNode => (T) Parent(typeof(T));

    public bool Child(Type type, out BaseNode child) {
        child = Child(type);
        return child != null;
    }
    public BaseNode Child(Type type = null) {
        foreach (var node in Node.AllNodes()) {
            if (type == null || node.GetType() == type)
                return node;
        }

        return null;
    }

    public bool Child<T>(out T child) where T : BaseNode {
        if (Child(typeof(T)) is T t) {
            child = t;
            return true;
        }

        child = default;
        return false;
    }
    public T Child<T>() where T : BaseNode => (T) Child(typeof(T));
}

public struct NodeCursorAll
{
    public BaseNode Node { get; set; }
    public NodeCursorAll(BaseNode node) {
        Node = node;
    }

    public IEnumerable<BaseNode> Of(Type type, bool topLevelOnly = false) => Children(type, topLevelOnly);

    public IEnumerable<T> Of<T>(bool topLevelOnly = false) => Children<T>(topLevelOnly);

    public IEnumerable<BaseNode> Children(Type type = null, bool topLevelOnly = false) {
        foreach (var node in Node.AllNodes()) {
            if (type == null || node.GetType() == type) {
                yield return node;
            }

            if (topLevelOnly) {
                continue;
            }

            foreach (var child in new NodeCursorAll(node).Children(type)) {
                yield return child;
            }
        }
    }

    public IEnumerable<T> Children<T>(bool topLevelOnly = false) {
        foreach (var node in Node.AllNodes()) {
            if(node == null)
                continue;
            
            if (node is T t) {
                yield return t;
            }

            if (topLevelOnly) {
                continue;
            }

            foreach (var child in new NodeCursorAll(node).Children<T>()) {
                yield return child;
            }
        }
    }
}