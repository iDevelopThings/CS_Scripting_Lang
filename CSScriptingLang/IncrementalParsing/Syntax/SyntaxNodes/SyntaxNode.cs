using System.Diagnostics;
using CSScriptingLang.Core.Diagnostics;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Modules;
using CSScriptingLang.Parsing.AST.NamedSymbol;
using CSScriptingLang.RuntimeValues.Prototypes.Types;
using CSScriptingLang.Utils;
using SharpX;

namespace CSScriptingLang.IncrementalParsing.Syntax.SyntaxNodes;

public interface IExecSingle
{
    Maybe<ValueReference> Execute(ExecContext ctx);
}

public interface IExecMulti
{
    IEnumerable<Maybe<ValueReference>> ExecuteMulti(ExecContext ctx);
}

public static class ExecExtensions
{
    public static Maybe<ValueReference> DoExecute(this IExecSingle exec, ExecContext ctx)
        => exec.Execute(ctx);

    public static Maybe<ValueReference> DoExecuteSingle(this SyntaxElement element, ExecContext ctx) {
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        if (element is IExecSingle single) {
            return single.Execute(ctx);
        }

        if (element is IExecMulti multi) {
            return multi.ExecuteMulti(ctx).FirstOrDefault();
        }

        throw new Exception($"Element {element?.GetType().ToShortName()} does not implement IExecSingle or IExecMulti");
    }

    public static IEnumerable<Maybe<ValueReference>> DoExecuteMulti(this IExecMulti exec, ExecContext ctx)
        => exec.ExecuteMulti(ctx);

    public static IEnumerable<Maybe<ValueReference>> DoExecute(this SyntaxElement element, ExecContext ctx) {
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        if (element is IExecSingle single) {
            yield return single.Execute(ctx);
        } else if (element is IExecMulti multi) {
            foreach (var valueReference in multi.ExecuteMulti(ctx)) {
                yield return valueReference;
            }
        } else {
            throw new Exception($"Element {element.GetType().ToShortName()} does not implement IExecSingle or IExecMulti");
        }
    }

    public static IEnumerable<(T element, Maybe<ValueReference> value)> DoExecute<T>(this IEnumerable<T> elements, ExecContext ctx)
        where T : SyntaxElement {
        foreach (var element in elements) {
            foreach (var valueReference in element.DoExecute(ctx)) {
                yield return (element, valueReference);
            }
        }
    }
}

public partial class SyntaxNode(int index, SyntaxTree tree) : SyntaxElement(index, tree)
{
    public DefinitionScope Scope { get; set; }

    public SyntaxKind Kind => (SyntaxKind) RawKind;

    public List<Ty> ResolvedTypes { get; set; }
    public Ty       ResolvedType  => ResolvedTypes?.FirstOrDefault();

    public virtual IEnumerable<Ty> ResolveAndCacheTypes(ExecContext ctx, DefinitionSymbol symbol) {
        if(ResolvedTypes != null)
            return ResolvedTypes;

        var resolved = ResolveTypes(ctx, symbol);
        ResolvedTypes = resolved.ToList();
        
        return ResolvedTypes;
    }

    public virtual IEnumerable<Ty> ResolveTypes(ExecContext ctx, DefinitionSymbol symbol) {
        Diagnostic_Warning($"Failed to resolve type for node of type {GetType().ToFullLinkedName()}");
        yield break;
    }

    public virtual IEnumerable<NamedSymbolInformation> FindReferences(DefinitionScope scope = null) {
        yield break;
    }

    
    public override IEnumerable<SyntaxElement> DescendantsAndSelf {
        get {
            var stack = new Stack<SyntaxElement>();
            stack.Push(this);
            while (stack.Count > 0) {
                var node = stack.Pop();
                yield return node;
                foreach (var child in node.ChildrenNode.Reverse()) {
                    stack.Push(child);
                }
            }
        }
    }

    public override IEnumerable<SyntaxNode> Descendants {
        get {
            var stack = new Stack<SyntaxNode>();
            foreach (var child in ChildrenNode.Reverse()) {
                stack.Push(child);
            }

            while (stack.Count > 0) {
                var node = stack.Pop();
                yield return node;
                foreach (var child in node.ChildrenNode.Reverse()) {
                    stack.Push(child);
                }
            }
        }
    }

    public override IEnumerable<SyntaxElement> DescendantsInRange(SourceRange range) {
        var           validChildren = new List<SyntaxElement>();
        SyntaxElement parentNode    = this;
        var           found         = false;
        do {
            found = false;
            foreach (var child in parentNode.ChildrenWithTokens) {
                if (child.SourceRange.Contains(range)) {
                    parentNode = child;
                    found      = true;
                    break;
                }
            }
        } while (found);

        foreach (var child in parentNode.ChildrenWithTokens) {
            if (child.SourceRange.Intersects(range)) {
                validChildren.Add(child);
            }
        }

        validChildren.Reverse();
        var stack = new Stack<SyntaxElement>(validChildren);
        while (stack.Count > 0) {
            var node = stack.Pop();
            if (node.SourceRange.Intersects(range)) {
                yield return node;
            }

            foreach (var child in node.ChildrenNode.Reverse()) {
                stack.Push(child);
            }
        }
    }

    public override IEnumerable<SyntaxElement> DescendantsWithToken {
        get {
            var stack = new Stack<SyntaxElement>();

            foreach (var child in ChildrenWithTokens.Reverse()) {
                stack.Push(child);
            }

            while (stack.Count > 0) {
                var node = stack.Pop();
                yield return node;
                // ReSharper disable once InvertIf
                if (node is SyntaxNode n) {
                    foreach (var child in n.ChildrenWithTokens.Reverse()) {
                        stack.Push(child);
                    }
                }
            }
        }
    }

    public override IEnumerable<SyntaxElement> DescendantsAndSelfWithTokens {
        get {
            var stack = new Stack<SyntaxElement>();
            stack.Push(this);
            while (stack.Count > 0) {
                var node = stack.Pop();
                yield return node;
                // ReSharper disable once InvertIf
                if (node is SyntaxNode n) {
                    foreach (var child in n.ChildrenWithTokens.Reverse()) {
                        stack.Push(child);
                    }
                }
            }
        }
    }


    public override string DebugContent() => "";

    // public virtual Maybe<ValueReference> Execute(ExecContext ctx) {
    //     DiagnosticManager.Diagnostic_Error_Fatal().Message($"Expression.Execute not implemented for {GetType().ToFullLinkedName()}").Report();
    //     return new ValueReference(ctx).ToMaybe();
    // }
    // public virtual IEnumerable<Maybe<ValueReference>> ExecuteMulti(ExecContext ctx) {
    //     yield return Execute(ctx);
    // }
}