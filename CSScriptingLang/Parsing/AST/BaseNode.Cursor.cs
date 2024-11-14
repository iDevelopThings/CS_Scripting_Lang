using CSScriptingLang.Lexing;
using CSScriptingLang.Parsing.AST.NamedSymbol;
using Position = OmniSharp.Extensions.LanguageServer.Protocol.Models.Position;

namespace CSScriptingLang.Parsing.AST;

public abstract partial class BaseNode
{
    public NamedSymbolRange Range => this;

    public List<Token> AllTokens() {
        var tokens = new List<Token> {StartToken};
        var nodes  = AllNodes().ToList();
        foreach (var child in nodes) {
            if (child.StartToken?.Value != null && child.EndToken?.Value != null) {
                tokens.Add(child.StartToken);
                tokens.Add(child.EndToken);
            }
        }

        tokens.Add(EndToken);

        return tokens;
    }
    public (Token, Token) FindTokenRange() {
        var minToken = StartToken;
        var maxToken = EndToken;

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
    public T OfType<T>() where T : BaseNode => Child<T>();
    public IEnumerable<BaseNode> Parents(Type type = null) {
        var p = Parent;
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
    public bool ParentOfType(Type type, out BaseNode parent) {
        parent = ParentOfType(type);
        return parent != null;
    }
    public BaseNode ParentOfType(Type type = null) => Parents(type).FirstOrDefault();
    public bool ParentOfType<T>(out T parent) where T : BaseNode {
        if (ParentOfType(typeof(T)) is T t) {
            parent = t;
            return true;
        }

        parent = default;
        return false;
    }
    public T ParentOfType<T>() where T : BaseNode => (T) ParentOfType(typeof(T));
    public bool Child(Type type, out BaseNode child) {
        child = Child(type);
        return child != null;
    }
    public BaseNode Child(Type type = null) {
        foreach (var node in AllNodes()) {
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

    public IEnumerable<BaseNode> AllOfType(Type    type, bool topLevelOnly = false) => Children(type, topLevelOnly);
    public IEnumerable<T>        AllOfType<T>(bool topLevelOnly = false) => Children<T>(topLevelOnly);

    public IEnumerable<BaseNode> Children(Type type = null, bool topLevelOnly = false) {
        foreach (var node in AllNodes()) {
            if (type == null || node.GetType() == type) {
                yield return node;
            }

            if (topLevelOnly) {
                continue;
            }

            foreach (var child in node.Children(type)) {
                yield return child;
            }
        }
    }
    public IEnumerable<T> Children<T>(bool topLevelOnly = false) {
        foreach (var node in AllNodes()) {
            if (node == null)
                continue;

            if (node is T t) {
                yield return t;
            }

            if (topLevelOnly) {
                continue;
            }

            foreach (var child in node.Children<T>()) {
                yield return child;
            }
        }
    }
    public T ChildAtIndex<T>(int index, bool topLevelOnly = false) {
        var children = Children<T>(topLevelOnly);
        var child    = children.ElementAtOrDefault(index);
        return child;
    }

    public BaseNode NodeAtPosition(LSPPosition position) {
        // Check if the current node contains the position, then recursively check its children
        foreach (var node in AllNodes()) {
            if (!node.Range.Contains(position))
                continue;

            // Recursively check children for more specific matches
            var childNode = node.AllNodes()
               .Select(n => n.NodeAtPosition(position))
               .FirstOrDefault(n => n != null);

            return childNode ?? node; // Return the child node if found, else the current node
        }

        // No match found
        return null;
    }

    public bool NodeAtPosition(LSPPosition position, out BaseNode node) {
        node = NodeAtPosition(position);
        return node != null;
    }

}