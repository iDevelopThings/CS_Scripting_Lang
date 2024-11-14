using System.Collections;
using System.Runtime.CompilerServices;
using CSScriptingLang.Core.Diagnostics;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Execution.Statements;
using CSScriptingLang.Lexing;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.Utils;
using SharpX;

namespace CSScriptingLang.Interpreter.Execution.Expressions;

public interface IExpression
{
}

[ASTNode]
public abstract partial class Expression : BaseNode, IExpression
{
    public virtual bool IsConstant => false;

    public virtual ValueReference Execute(ExecContext ctx) {
        DiagnosticManager.Diagnostic_Error_Fatal().Message($"Expression.Execute not implemented for {GetType().ToFullLinkedName()}").Range(this).Report();
        return new ValueReference(ctx);
    }

    public virtual IEnumerable<Maybe<ValueReference>> ExecuteEnumerable(ExecContext ctx) {
        yield return Execute(ctx).ToMaybe();
    }
    public virtual IEnumerable<ValueReference> ExecuteMulti(ExecContext ctx) {
        yield return Execute(ctx);
    }

}

// [ASTNode]
public abstract partial class BaseExpressionsList<T> : Expression, IEnumerable<T> where T : Expression
{
    public List<T> Expressions { get; set; } = [];

    public int Count => Expressions.Count;

    protected BaseExpressionsList() { }
    protected BaseExpressionsList(IEnumerable<T> nodes) {
        Expressions = nodes.ToList();
    }
    protected BaseExpressionsList(IEnumerable<BaseNode> statements) {
        Expressions = statements.Cast<T>().ToList();
    }

    public BaseExpressionsList<T> Add(T node) {
        if (node == null) {
            Console.WriteLine("Warning: Attempted to add null node to program node.");
            return this;
        }

        Expressions.Add(node);

        OnNodeAdded(node);

        return this;
    }
    public BaseExpressionsList<T> Add(BaseNode node, [CallerMemberName] string caller = "") {
        switch (node) {
            case null:
                Console.WriteLine($"[{caller}] Attempted to add null node to BaseExpressionsList<{typeof(T).Name}>.");
                return this;
            case T t:
                Add(t);
                break;
            default:
                Console.WriteLine($"[{caller}] Attempted to add node of type {node.GetType().Name} to BaseExpressionsList<{typeof(T).Name}>.");
                break;
        }

        return this;
    }

    public static implicit operator List<T>(BaseExpressionsList<T> nodeList) {
        return nodeList.Expressions;
    }
    public static BaseExpressionsList<T> operator +(BaseExpressionsList<T> nodeList, T node)
        => nodeList.Add(node);

    public virtual void OnNodeAdded(T node) { }

    public IEnumerable<TNode> NodesOfType<TNode>() where TNode : BaseNode
        => Expressions.OfType<TNode>();

    public override IEnumerable<BaseNode> AllNodes() {
        if (Expressions != null) {
            foreach (var node in Expressions) {
                if (node is BaseNode baseNode) {
                    yield return baseNode;
                }
            }
        }

        foreach (var node in base.AllNodes()) {
            yield return node;
        }
    }

    public IEnumerator<T> GetEnumerator() {
        return Expressions.GetEnumerator();
    }
    IEnumerator IEnumerable.GetEnumerator() {
        return ((IEnumerable) Expressions).GetEnumerator();
    }

    public T this[int index] => Expressions[index];

}


[ASTNode]
public partial class ExpressionListNode : NodeList<Expression>
{
    public List<Expression> Expressions     => Nodes;
    public List<BaseNode>   ExpressionNodes => Nodes.OfType<BaseNode>().ToList();
    
    public ExpressionListNode() { }
    public ExpressionListNode(IEnumerable<Expression> expressions) : base(expressions) { }
    public ExpressionListNode(Expression expr) : base([expr]) { }
    
    public IEnumerable<ValueReference> Execute(ExecContext ctx) {
        return Expressions.Select(e => e.Execute(ctx));
    }
}

[ASTNode]
public partial class ExpressionList : BaseExpressionsList<Expression>
{
    public new IEnumerable<ValueReference> Execute(ExecContext ctx) {
        return Expressions.Select(e => e.Execute(ctx));
    }
}