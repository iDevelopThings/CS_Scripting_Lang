using CSScriptingLang.Common.Extensions;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Execution.Declaration;
using CSScriptingLang.Interpreter.Modules;
using CSScriptingLang.Lexing;
using CSScriptingLang.Parsing.AST.NamedSymbol;
using CSScriptingLang.RuntimeValues.Prototypes;
using CSScriptingLang.RuntimeValues.Prototypes.Types;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.Utils;
using SharpX;

namespace CSScriptingLang.IncrementalParsing.Syntax.SyntaxNodes;

[SyntaxNode]
public partial class TypeDeclaration(int index, SyntaxTree tree) : SyntaxNode(index, tree), IDeclaration, IExecSingle, INamedSymbolProvider
{
    public Prototype Prototype { get; set; }

    public IdentifierExpr Name => ChildNode<IdentifierExpr>();

    public IEnumerable<TypeDeclMember> Members      => ChildNodes<TypeDeclMember>();
    public IEnumerable<TypeDeclMember> Fields       => ChildNodes<TypeDeclMember>(m => m.MemberKind == TypeDeclMemberType.Field);
    public IEnumerable<TypeDeclMember> Methods      => ChildNodes<TypeDeclMember>(m => m.MemberKind == TypeDeclMemberType.Method);
    public IEnumerable<TypeDeclMember> Constructors => ChildNodes<TypeDeclMember>(m => m.MemberKind == TypeDeclMemberType.Constructor);
    public IEnumerable<TypeDeclMember> EnumMembers  => ChildNodes<TypeDeclMember>(m => m.MemberKind == TypeDeclMemberType.EnumMember);

    public override string DebugContent() {
        return DataContentBuilder.Create()
           .Add("struct")
           .Add(Name?.DebugContent())
           .Add("{")
           .Add(Fields.Select(f => f.DebugContent()).Join(" "))
           .Add(Methods.Select(m => m.DebugContent()).Join(" "))
           .Add("}")
           .ClearTrailingSpace();
    }

    public virtual Prototype HandleDeclaration(ExecContext ctx) {
        // return Prototype = new Prototype(Name.Value, this, ctx);
        return null;
    }
    public virtual Maybe<ValueReference> Execute(ExecContext ctx) {
        throw new NotImplementedException();
    }
    
    public IEnumerable<NamedSymbolInformation> GetNamedSymbols() {
        yield return new(this, Name, NamedSymbolKind.Struct, Name);
    }

}

public class TypeDeclaration<T, TPrototypeType>(int index, SyntaxTree tree) : TypeDeclaration(index, tree)
{
    public new TPrototypeType Prototype { get; set; }
}

[SyntaxNode]
public partial class TypeDeclMember(int index, SyntaxTree tree, TypeDeclMemberType type) : SyntaxNode(index, tree), INamedSymbolProvider
{
    public IdentifierExpr             Name         => FunctionDecl?.NameIdentifier ?? ChildNode<IdentifierExpr>();
    public TypedIdentifierExpr        Type         => ChildAfter<TypedIdentifierExpr>(Name);
    public FunctionDecl               FunctionDecl => ChildNode<FunctionDecl>();
    public IEnumerable<AttributeDecl> Attributes   => ChildNodes<AttributeDecl>();

    public IEnumerable<ExprSyntax> DefaultValues => ChildrenAfter<ExprSyntax>(Name);
    public ArgumentListExpr        Arguments     => ChildNode<ArgumentListExpr>();

    public virtual TypeDeclMemberType MemberKind { get; set; } = type;

    public override string GetDebugName() {
        return MemberKind switch {
            TypeDeclMemberType.UNKNOWN     => "TypeMember(Unknown)",
            TypeDeclMemberType.Constructor => "TypeMember(Constructor)",
            TypeDeclMemberType.Field       => "TypeMember(Field)",
            TypeDeclMemberType.Method      => "TypeMember(Method)",
            TypeDeclMemberType.EnumMember  => "TypeMember(EnumMember)",
            _                              => throw new ArgumentOutOfRangeException(),
        };
    }

    public override string DebugContent() {
        return DataContentBuilder.Create()
           .Add(Name?.DebugContent())
           .Add(Type?.DebugContent())
           .ClearTrailingSpace();
    }

    public override IEnumerable<Ty> ResolveTypes(ExecContext ctx, DefinitionSymbol symbol) {
        if (MemberKind == TypeDeclMemberType.Field) {
            foreach (var resolveType in Type.ResolveAndCacheTypes(ctx, symbol)) {
                yield return resolveType;
            }
        }
    }
    public IEnumerable<NamedSymbolInformation> GetNamedSymbols() {
        if (MemberKind == TypeDeclMemberType.Field) {
            yield return new(this, Name, NamedSymbolKind.StructField, Name);
        }
    }
}

[SyntaxNode]
public partial class StructDecl(int index, SyntaxTree tree) : TypeDeclaration<StructDecl, StructPrototype>(index, tree)
{
    public override string DebugContent() {
        return DataContentBuilder.Create()
           .Add("struct")
           .Add(Name?.DebugContent())
           .Add("{")
           .Add(Fields.Select(f => f.DebugContent()).Join(" "))
           .Add(Methods.Select(m => m.DebugContent()).Join(" "))
           .Add("}")
           .ClearTrailingSpace();
    }


    public override Prototype HandleDeclaration(ExecContext ctx) {
        if (Prototype == null) {
            Prototype = TypesTable.DeclareStruct(ctx, this);
        }
        return Prototype;
    }

    public override Maybe<ValueReference> Execute(ExecContext ctx) {
        HandleDeclaration(ctx);
        return ctx.ValReference(Prototype.Proto).ToMaybe();
    }


    public override IEnumerable<Ty> ResolveTypes(ExecContext ctx, DefinitionSymbol symbol) {
        var ty = Prototype.Ty;

        foreach (var prop in Fields) {
            var val = prop.ResolveAndCacheTypes(ctx, symbol).FirstOrDefault();
            ty.SetMember(prop.Name, val);
        }

        yield return ty;
    }
}

[SyntaxNode]
public partial class EnumDecl(int index, SyntaxTree tree) : TypeDeclaration<EnumDecl, EnumPrototype>(index, tree)
{
    public override string DebugContent() {
        return DataContentBuilder.Create()
           .Add("enum")
           .Add(Name?.DebugContent())
           .Add("{")
           .Add(Constructors.Select(f => f.DebugContent()).Join(" "))
           .Add(EnumMembers.Select(m => m.DebugContent()).Join(" "))
           .Add("}")
           .ClearTrailingSpace();
    }

    public override Prototype HandleDeclaration(ExecContext ctx) => Prototype ??= TypesTable.DeclareEnum(ctx, this);

    public override Maybe<ValueReference> Execute(ExecContext ctx) {
        var proto = HandleDeclaration(ctx);

        var value = ctx.Variables.Set(proto.ValueType.Name, proto.Proto);

        return ctx.VariableAccessReference(value).ToMaybe();
    }
}

[SyntaxNode]
public partial class InterfaceDecl(int index, SyntaxTree tree) : TypeDeclaration<EnumDecl, ObjectPrototype>(index, tree)
{
    public override string DebugContent() {
        return DataContentBuilder.Create()
           .Add("interface")
           .Add(Name?.DebugContent())
           .Add("{")
           .Add(Constructors.Select(f => f.DebugContent()).Join(" "))
           .Add(EnumMembers.Select(m => m.DebugContent()).Join(" "))
           .Add("}")
           .ClearTrailingSpace();
    }

    public override Prototype HandleDeclaration(ExecContext ctx) => Prototype ??= TypesTable.DeclareInterface(ctx, this);

    public override Maybe<ValueReference> Execute(ExecContext ctx) {
        HandleDeclaration(ctx);
        return ctx.ValReference(Prototype.Proto);
    }
}

/*[SyntaxNode]
public partial class StructDeclarationField(int index, SyntaxTree tree) : SyntaxNode(index, tree)
{
    public IdentifierExpr      Name => ChildNode<IdentifierExpr>();
    public TypedIdentifierExpr Type => ChildAfter<TypedIdentifierExpr>(Name);

    public override string DebugContent() {
        return DataContentBuilder.Create()
           .Add(Name?.DebugContent())
           .Add(Type?.DebugContent())
           .ClearTrailingSpace();
    }
}

[SyntaxNode]
public partial class StructDeclarationMethod(int index, SyntaxTree tree) : SyntaxNode(index, tree)
{
    public FunctionDecl Decl => ChildNode<FunctionDecl>();

    public override string DebugContent() {
        return Decl.DebugContent();
    }
}*/