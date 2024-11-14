using CSScriptingLang.Lexing;
using CSScriptingLang.Utils;

namespace CSScriptingLang.IncrementalParsing.Syntax.SyntaxNodes;

public partial class SyntaxToken(int index, SyntaxTree tree) : SyntaxElement(index, tree)
{
    public TokenType Kind => (TokenType) RawKind;

    // public ReadOnlySpan<char> Text => Tree.Script.Source.AsSpan(SourceRange.StartOffset, SourceRange.Length);

    public string RepresentText => Text.Trim().ToString();

    public override IEnumerable<SyntaxElement> DescendantsAndSelf {
        get { yield return this; }
    }

    public override IEnumerable<SyntaxElement> Descendants => [];

    public override IEnumerable<SyntaxElement> DescendantsInRange(SourceRange range) {
        if (range.Intersects(SourceRange)) {
            yield return this;
        }
    }

    public override IEnumerable<SyntaxElement> DescendantsWithToken => [];

    public override IEnumerable<SyntaxElement> DescendantsAndSelfWithTokens {
        get { yield return this; }
    }


    public override string ToSimpleDebugString() {
        return $"{GetType().ToShortName()} -> {Kind.ToString()} SrcRange={SourceRange} {(Text.IsEmpty ? "Empty" : "'" + Text.ToString() + "'")}";
    }
}