using System.Diagnostics;
using CSScriptingLang.Lexing;
using CSScriptingLang.IncrementalParsing.Syntax;

namespace CSScriptingLang.IncrementalParsing.Tree;

[Flags]
public enum NodeFlags
{
    None = 0x0,

    Node  = 0x1,
    Token = 0x2,
}

// Green tree node among red and green trees
[DebuggerDisplay($"{{{nameof(ToSimpleDebugString)}(),nq}}")]
[DebuggerTypeProxy(typeof(GreenNodeDebugView))]
public class GreenNode
{
    private class GreenNodeDebugView
    {
        private readonly GreenNode _node;
        public GreenNodeDebugView(GreenNode node) {
            _node = node;
        }

        public string Kind   => _node.IsNode ? _node.SyntaxKind.ToString() : _node.TokenKind.ToString();
        public int    Length => _node.Length;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public List<GreenNode> Children => _node.Children.ToList();
    }


    public int Length { get; }
    
    private readonly List<GreenNode> _children;

    public NodeFlags Flag { get; set; }

    public long RawKind { get; set; }

    public IEnumerable<GreenNode> Children => IsNode ? _children! : Enumerable.Empty<GreenNode>();

    public SyntaxKind SyntaxKind => Flag == NodeFlags.Node ? (SyntaxKind) RawKind : SyntaxKind.None;
    public TokenType  TokenKind  => Flag == NodeFlags.Token ? (TokenType) RawKind : TokenType.None;

    public bool IsNode  => Flag == NodeFlags.Node;
    public bool IsToken => Flag == NodeFlags.Token;

    public GreenNode(SyntaxKind kind, int length, List<GreenNode> children) {
        Length    = length;
        Flag      = NodeFlags.Node;
        RawKind   = (ushort) kind;
        _children = children;
    }

    public GreenNode(TokenType kind, int length) {
        Flag    = NodeFlags.Token;
        RawKind = (long) kind;
        Length  = length;
    }

    public string ToSimpleDebugString() {
        var str = "";
        if (IsNode) {
            str += $"Node -> SyntaxKind.{SyntaxKind.ToString()}";
        } else {
            str += $"Token -> TokenType.{TokenKind.ToString()}";
        }

        str += $" Children: {Children.Count()}";

        return str;
    }
}