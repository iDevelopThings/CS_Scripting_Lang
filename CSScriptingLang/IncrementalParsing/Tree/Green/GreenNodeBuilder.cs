using CSScriptingLang.Lexing;
using CSScriptingLang.IncrementalParsing.Syntax;

namespace CSScriptingLang.IncrementalParsing.Tree.Green;

public class GreenNodeBuilder
{
    public readonly struct ParentInfo(int position, SyntaxKind kind)
    {
        public int        FirstChild { get; } = position;
        public SyntaxKind Kind       { get; } = kind;
    }

    public int               ElementCount { get; set; } = 0;
    public Stack<ParentInfo> Parents      { get; }      = new();
    public List<GreenNode>   Children     { get; }      = [];

    public void StartNode(SyntaxKind kind) {
        var position = Children.Count;
        Parents.Push(new ParentInfo(position, kind));
    }

    private static bool IsTrivia(GreenNode greenNode) {
        if (greenNode.IsNode) {
            return greenNode.SyntaxKind is SyntaxKind.Comment or SyntaxKind.Trivia;
        }
        return greenNode.TokenKind is TokenType.Whitespace or TokenType.NewLine;
    }

    public void FinishNode() {
        if (Parents.Count == 0) {
            return;
        }

        var parentInfo   = Parents.Pop();
        var nodeChildren = new List<GreenNode>();
        var length       = 0;
        var childStart   = parentInfo.FirstChild;
        var childEnd     = Children.Count - 1;
        var childCount   = Children.Count;
        // skip trivia
        if (parentInfo.Kind is not (SyntaxKind.Block or SyntaxKind.Source)) {
            for (; childStart < Children.Count; childStart++) {
                if (!IsTrivia(Children[childStart])) {
                    break;
                }
            }

            for (; childEnd >= childStart; childEnd--) {
                if (!IsTrivia(Children[childEnd])) {
                    break;
                }
            }
        }

        for (var i = childStart; i <= childEnd; i++) {
            if (i == childStart) {
                length = Children[i].Length;
            } else {
                length += Children[i].Length;
            }

            nodeChildren.Add(Children[i]);
        }

        Children.RemoveRange(childStart, childEnd - childStart + 1);
        var green = CreateGreenNode(parentInfo.Kind, length, nodeChildren);
        if (childEnd + 1 < childCount) {
            Children.Insert(childStart, green);
        } else {
            Children.Add(green);
        }
    }

    private GreenNode CreateGreenNode(SyntaxKind kind, int length, List<GreenNode> children) {
        ElementCount++;
        return new GreenNode(kind, length, children);
    }

    private GreenNode CreateGreenToken(TokenType kind, SourceRange range) {
        ElementCount++;
        return new GreenNode(kind, range.Length);
    }

    public void EatToken(TokenType kind, SourceRange range) {
        Children.Add(CreateGreenToken(kind, range));
    }

    public (GreenNode, int) Finish() {
        return (Children[0], ElementCount);
    }
}