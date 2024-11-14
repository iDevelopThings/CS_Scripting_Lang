using CSScriptingLang.Core.Diagnostics;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Utils;
using SharpX;

namespace CSScriptingLang.IncrementalParsing.Syntax.SyntaxNodes;

[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
public class SyntaxNodeAttribute : Attribute;

public partial class PlaceholderSyntaxNode(int index, SyntaxTree tree) : SyntaxNode(index, tree)
{
    public override string DebugContent() {
        var str = "";
        str += Tree.Script.SourceText.GetRange(SourceRange);
        
        /*foreach (var child in ChildrenElements) {
            if (child is SyntaxNode node) {
                var nodeText = node.Text.ToString();
                if (nodeText.IsMultiLine())
                    nodeText = nodeText.Replace("\r\n", "\n").Replace("\n", "\\n");

                str += nodeText;
                str += " ";
            } else if (child is SyntaxToken token) {
                var nodeText = token.RepresentText;
                if (nodeText.IsMultiLine())
                    nodeText = nodeText.Replace("\r\n", "\n").Replace("\n", "\\n");
                str += $"Token.{token.Kind.ToString()} {token.RepresentText} {nodeText}";
                str += " ";
            }

            /* else {
                str += child is PlaceholderSyntaxNode pn ? $"Placeholder({pn.Kind.ToString()})" : child.ToString();
                str += " ";
            }#1#
        }*/
        
        return str;
    }
}

[SyntaxNode]
public partial class SourceSyntax(int index, SyntaxTree tree) : SyntaxNode(index, tree), IExecMulti
{
    public Block Block => ChildNode<Block>();

    public IEnumerable<Maybe<ValueReference>> ExecuteMulti(ExecContext ctx) {
        return Block.Execute(ctx, false);
    }
}

[SyntaxNode]
public partial class CommentSyntax(int index, SyntaxTree tree) : SyntaxNode(index, tree) { }