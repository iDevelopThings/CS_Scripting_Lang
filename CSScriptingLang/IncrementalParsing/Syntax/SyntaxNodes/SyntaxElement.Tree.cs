using CSScriptingLang.Lexing;
using CSScriptingLang.IncrementalParsing.Syntax.SyntaxNodes;
using CSScriptingLang.Utils;
using SharpX.Extensions;

namespace CSScriptingLang.IncrementalParsing.Syntax;

public abstract partial class SyntaxElement
{
    public T ChildNode<T>() where T : SyntaxNode
        => ChildNodes<T>().FirstOrDefault();
    public T ChildNode<T>(Func<T, bool> predicate) where T : SyntaxNode
        => ChildNodes<T>().FirstOrDefault(predicate);
    public T ChildNode<T>(SyntaxNode skipping) where T : SyntaxNode
        => ChildNodes<T>().FirstOrDefault(it => it.ElementId != skipping.ElementId);

    public IEnumerable<T> ChildNodes<T>() where T : SyntaxNode
        => ChildrenNode.OfType<T>();
    public IEnumerable<T> ChildNodes<T>(Func<T, bool> predicate) where T : SyntaxNode
        => ChildrenNode.OfType<T>().Where(predicate);
    public IEnumerable<SyntaxNode> ChildNodes(SyntaxKind kind)
        => ChildrenNode.Where(it => it.Kind == kind);
    public IEnumerable<T> ChildNodes<T>(SyntaxKind kind) where T : SyntaxNode
        => ChildrenNode.OfType<T>().Where(it => it.Kind == kind);


    public T ChildToken<T>() where T : SyntaxToken
        => ChildrenElements.OfType<T>().FirstOrDefault();
    public T ChildToken<T>(Func<TokenType, bool> predicate) where T : SyntaxToken
        => ChildrenElements.OfType<T>().FirstOrDefault(it => predicate(it.Kind));
    public T ChildToken<T>(TokenType kind) where T : SyntaxToken
        => ChildToken<T>(it => it.HasAny(kind));
    
    public SyntaxToken ChildToken(Func<TokenType, bool> predicate)
        => ChildrenElements.OfType<SyntaxToken>().FirstOrDefault(it => predicate(it.Kind));
    public SyntaxToken ChildToken(TokenType kind)
        => ChildToken(it => it.HasAny(kind));

    public bool HasKeywordToken(Keyword kw) => ChildrenTokens
       .Any(t => t.Kind == TokenType.KeywordIdentifier && t.RepresentText.EqualsIgnoreCase(kw.ToString()));

    public bool HasChildElement<T>() where T : SyntaxElement
        => ChildrenElements.OfType<T>().Any();
    public bool HasChildElement<T>(Func<T, bool> predicate) where T : SyntaxElement
        => ChildrenElements.OfType<T>().Any(predicate);

    // Allows us to handle this situation with for loops:
    // for(var i = 0; i < 10; i++) 
    //     ~~~~~~~~~  ~~~~~~  ~~~
    //     Expr 1     Expr 2  Expr 3 || All separated by `;`
    public IEnumerable<SyntaxNode> ChildNodesSeparatedByToken(TokenType kind) {
        var start = ChildStartIndex;
        if (start == -1) {
            yield break;
        }
        
        // So, for `var i = 0` this is var decl node
        // `i < 10` is Expr node, and `i++` is also Expr node
        // Split by token type of `;`, would give us 3 nodes

        
        var finish  = ChildFinishIndex;
        var current = start;
        while (current <= finish) {
            var element = Tree.GetElement(current);
            if (element is SyntaxToken token && token.Kind.HasAny(kind)) {
                current++;
                continue;
            }

            if (element is SyntaxNode node) {
                yield return node;
            }

            current++;
        }
    }

    public IEnumerable<T> ChildNodesBeforeToken<T>(TokenType kind) where T : SyntaxElement {
        foreach (var child in ChildrenElements) {
            switch (child) {
                case SyntaxToken token when token.Kind.HasAny(kind):
                    yield break;
                case T node:
                    yield return node;
                    break;
            }
        }
    }
    public IEnumerable<T> ChildNodesAfterToken<T>(TokenType kind) where T : SyntaxElement {
        var afterToken = false;
        foreach (var child in ChildrenElements) {
            if (afterToken && child is T node) {
                yield return node;
            }

            if (child is SyntaxToken token && token.Kind.HasAny(kind)) {
                afterToken = true;
            }
        }
    }
    
    public IEnumerable<T> ChildNodesBeforeToken<T>(Func<SyntaxElement, bool> predicate) where T : SyntaxElement {
        foreach (var child in ChildrenElements) {
            if (predicate(child)) {
                yield break;
            }

            if (child is T node) {
                yield return node;
            }
        }
    }
    public IEnumerable<T> ChildNodesAfterToken<T>(Func<SyntaxElement, bool> predicate) where T : SyntaxElement {
        var afterToken = false;
        foreach (var child in ChildrenElements) {
            if (afterToken && child is T node) {
                yield return node;
            }

            if (predicate(child)) {
                afterToken = true;
            }
        }
    }
    
    public IEnumerable<T> ChildrenAfter<T>(SyntaxElement element) where T : SyntaxElement {
        var afterElement = false;
        foreach (var child in ChildrenElements) {
            if (afterElement && child is T node) {
                yield return node;
            }

            if (child.ElementId != element.ElementId) {
                afterElement = true;
            }
        }
    }
    public IEnumerable<T> ChildrenBefore<T>(SyntaxElement element) where T : SyntaxElement {
        foreach (var child in ChildrenElements) {
            if (child.ElementId == element.ElementId) {
                yield break;
            }

            if (child is T node) {
                yield return node;
            }
        }
    }
    
    public T ChildAfter<T>(SyntaxElement element) where T : SyntaxElement
        => ChildrenAfter<T>(element).FirstOrDefault();
    public T ChildAfterOrNull<T>(SyntaxElement element) where T : SyntaxElement
        => element is not null ? ChildrenAfter<T>(element).FirstOrDefault() : null;
    public T ChildBefore<T>(SyntaxElement element) where T : SyntaxElement
        => ChildrenBefore<T>(element).LastOrDefault();
    
    public T ChildBeforeOrNull<T>(SyntaxElement element) where T : SyntaxElement
        => element is not null ? ChildrenBefore<T>(element).LastOrDefault() : null;
    public T ChildNodeAfterToken<T>(TokenType kind) where T : SyntaxElement {
        var afterToken = false;
        foreach (var child in ChildrenElements) {
            if (afterToken && child is T node) {
                return node;
            }

            if (child is SyntaxToken token && token.Kind.HasAny(kind)) {
                afterToken = true;
            }
        }

        return null;
    }
    
    /*public IEnumerable<SyntaxToken> ChildTokens(TokenType kind) {
        var start = ChildStartIndex;
        if (start == -1) {
            yield break;
        }

        var finish = ChildFinishIndex;
        for (var i = start; i <= finish; i++) {
            if (Tree.GetTokenType(i).HasAny(kind)) {
                var element = Tree.GetElement(i);
                if (element is not null) {
                    yield return (element as SyntaxToken)!;
                }
            }
        }
    }*/
    
    public SyntaxElement GetNextSibling(int next = 1) {
        var parent = Parent;
        if (parent is null) {
            return null;
        }

        var start = parent.ChildStartIndex;
        if (start == -1) {
            return null;
        }

        var finish        = parent.ChildFinishIndex;
        var nextElementId = ElementId + next;
        return nextElementId <= finish ? Tree.GetElement(nextElementId) : null;
    }
    public SyntaxElement GetPrevSibling(int prev = 1) {
        var parent = Parent;
        if (parent is null) {
            return null;
        }

        var start = parent.ChildStartIndex;
        if (start == -1) {
            return null;
        }

        var prevElementId = ElementId - prev;
        return prevElementId >= start ? Tree.GetElement(prevElementId) : null;
    }
    
    public SyntaxToken GetPrevToken() {
        var prevSibling = GetPrevSibling();
        if (prevSibling is SyntaxToken prevToken) {
            return prevToken;
        }

        return prevSibling?.LastToken();
    }
    public SyntaxToken LastToken() {
        var lastChild = ChildrenWithTokens.LastOrDefault();
        if (lastChild is SyntaxToken token) {
            return token;
        }

        return lastChild?.LastToken();
    }

    public IEnumerable<T> PrevOfType<T>() where T : SyntaxElement {
        var parent = Parent;
        if (parent is null) {
            yield break;
        }

        var start = parent.ChildStartIndex;
        if (start == -1) {
            yield break;
        }

        for (var i = ElementId - 1; i >= start; i--) {
            var element = Tree.GetElement(i);
            if (element is T node) {
                yield return node;
            }
        }
    }
    public IEnumerable<T> NextOfType<T>() where T : SyntaxElement {
        var parent = Parent;
        if (parent is null) {
            yield break;
        }

        var finish = parent.ChildFinishIndex;
        if (finish == -1) {
            yield break;
        }

        for (var i = ElementId + 1; i <= finish; i++) {
            var element = Tree.GetElement(i);
            if (element is T node) {
                yield return node;
            }
        }
    }

    // 0 based line and col
    public SyntaxToken TokenAt(int line, int col) {
        var offset = Tree.Script.GetOffset(line, col);
        return TokenAt(offset);
    }
    // 0 based line and col
    public SyntaxToken TokenAt(int offset) {
        var node = this;
        while (node != null) {
            var nodeElement = node.ChildrenWithTokens.FirstOrDefault(it => it.SourceRange.Contains(offset));
            if (nodeElement is SyntaxToken token) {
                return token;
            }

            node = nodeElement;
        }

        return null;
    }
    public SyntaxToken TokenLeftBiasedAt(int line, int col) {
        if (col > 0) {
            col--;
        }

        var offset = Tree.Script.GetOffset(line, col);
        if (offset == Tree.Script.SourceText.Length) {
            offset--;
        }

        return offset < 0 ? null : TokenAt(offset);
    }

    public SyntaxNode NameNodeAt(int line, int col) {
        var token = TokenAt(line, col);
        if (token is null) {
            return null;
        }

        if (token is NameToken or NumberToken or StringToken) {
            return token.Parent;
        }

        token = TokenLeftBiasedAt(line, col);
        return token?.Parent;
    }
    public SyntaxNode NodeAt(int line, int col) {
        var token = TokenAt(line, col);
        return token?.Parent;
    }
    public SyntaxNode FindNode(SourceRange range, SyntaxKind kind) {
        SyntaxNode node = this as SyntaxNode;
        while (node != null) {
            if (node.SourceRange.Equals(range) && node.Kind == kind) {
                return node;
            }

            node = node.ChildrenNode.FirstOrDefault(it => it.SourceRange.Contains(range));
        }

        return null;
    }

    /// <summary>
    /// Check if the current node is after the given element, ie
    /// (a + b) - b is after a
    /// </summary>
    public bool IsAfter(SyntaxElement element) {
        var parent = Parent;
        if (parent is null) {
            return false;
        }

        var start = parent.ChildStartIndex;
        if (start == -1) {
            return false;
        }

        var elementId = element.ElementId;
        for (var i = ElementId - 1; i >= start; i--) {
            if (i == elementId) {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Check if the current node is before the given element, ie
    /// (a + b) `a.IsBefore(b)` is true
    /// </summary>
    public bool IsBefore(SyntaxElement element) {
        var parent = Parent;
        if (parent is null) {
            return false;
        }

        var finish = parent.ChildFinishIndex;
        if (finish == -1) {
            return false;
        }

        var elementId = element.ElementId;
        for (var i = ElementId + 1; i <= finish; i++) {
            if (i == elementId) {
                return true;
            }
        }

        return false;
    }
}