/*using System;
using CSScriptingLang.Interpreter.Execution.Declaration;
using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.Lexing;
using CSScriptingLang.RuntimeValues.Values;

namespace CSScriptingLang.RuntimeValues.Prototypes
{
    partial class StructPrototype
    {
        public class MemberMetaa
        {
            public TypeDeclarationMemberNode Declaration { get; set; }
            public string Name => Declaration.Name;
            public TypeDeclarationMemberNode.MemberType MemberType => Declaration.Type;
            public IEnumerable<AttributeDeclaration> Attributes => Declaration.Attributes;

            public class JsonAttribute
            {
                public string Name { get; set; }
                public AttributeDeclaration Declaration { get; set; }
            }

            public List<JsonAttribute> JsonAttributes { get; set; }
            public Func<Value> ValueConstructor { get; set; }

            public MemberMetaa(TypeDeclarationMemberNode declaration)
            {
                Declaration = declaration;
                JsonAttributes = Attributes.Where(attr => attr.Name.Name.Equals("jsonable", StringComparison.CurrentCultureIgnoreCase)).Select(attr =>
                {
                    var pName = declaration.Name.Name;
                    foreach (var arg in attr.Args)
                    {
                        if (arg is not StringExpression str)
                        {
                            throw new InterpreterRuntimeException("Expected a string argument for Jsonable attribute");
                        }

                        pName = str.RTValue;
                    }

                    return new JsonAttribute
                    {
                        Name = pName,
                        Declaration = attr,
                    };
                }).ToList();
            }
        }

        public List<PrototypeMembersMixin.MemberMetaa>        Declared              { get; set; } = new();
        public IEnumerable<PrototypeMembersMixin.MemberMetaa> DeclareddFields       => Declared.Where(m => m.MemberType == TypeDeclarationMemberNode.MemberType.Field);
        public IEnumerable<PrototypeMembersMixin.MemberMetaa> DeclareddMethods      => Declared.Where(m => m.MemberType == TypeDeclarationMemberNode.MemberType.Method);
        public IEnumerable<PrototypeMembersMixin.MemberMetaa> DeclareddConstructors => Declared.Where(m => m.MemberType == TypeDeclarationMemberNode.MemberType.Constructor);
    }
}*/