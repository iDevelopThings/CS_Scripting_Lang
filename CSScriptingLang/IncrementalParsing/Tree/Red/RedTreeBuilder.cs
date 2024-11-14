using CSScriptingLang.Lexing;
using CSScriptingLang.Utils;

#pragma warning disable CS9113 // Parameter is unread.

namespace CSScriptingLang.IncrementalParsing.Tree.Red;

public class RedTreeBuilder(IncrementalParser parser)
{
    public List<RedNode> Build(GreenNode greenRoot, int totalCount) {
        using var _ = TimedScope.Scoped_Print("RedTreeBuilder::Build");

        var redNodes = new List<RedNode>(totalCount) {
            new(
                greenRoot.RawKind,
                greenRoot.Flag,
                new SourceRange(0, greenRoot.Length),
                -1, -1, -1
            ),
        };

        var parentIndex = 0;
        var queue       = new Queue<(int, GreenNode)>();
        queue.Enqueue((parentIndex, greenRoot));
        while (queue.Count != 0) {
            var (nodeIndex, greenNode) = queue.Dequeue();
            var startOffset     = redNodes[nodeIndex].SourceRange.StartOffset;
            var childStartIndex = redNodes.Count;
            var childEndIndex   = -1;
            foreach (var childGreen in greenNode.Children) {
                var childIndex = redNodes.Count;
                childEndIndex = childIndex;
                redNodes.Add(
                    new RedNode(
                        childGreen.RawKind,
                        childGreen.Flag,
                        new SourceRange(startOffset, childGreen.Length),
                        nodeIndex,
                        -1,
                        -1
                    )
                );

                startOffset += childGreen.Length;

                if (childGreen.IsNode) {
                    queue.Enqueue((childIndex, childGreen));
                }
            }

            if (childEndIndex != -1) {
                redNodes[nodeIndex] = redNodes[nodeIndex] with {
                    ChildStart = childStartIndex,
                    ChildEnd = childEndIndex,
                };
            }
        }

        return redNodes;
    }
}