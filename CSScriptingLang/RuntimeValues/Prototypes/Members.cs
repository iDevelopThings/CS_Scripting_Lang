using CSScriptingLang.IncrementalParsing.Syntax.SyntaxNodes;
using CSScriptingLang.Interpreter.Execution.Declaration;
using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.Lexing;
using CSScriptingLang.Mixins;
using CSScriptingLang.RuntimeValues.Values;

namespace CSScriptingLang.RuntimeValues.Prototypes;

public class MemberMeta
{
    public int                       Ordinal     { get; set; } = -1;
    public TypeDeclarationMemberNode Declaration { get; set; }
    public TypeDeclMember            Decl        { get; set; }

    public string             Name       => Declaration?.Name ?? Decl?.Name ?? "";
    public TypeDeclMemberType MemberType => Declaration?.Type ?? Decl?.MemberKind ?? TypeDeclMemberType.UNKNOWN;

    public class JsonAttribute
    {
        public string Name { get; set; }
        // public AttributeDeclaration Declaration { get; set; }
    }

    public List<JsonAttribute> JsonAttributes   { get; set; }
    public Func<Value>         ValueConstructor { get; set; }

    public MemberMeta(TypeDeclarationMemberNode declaration) {
        Declaration = declaration;

        JsonAttributes = Declaration?.Attributes
           .Where(attr => attr.Name.Name.Equals("jsonable", StringComparison.CurrentCultureIgnoreCase))
           .Select(attr => {
                var pName = declaration.Name.Name;
                foreach (var arg in attr.Args) {
                    if (arg is not StringExpression str) {
                        throw new InterpreterRuntimeException("Expected a string argument for Jsonable attribute");
                    }
                    pName = str.RTValue;
                }
                return new JsonAttribute {
                    Name = pName,
                    // Declaration = attr,
                };
            })
           .ToList();
    }
    public MemberMeta(TypeDeclMember declaration) {
        Decl = declaration;

        JsonAttributes = Decl.Attributes
           .Where(attr => attr.Name.AsString().Equals("jsonable", StringComparison.CurrentCultureIgnoreCase))
           .Select(attr => {
                var pName = declaration.Name.AsString();
                foreach (var arg in attr.Arguments) {
                    if (arg is not StringExpr str) {
                        throw new InterpreterRuntimeException("Expected a string argument for Jsonable attribute");
                    }
                    pName = str.RawValue;
                }
                return new JsonAttribute {
                    Name = pName,
                    // Declaration = attr,
                };
            })
           .ToList();
    }
}

public interface IPrototypeMembers
{
    List<MemberMeta>        DeclaredMembers      { get; set; }
    IEnumerable<MemberMeta> DeclaredFields       { get; }
    IEnumerable<MemberMeta> DeclaredMethods      { get; }
    IEnumerable<MemberMeta> DeclaredConstructors { get; }
}

[Mixin(
    "CSScriptingLang.Interpreter.Execution.Declaration",
    "CSScriptingLang.Interpreter.Execution.Expressions",
    "CSScriptingLang.Lexing",
    "CSScriptingLang.RuntimeValues.Values"
)]
public class PrototypeMembersMixin
{
    public List<MemberMeta> DeclaredMembers { get; set; } = new();

    public IEnumerable<MemberMeta> DeclaredFields       => DeclaredMembers.Where(m => m.MemberType == TypeDeclMemberType.Field);
    public IEnumerable<MemberMeta> DeclaredMethods      => DeclaredMembers.Where(m => m.MemberType == TypeDeclMemberType.Method);
    public IEnumerable<MemberMeta> DeclaredConstructors => DeclaredMembers.Where(m => m.MemberType == TypeDeclMemberType.Constructor);
    public IEnumerable<MemberMeta> DeclaredEnumMembers  => DeclaredMembers.Where(m => m.MemberType == TypeDeclMemberType.EnumMember);
}