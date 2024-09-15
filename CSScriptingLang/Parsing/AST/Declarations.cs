using System.Reflection;
using CSScriptingLang.Interpreter;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.Interpreter.Execution.Statements;
using CSScriptingLang.Interpreter.Modules;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.RuntimeValues.Values;
using CSScriptingLang.Utils;
using SharpX;
using Module = CSScriptingLang.Interpreter.Modules.Module;

namespace CSScriptingLang.Parsing.AST;

public interface IDeclarationNode { }

public class DeclarationContext
{
    public Module DeclaringModule { get; set; }
    public Script DeclaringScript { get; set; }

    public void Set(Script script) {
        DeclaringScript = script;
        DeclaringModule = script.Module;
    }
}

public interface ITopLevelDeclarationNode : IDeclarationNode
{
    public DeclarationContext DeclarationContext { get; set; }
}

[ASTNode]
public partial class VariableInitializerNode : BaseNode
{
    [VisitableNodeProperty]
    public IdentifierExpression Variable { get; set; }

    public string Name => Variable.Name;

    [VisitableNodeProperty]
    public Expression Val { get; set; }

    public VariableInitializerNode() { }
    public VariableInitializerNode(IdentifierExpression variable, Expression val) {
        Variable = variable;
        Val      = val;
    }

    public ValueReference Execute(ExecContext ctx) {
        if (ctx.Variables.Get(Name, out var symbol)) {
            if (!symbol.IsBaseDeclaration) {
                ctx.LogError(this, $"Variable '{Name}' already declared");
            }
        }

        if (Val is LiteralValueExpression literal) {
            var literalResult = literal.Execute(ctx);

            var literalSymbol = ctx.Variables.Declare(Name, literalResult.Value);

            return ctx.VariableAccessReference(literalSymbol);
        }

        var valueResult = ctx.ExecuteRValue(Val.Execute);

        ctx.Variables.Set(
            Name, valueResult.Value?.GetOrClone() ?? Value.Null()
        );

        return ctx.VariableAccessReference(ctx.Variables.Get(Name));
    }
}

[ASTNode]
public partial class VariableDeclarationNode : Statement, ITopLevelDeclarationNode
{
    public DeclarationContext DeclarationContext { get; set; } = new();

    public TupleListDeclarationNode InitialVariable { get; set; }
    public TupleListDeclarationNode InitialValue    { get; set; }

    public List<VariableInitializerNode> Initializers { get; set; } = new();

    [VisitableNodeProperty]
    public IEnumerable<IdentifierExpression> Variables => Initializers.Select(x => x.Variable);

    [VisitableNodeProperty]
    public IEnumerable<Expression> Values => Initializers.Select(x => x.Val);


    public IEnumerable<string> VariableNames => Variables.Select(x => x.Name);


    public bool HasInitializer<T>() where T : BaseNode {
        return Initializers.Any(x => x.Val is T);
    }

    public VariableDeclarationNode() { }

    public void AddPairs(BaseNode varNodes, BaseNode exprNodes) {
        if (InitialValue == null && varNodes is TupleListDeclarationNode initVariable) {
            InitialVariable = initVariable;
        }

        if (InitialValue == null && exprNodes is TupleListDeclarationNode initValue) {
            InitialValue = initValue;
        }

        {
            if (varNodes is ExpressionList varList) {
                if (exprNodes is ExpressionList exprList) {
                    for (var i = 0; i < varList.Expressions.Count; i++) {
                        AddPair((IdentifierExpression) varList.Expressions[i], exprList.Expressions[i]);
                    }
                }

                if (exprNodes is RangeExpression range) {
                    for (var i = 0; i < varList.Expressions.Count; i++) {
                        AddPair((IdentifierExpression) varList.Expressions[i], range);
                    }
                }
                return;
            }
        }
        {
            if (varNodes is TupleListDeclarationNode varList) {
                if (exprNodes is TupleListDeclarationNode exprList) {
                    for (var i = 0; i < varList.Nodes.Count; i++) {
                        AddPair(varList.Nodes[i] as IdentifierExpression, exprList.Nodes[i] as Expression);
                    }

                    return;
                }

                if (exprNodes is ExpressionList exprs) {
                    for (var i = 0; i < varList.Nodes.Count; i++) {
                        AddPair(varList.Nodes[i] as IdentifierExpression, exprs.Expressions[i]);
                    }

                    return;
                }

                if (exprNodes is Expression nodes) {
                    for (var i = 0; i < varList.Nodes.Count; i++) {
                        AddPair((IdentifierExpression) varList.Nodes[i], nodes);
                    }

                    return;
                }
            }
        }

        if (varNodes is IdentifierExpression varNode && exprNodes is Expression exprNode) {
            AddPair(varNode, exprNode);
            return;
        }

        throw new ArgumentException($"Invalid arguments ({varNodes.GetType().ToFullLinkedName()}, {exprNodes.GetType().ToFullLinkedName()}) for VariableDeclarationNode.AddPairs");
    }

    public void AddPair(IdentifierExpression varNode, Expression exprNode) {
        Initializers.Add(new VariableInitializerNode(varNode, exprNode));
    }

    public override Maybe<ValueReference> Execute(ExecContext ctx) {
        return Initializers[0].Execute(ctx).ToMaybe();
    }
    public override IEnumerable<Maybe<ValueReference>> ExecuteMulti(ExecContext ctx) {
        foreach (var initializer in Initializers) {
            yield return initializer.Execute(ctx).ToMaybe();
        }
    }
    
}

[ASTNode]
public partial class ArgumentDeclarationNode : BaseNode
{
    public string Name { get; set; }

    public bool IsVariadic  { get; set; }
    public Type VarArgsType => IsVariadic ? typeof(object[]) : null;
    public Type NativeType  { get; set; }
    public int  Index       { get; set; }
    public bool IsNative    { get; set; }
    public bool IsOptional  { get; set; }

    public ArgumentDeclarationNode() { }
    public ArgumentDeclarationNode(string name, string type) {
        Name     = name;
        TypeName = type;
    }

    public static ArgumentDeclarationNode VarArgs(string name) => new(name, "object[]") {IsVariadic = true};

    public override string ToString(int indent = 0) {
        return $"{new string(' ', indent)}{GetType().Name}: {Name} ({Type})";
    }
}

[ASTNode]
public partial class ArgumentListDeclarationNode : NodeList<ArgumentDeclarationNode>
{
    [VisitableNodeProperty]
    public List<ArgumentDeclarationNode> Arguments => Nodes;

    public bool HasVarArgs   { get; set; }
    public int  VarArgsIndex { get; set; }

    public ArgumentListDeclarationNode() { }
    public ArgumentListDeclarationNode(IEnumerable<ArgumentDeclarationNode> arguments) : base(arguments) { }


    public int GetValidArgumentCount(int callArgCount) {
        if (HasVarArgs) {
            return callArgCount;
        }

        return Math.Min(callArgCount, Arguments.Count);
    }

    public bool Get(int index, out ArgumentDeclarationNode node) {
        if (index < 0) {
            node = null;
            return false;
        }

        if (HasVarArgs && index >= VarArgsIndex) {
            node = Arguments[VarArgsIndex];
            return true;
        }

        if (index >= Arguments.Count) {
            if (HasVarArgs) {
                node = Arguments[VarArgsIndex];
                return true;
            }

            node = null;
            return false;
        }

        node = Arguments[index];
        return true;
    }

    public override void OnNodeAdded(ArgumentDeclarationNode node) {
        base.OnNodeAdded(node);

        if (node.IsVariadic && !HasVarArgs) {
            HasVarArgs   = true;
            VarArgsIndex = node.IsNative ? node.Index - 1 : node.Index;
        }

    }

    public override string ToString(int indent = 0) {
        var str = $"{new string(' ', indent)}{GetType().Name} ";

        if (Arguments.Count == 0) {
            return str + "(empty)";
        }

        str += PrintNodes(indent);

        return str;
    }


}

[ASTNode]
public partial class TupleListDeclarationNode : NodeList<BaseNode>
{
    public TupleListDeclarationNode() { }
    public TupleListDeclarationNode(IEnumerable<BaseNode> arguments) : base(arguments) { }

    public override string ToString(int indent = 0) {
        var str = $"{new string(' ', indent)}{GetType().Name} ";

        if (Nodes.Count == 0) {
            return str + "(empty)";
        }

        str += PrintNodes(indent);

        return str;
    }

}

[ASTNode]
public partial class DefDeclaration_FunctionNode : BaseNode, ITopLevelDeclarationNode
{
    [VisitableNodeProperty]
    public ArgumentListDeclarationNode Parameters { get; set; } = new();

    public DeclarationContext DeclarationContext { get; set; } = new();

    public string Name { get; set; }

    public bool IsStatic    { get; set; }
    public bool IsNative    { get; set; }
    public bool IsAsync     { get; set; }
    public bool IsCoroutine { get; set; }

    private Action<FunctionExecContext> _nativeFunction;
    public Action<FunctionExecContext> NativeFunction {
        get => _nativeFunction;
        set {
            _nativeFunction = value;
            IsNative        = value != null;
        }
    }

    public DefDeclaration_FunctionNode(string name) {
        Name = name;
    }

    public override string ToString(int indent = 0) {
        var str = $"{new string(' ', indent)}{GetType().Name}: " +
                  $"({Parameters.ToString(0)}) {{\n" +
                  $"{new string(' ', indent)}}}";

        return str;
    }
}

[ASTNode]
public partial class SignalDeclarationNode : BaseNode, ITopLevelDeclarationNode
{
    public DeclarationContext DeclarationContext { get; set; } = new();

    [VisitableNodeProperty]
    public ArgumentListDeclarationNode Parameters { get; set; } = new();

    public string Name { get; set; }

    // public RuntimeTypeInfo_Signal Type { get; set; }

    public SignalDeclarationNode() { }
    public SignalDeclarationNode(ArgumentListDeclarationNode parameters) {
        Parameters = parameters;
    }


    public override string ToString(int indent = 0) {
        var str = $"{new string(' ', indent)}{GetType().Name}: " +
                  $"({Parameters.ToString(0)})";

        return str;
    }
}