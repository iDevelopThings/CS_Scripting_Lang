using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.Interpreter.Execution.Statements;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.RuntimeValues.Prototypes;
using CSScriptingLang.RuntimeValues.Types;
using SharpX;

namespace CSScriptingLang.Interpreter.Execution.Declaration;

[ASTNode]
public partial class AttributeDeclaration : Statement, ITopLevelDeclarationNode
{
    [VisitableNodeProperty]
    public IdentifierExpression Name { get; set; }
    [VisitableNodeProperty]
    public ExpressionList Args { get; set; }

    public DeclarationContext DeclarationContext { get; set; } = new();

    public AttributeDeclaration(IdentifierExpression name) {
        Name = name;
    }

    public override Maybe<ValueReference> Execute(ExecContext ctx) {
        return Maybe.Nothing<ValueReference>();
    }
}

[ASTNode]
public abstract partial class TypeDeclaration : Statement, ITopLevelDeclarationNode
{
    public StructPrototype Prototype { get; set; } = null;

    [VisitableNodeProperty]
    public IdentifierExpression Name { get; set; }

    [VisitableNodeProperty]
    public TypeDeclarationMembersNode Members { get; set; } = new();

    public IEnumerable<TypeDeclarationMemberNode> Fields       => Members.Fields;
    public IEnumerable<TypeDeclarationMemberNode> Methods      => Members.Methods;
    public IEnumerable<TypeDeclarationMemberNode> Constructors => Members.Constructors;
    public IEnumerable<TypeDeclarationMemberNode> EnumMembers  => Members.EnumMembers;

    public DeclarationContext DeclarationContext { get; set; } = new();

    [VisitableNodeProperty]
    public List<AttributeDeclaration> Attributes { get; set; } = new();

    protected TypeDeclaration(IdentifierExpression name) {
        Name = name;
    }

}

[ASTNode]
public partial class TypeDeclarationMemberNode : BaseNode
{
    [VisitableNodeProperty]
    public IdentifierExpression Name { get; set; }

    [VisitableNodeProperty]
    public TypeIdentifierExpression TypeIdentifier { get; set; }

    [VisitableNodeProperty]
    public FunctionDeclaration FunctionDeclaration { get; set; }

    [VisitableNodeProperty]
    public List<AttributeDeclaration> Attributes { get; set; } = new();

    [VisitableNodeProperty]
    public ExpressionListNode DefaultValues { get; set; }

    public TypeDeclMemberType Type { get; set; }

    public TypeDeclarationMemberNode(IdentifierExpression name) {
        Name = name;
    }
    public TypeDeclarationMemberNode(IdentifierExpression name, TypeIdentifierExpression type) {
        Name           = name;
        TypeIdentifier = type;
        Type           = TypeDeclMemberType.Field;

        StartToken = name.StartToken;
        EndToken   = type.EndToken;
    }

    public TypeDeclarationMemberNode(IdentifierExpression name, FunctionDeclaration method) {
        Name                = name;
        FunctionDeclaration = method;
        Type                = TypeDeclMemberType.Method;

        StartToken = name.StartToken;
        EndToken   = method.EndToken;
    }

    public static TypeDeclarationMemberNode EnumMember(IdentifierExpression name, Expression value) {
        return new TypeDeclarationMemberNode(name) {
            DefaultValues = new ExpressionListNode(value),
            Type          = TypeDeclMemberType.EnumMember,
            StartToken    = name.StartToken,
            EndToken      = value.EndToken,
        };
    }

    public static TypeDeclarationMemberNode EnumMember(IdentifierExpression name, ExpressionListNode values) {
        return new TypeDeclarationMemberNode(name) {
            DefaultValues = values,
            Type          = TypeDeclMemberType.EnumMember,
            StartToken    = name.StartToken,
            EndToken      = values.EndToken,
        };
    }
}

public enum TypeDeclMemberType
{
    UNKNOWN,
    Field,
    Method,
    Constructor,
    EnumMember,
}

[ASTNode]
public partial class TypeDeclarationMembersNode : NodeList<TypeDeclarationMemberNode>
{
    public IEnumerable<TypeDeclarationMemberNode> Fields       => this.Where(x => x.Type == TypeDeclMemberType.Field);
    public IEnumerable<TypeDeclarationMemberNode> Methods      => this.Where(x => x.Type == TypeDeclMemberType.Method);
    public IEnumerable<TypeDeclarationMemberNode> Constructors => this.Where(x => x.Type == TypeDeclMemberType.Constructor);
    public IEnumerable<TypeDeclarationMemberNode> EnumMembers  => this.Where(x => x.Type == TypeDeclMemberType.EnumMember);

    public TypeDeclarationMembersNode() { }
    public TypeDeclarationMembersNode(IEnumerable<TypeDeclarationMemberNode> arguments) : base(arguments) { }

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

    public StructPrototype HandleDeclaration(ExecContext ctx) {
        if (Prototype == null) {
            Prototype = TypesTable.DeclareStruct(ctx, this);
        }
        return Prototype;
    }

    public override Maybe<ValueReference> Execute(ExecContext ctx) {
        HandleDeclaration(ctx);

        return ctx.ValReference(Prototype.Proto).ToMaybe();
    }
}

[ASTNode]
public partial class InterfaceDeclaration : TypeDeclarationNode<StructDeclaration>
{
    public InterfaceDeclaration(IdentifierExpression name) : base(name) { }

    public new ObjectPrototype Prototype { get; set; } = null;

    public ObjectPrototype HandleDeclaration(ExecContext ctx) {
        if (Prototype == null) {
            Prototype = TypesTable.DeclareInterface(ctx, this);
        }
        return Prototype;
    }

    public override Maybe<ValueReference> Execute(ExecContext ctx) {
        HandleDeclaration(ctx);

        return ctx.ValReference(Prototype.Proto).ToMaybe();
    }
}

[ASTNode]
public partial class EnumDeclaration : TypeDeclarationNode<EnumDeclaration>
{
    public EnumDeclaration(IdentifierExpression name) : base(name) { }

    public new EnumPrototype Prototype { get; set; } = null;

    public EnumPrototype HandleDeclaration(ExecContext ctx) {
        if (Prototype == null) {
            Prototype = TypesTable.DeclareEnum(ctx, this);
        }
        return Prototype;
    }

    public override Maybe<ValueReference> Execute(ExecContext ctx) {
        var proto = HandleDeclaration(ctx);

        var value = ctx.Variables.Set(proto.ValueType.Name, proto.Proto);

        value.Val.ToDebugString();
        
        return ctx.VariableAccessReference(value).ToMaybe();
    }
}