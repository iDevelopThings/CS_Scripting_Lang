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
    public BaseNode Parent(Type type = null) {
        var p = Node.Parent;
        while (p != null) {
            if (type == null || p.GetType() == type)
                return p;
            p = p.Parent;
        }

        return null;
    }

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