using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.Interpreter.Execution.Statements;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.RuntimeValues.Prototypes;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.RuntimeValues.Values;
using SharpX;

namespace CSScriptingLang.Interpreter.Execution.Declaration;

[ASTNode]
public abstract partial class TypeDeclaration : Statement, ITopLevelDeclarationNode
{
    public StructPrototype Prototype { get; set; } = null;

    [VisitableNodeProperty]
    public IdentifierExpression Name { get; set; }

    [VisitableNodeProperty]
    public TypeDeclarationMembersNode Members { get; set; } = new();

    [VisitableNodeProperty]
    public TypeDeclarationMethodsNode Methods { get; set; } = new();

    public DeclarationContext DeclarationContext { get; set; } = new();

    protected TypeDeclaration(IdentifierExpression name) {
        Name = name;
    }

    public override Maybe<ValueReference> Execute(ExecContext ctx) {
        return base.Execute(ctx);
    }
}

[ASTNode]
public partial class TypeDeclarationMemberNode : BaseNode
{
    [VisitableNodeProperty]
    public IdentifierExpression Name { get; set; }

    [VisitableNodeProperty]
    public IdentifierExpression TypeIdentifier { get; set; }

    public TypeDeclarationMemberNode(IdentifierExpression name) {
        Name = name;
    }
    public TypeDeclarationMemberNode(IdentifierExpression name, IdentifierExpression type) {
        Name           = name;
        TypeIdentifier = type;
        TypeName       = type.Name;

        StartToken = name.StartToken;
        EndToken   = type.EndToken;
    }
}

[ASTNode]
public partial class TypeDeclarationMembersNode : NodeList<TypeDeclarationMemberNode>
{
    public TypeDeclarationMembersNode() { }
    public TypeDeclarationMembersNode(IEnumerable<TypeDeclarationMemberNode> arguments) : base(arguments) { }

    public override void OnNodeAdded(TypeDeclarationMemberNode node) {
        base.OnNodeAdded(node);
    }

    public override string ToString(int indent = 0) {
        var str = $"{new string(' ', indent)}{GetType().Name} ";

        return str;
    }
}

[ASTNode]
public partial class TypeDeclarationMethodsNode : NodeList<FunctionDeclaration>
{
    public TypeDeclarationMethodsNode() { }
    public TypeDeclarationMethodsNode(IEnumerable<FunctionDeclaration> arguments) : base(arguments) { }

    public override void OnNodeAdded(FunctionDeclaration node) {
        base.OnNodeAdded(node);
    }

    public override string ToString(int indent = 0) {
        var str = $"{new string(' ', indent)}{GetType().Name} ";

        return str;
    }
}

[ASTNode]
public abstract partial class TypeDeclarationNode<T> : TypeDeclaration
{
    protected TypeDeclarationNode(IdentifierExpression name) : base(name) { }
}

[ASTNode]
public partial class StructDeclaration : TypeDeclarationNode<StructDeclaration>
{
    public StructDeclaration(IdentifierExpression name) : base(name) { }


    public override Maybe<ValueReference> Execute(ExecContext ctx) {
        if (Prototype == null) {
            var (obj, proto) = TypesTable.DeclareStruct(ctx, this);

            Prototype = proto;
        }

        return ctx.ValReference(Prototype.Proto).ToMaybe();
    }
}

[ASTNode]
public partial class InterfaceDeclarationNode : TypeDeclarationNode<StructDeclaration>
{
    public InterfaceDeclarationNode(IdentifierExpression name) : base(name) { }
}