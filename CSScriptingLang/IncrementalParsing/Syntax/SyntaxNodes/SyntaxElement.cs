using System.Diagnostics;
using CSScriptingLang.Core.Diagnostics;
using CSScriptingLang.Parsing.AST.NamedSymbol;
using CSScriptingLang.IncrementalParsing.Syntax.SyntaxNodes;
using CSScriptingLang.Interpreter.Modules;
using CSScriptingLang.Utils;

namespace CSScriptingLang.IncrementalParsing.Syntax;

[DebuggerDisplay($"{{{nameof(ToSimpleDebugString)}(),nq}}")]
public abstract partial class SyntaxElement(int index, SyntaxTree tree) : IEquatable<SyntaxElement>
{
    // ReSharper disable once MemberCanBePrivate.Global
    public int ElementId { get; } = index;

    public SyntaxTree Tree { get; } = tree;

    public ReadOnlySpan<char> Text => Tree.Script.SourceText.GetRange(SourceRange);
    public ReadOnlySpan<char> TextOffset => Tree.Script.SourceText.GetRange(
        new SourceRange(SourceRange.StartOffset, SourceRange.Length + 1)
    );

    protected long RawKind => Tree.GetRawKind(ElementId);

    protected int ParentIndex      => Tree.GetParent(ElementId);
    protected int ChildStartIndex  => Tree.GetChildStart(ElementId);
    protected int ChildFinishIndex => Tree.GetChildEnd(ElementId);

    public int    ScriptId    => Tree.Script.Id;
    public Script GetScript() => ModuleResolver.GetScriptById(ScriptId);


    public SourceRange SourceRange => Tree.GetSourceRange(ElementId);

    public SyntaxNode Parent => Tree.GetElement(ParentIndex) as SyntaxNode;

    public SyntaxElementId UniqueId     => new(ScriptId, ElementId);
    public string          UniqueString => UniqueId.ToString();

    public int Position => SourceRange.StartOffset;

    public string SourceLocationString()
        => $"{Tree.Script.Uri}:{GetSymbolRange().Start.Line + 1}";

    public NamedSymbolRange GetSymbolRange()
        => Tree.Script.SourceText.GetTokenRange(SourceRange, ScriptId);
    /*{
        var startCol  = Tree.Script.SourceText.GetCol(SourceRange.StartOffset);
        var endCol    = Tree.Script.SourceText.GetCol(SourceRange.EndOffset);
        var startLine = Tree.Script.SourceText.GetLine(SourceRange.StartOffset);
        var endLine   = Tree.Script.SourceText.GetLine(SourceRange.EndOffset);

        var st        = Tree.Script.SourceText.GetTokenRange(SourceRange, ScriptId);

        return new NamedSymbolRange {
            ScriptId = ScriptId,
            Start = new NamedSymbolPosition {
                ScriptId  = ScriptId,
                Line      = startLine,
                Character = startCol,
            },
            End = new NamedSymbolPosition {
                ScriptId  = ScriptId,
                Line      = endLine,
                Character = endCol,
            },
        };
    }*/

    protected IEnumerable<SyntaxElement> ChildrenElements {
        get {
            var start = ChildStartIndex;
            if (start == -1) {
                yield break;
            }

            var finish = ChildFinishIndex;
            for (var i = start; i <= finish; i++) {
                var element = Tree.GetElement(i);
                if (element is not null) {
                    yield return element;
                }
            }
        }
    }

    public IEnumerable<SyntaxNode> ChildrenNode {
        get {
            var start = ChildStartIndex;
            if (start == -1) {
                yield break;
            }

            var finish = ChildFinishIndex;
            for (var i = start; i <= finish; i++) {
                if (Tree.IsNode(i)) {
                    var element = Tree.GetElement(i);
                    if (element is not null) {
                        yield return (element as SyntaxNode)!;
                    }
                }
            }
        }
    }

    public IEnumerable<SyntaxElement> ChildrenWithTokens => ChildrenElements;

    public IEnumerable<SyntaxToken> ChildrenTokens {
        get {
            var start = ChildStartIndex;
            if (start == -1)
                yield break;

            var finish = ChildFinishIndex;
            for (var i = start; i <= finish; i++) {
                if (!Tree.IsToken(i))
                    continue;

                var element = Tree.GetElement(i);
                if (element is not null) {
                    yield return element as SyntaxToken;
                }
            }
        }
    }

    // Traverse all descendants, including yourself
    public abstract IEnumerable<SyntaxElement> DescendantsAndSelf { get; }

    // excluding myself
    public abstract IEnumerable<SyntaxElement> Descendants { get; }
    public abstract IEnumerable<SyntaxElement> DescendantsInRange(SourceRange range);
    public abstract IEnumerable<SyntaxElement> DescendantsWithToken { get; }

    // Traverse all descendants and tokens, including yourself
    public abstract IEnumerable<SyntaxElement> DescendantsAndSelfWithTokens { get; }

    // Visit ancestor nodes
    public IEnumerable<SyntaxNode> Ancestors {
        get {
            var parent = Parent;
            while (parent != null) {
                yield return parent;
                parent = parent.Parent;
            }
        }
    }

    // Visit ancestor nodes, including yourself
    public IEnumerable<SyntaxElement> AncestorsAndSelf {
        get {
            var node = this;
            while (node != null) {
                yield return node;
                node = node.Parent;
            }
        }
    }

    // public ElementPtr<TNode> ToPtr<TNode>() where TNode : SyntaxElement => new(UniqueId);

    public DiagnosticBuilder Diagnostic()
        => DiagnosticManager.Build(Caller.GetFromFrame(3)).Range(this);

    public void Diagnostic_Error(string message = null)
        => Diagnostic().Error().Message(message).ReportToSyntaxTree(Tree);

    public void Diagnostic_Error_Fatal(string message = null)
        => Diagnostic().Error().AsFatal().Message(message).ReportToSyntaxTree(Tree);

    public void Diagnostic_Warning(string message = null)
        => Diagnostic().Warning().Message(message).ReportToSyntaxTree(Tree);

    public void Diagnostic_Info(string message = null)
        => Diagnostic().Info().Message(message).ReportToSyntaxTree(Tree);

    public void Diagnostic_Hint(string message = null)
        => Diagnostic().Hint().Message(message).ReportToSyntaxTree(Tree);

    public override int GetHashCode() {
        return UniqueId.GetHashCode();
    }

    public bool Equals(SyntaxElement other) {
        if (other is null) {
            return false;
        }

        return UniqueId == other.UniqueId;
    }

    public override bool Equals(object obj) {
        return Equals(obj as SyntaxElement);
    }

    public virtual void Accept(ISyntaxNodeVisitor visitor) { }

    public virtual string DebugContent() => "";

    public virtual string ToSimpleDebugString() {
        var nodeText = (Text.IsEmpty ? "Empty" : "'" + Text.ToString() + "'");
        if (nodeText.IsMultiLine())
            nodeText = nodeText.Replace("\r\n", "\n").Replace("\n", "\\n");

        return $"Type({GetType().ToShortName()}): SrcRange={SourceRange} {nodeText}";
    }
}