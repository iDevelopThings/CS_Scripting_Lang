using System.Reflection;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Execution.Statements;
using CSScriptingLang.Lexing;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.Utils;

namespace CSScriptingLang.Interpreter.Execution.Expressions;

[ASTNode]
public partial class InlineFunctionDeclaration : Expression
{
    [VisitableNodeProperty]
    public ArgumentListDeclarationNode Parameters { get; set; } = new();

    [VisitableNodeProperty]
    public BlockExpression Body { get; set; } = new();

    public IEnumerable<BaseNode> Statements => Body.Nodes;

    [VisitableNodeProperty]
    public IdentifierExpression NameIdentifier { get; set; }
    
    public string Name { get; set; }

    public TypeReference ReturnType { get; set; }

    public bool            HasReturnStatementDefined => Body.Any(x => x is ReturnStatement);
    public ReturnStatement ReturnStatement           => Body.Nodes.OfType<ReturnStatement>().FirstOrDefault();

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

    private static int _idCounter = 0;
    public InlineFunctionDeclaration() {
        Name       = $"__anon_func_{_idCounter++}";
        ReturnType = new TypeReference(this, "unit");
    }

    public InlineFunctionDeclaration(IdentifierExpression name) {
        NameIdentifier = name;
        Name       = name;
        ReturnType = new TypeReference(this, "unit");
    }

    public override string ToString(int indent = 0) {
        var str = $"{new string(' ', indent)}{GetType().Name}: " +
                  $"({Parameters.ToString(0)}) {{\n" +
                  $"{Body.ToString(indent + 2)}\n" +
                  $"{new string(' ', indent)}}}";

        return str;
    }
    
    
    public override ValueReference Execute(ExecContext ctx) {
        var (val, symbol) = ctx.DeclareFunction(this);

        return ctx.VariableAccessReference(symbol);
    }
}

[ASTNode]
public partial class FunctionDeclaration : InlineFunctionDeclaration, ITopLevelDeclarationNode
{
    public DeclarationContext DeclarationContext { get; set; } = new();
    public FunctionDeclaration(IdentifierExpression name) : base(name) { }

    public override string ToString(int indent = 0) {
        var str = $"{new string(' ', indent)}{GetType().ToShortName()}: {Name}" +
                  $"({Parameters.ToString(0)}) {{\n" +
                  $"{Body.ToString(indent + 2)}\n" +
                  $"{new string(' ', indent)}}}";

        return str;
    }

    public FunctionDeclaration SetNative(bool isNative) {
        IsNative = isNative;
        return this;
    }

    public override ValueReference Execute(ExecContext ctx) {
        return base.Execute(ctx);
    }
}

[ASTNode]
public partial class NativeBoundFunctionDeclarationNode : FunctionDeclaration
{
    public MethodInfo MethodInfo { get; set; }

    public List<Type> NativeReturnTypes { get; set; } = new();

    public NativeBoundFunctionDeclarationNode(IdentifierExpression name, MethodInfo methodInfo) : base(name) {
        MethodInfo = methodInfo;
        IsNative   = true;
    }


    public override string ToString() {
        return $"{GetType().ToShortName()}: {Name} (NativeBinding)";
    }
}