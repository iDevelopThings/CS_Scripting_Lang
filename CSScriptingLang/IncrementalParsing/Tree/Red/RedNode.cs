using System.Diagnostics;
using CSScriptingLang.Lexing;
using CSScriptingLang.IncrementalParsing.Syntax;

namespace CSScriptingLang.IncrementalParsing.Tree.Red;

[DebuggerDisplay($"{{{nameof(ToSimpleDebugString)}(),nq}}")]
public record struct RedNode(
    long        RawKind,
    NodeFlags   Flag,
    SourceRange SourceRange,
    int         Parent,
    int         ChildStart,
    int         ChildEnd
)
{
    public static RedNode Empty = new(0, NodeFlags.None, SourceRange.Empty, -1, -1, -1);

    public string ToSimpleDebugString() {
        var str  = "";
        var kind = (NodeFlags) (RawKind >> 16);

        if (kind == NodeFlags.Node) {
            str += $"Node -> {(SyntaxKind) ((ushort) kind)}";
        } else if (kind == NodeFlags.Token) {
            str += $"Token -> {(TokenType) ((ushort) kind)}";
        }

        str += $" [{SourceRange}] Child range: [{ChildStart}, {ChildEnd}] Parent: {Parent}";

        return str;
    }
}