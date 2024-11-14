using CSScriptingLang.Core.Diagnostics;
using CSScriptingLang.Interpreter.Execution.Declaration;
using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.Lexing;
using CSScriptingLang.Utils;

namespace CSScriptingLang.Parsing.Parsers;

public class TypeDeclarationParser : SubParserType<TypeDeclarationParser, TypeDeclaration>, IStatementParser<TypeDeclaration>, IParserMatcher
{
    public override TypeDeclaration Parse() {
        Parser.ParseAttributes();

        var start = EnsureAndConsume(Keyword.Type, "Expected 'type' keyword");
        var ident = IdentifierExpressionParser.ParseNode();
        var type  = EnsureAndConsume(Keyword.TypeDeclaration, "Expected type declaration(struct or interface)");

        TypeDeclaration node = type switch {
            {IsStructKeyword   : true} => new StructDeclaration(ident) {StartToken    = start},
            {IsInterfaceKeyword: true} => new InterfaceDeclaration(ident) {StartToken = start},
            {IsEnumKeyword     : true} => new EnumDeclaration(ident) {StartToken      = start},
            _                          => null,
        };

        if (node == null) {
            Expected("Expected type declaration(struct or interface)", start);
            return null;
        }

        node.Attributes.PushRange(Parser.AttributeStack);

        EnsureAndConsume(TokenType.LBrace, "Expected opening brace for type declaration");

        while (Token is {IsRBrace: false, IsEOF: false}) {
            var member = TypeDeclarationMemberParser.ParseNode(node);
            if (member != null) {
                node.Members.Add(member);
            }
        }

        var blockEnd = EnsureAndConsume(TokenType.RBrace, "Expected closing brace for type declaration");

        node.StartToken = start;
        node.EndToken   = blockEnd;

        return node;
    }



    public bool Matches() {
        return Token.IsTypeKeyword && Next.IsIdentifier && NextNext.IsTypeDeclarationKeyword;
    }
}

public class TypeDeclarationMemberParser : DependantSubParserType<TypeDeclarationMemberParser, TypeDeclarationMemberNode, TypeDeclaration>
{
    public override TypeDeclarationMemberNode Parse(TypeDeclaration typeDecl) {
        Parser.ParseAttributes();

        var structDeclaration    = (typeDecl as StructDeclaration)!;
        var interfaceDeclaration = (typeDecl as InterfaceDeclaration)!;
        var enumDeclaration      = (typeDecl as EnumDeclaration)!;

        Parser.ParseAttributes();

        if (enumDeclaration != null) {
            var member = ParseEnumMember(enumDeclaration);
            if (member != null) {
                return member;
            }
        }

        var startingToken = Token;

        var modifiers = Parser.ParseFunctionModifiers();

        if (structDeclaration != null && Token.IsFunctionKeyword) {
            Expected(
                "Incorrect method declaration syntax, usage is as follows: `type MyStruct struct { MyMethod(..) void; }`",
               (startingToken, Token)
            );
            return null;
        }

        if (Token.IsIdentifier && Next.IsLParen) {
            var fn = Parser.ParseFunctionDeclarationWithKeywords(
                requiresFunctionKeyword: false,
                requiresBody: typeDecl is StructDeclaration,
                applyModifiers: modifiers
            );

            AdvanceIfSemicolon();

            var isConstructor = typeDecl.Name.Name == fn.Name;

            return new TypeDeclarationMemberNode(fn.NameIdentifier, fn) {
                StartToken = fn.NameIdentifier.StartToken,
                EndToken   = fn.EndToken,
                Type       = isConstructor ? TypeDeclMemberType.Constructor : TypeDeclMemberType.Method,
            };
        }


        var fieldName = IdentifierExpressionParser.ParseNode();
        var fieldType = TypeIdentifierExpressionParser.ParseNode();

        var field = new TypeDeclarationMemberNode(fieldName, fieldType);
        field.Attributes.PushRange(Parser.AttributeStack);

        field.StartToken = fieldName.StartToken;
        field.EndToken   = fieldType.EndToken;

        return field;
    }

    private TypeDeclarationMemberNode ParseEnumMember(EnumDeclaration enumDeclaration) {
        if (Token.IsIdentifier && Next.IsLParen && Token.Value == enumDeclaration.Name.Name) {
            return null;
        }

        var ident = IdentifierExpressionParser.ParseNode();

        // We can have the following formats:
        //  - `EnumMember`
        //  - `EnumMember = 10`
        //  - `EnumMember(value, value2)`

        var idx = enumDeclaration.EnumMembers.Count();

        if (Token.IsAssignmentOperator) {
            Parser.Advance();

            var value = Parser.ParseExpression();
            return TypeDeclarationMemberNode.EnumMember(ident, value);
        } else if (Token.IsLParen) {
            var values = Parser.ParseArgumentsList();
            return TypeDeclarationMemberNode.EnumMember(ident, values);
        } else {
            var value = new Int32Expression(idx);
            return TypeDeclarationMemberNode.EnumMember(ident, value);
        }
    }

}



/*

// Method decl; `FuncName() Type {}`

if (Token.IsLParen) {

    var fn = new FunctionDeclaration(fieldName) {
        StartToken = fieldName.StartToken,
    };

    fn.Attributes.PushRange(Parser.AttributeStack);
    fn.Parameters = ArgumentDeclarationListParser.ParseNode();

    if (Token is {IsIdentifier: true, IsLBrace: false}) {
        fn.ReturnType = TypeIdentifierExpressionParser.ParseNode();
    }

    if (typeDecl is StructDeclaration) {
        var body = BlockExpressionParser.ParseNode();
        fn.Body.Nodes.AddRange(body.Nodes);
        AdvanceIfSemicolon();
        fn.EndToken = body.EndToken;
        if (!fn.HasReturnStatementDefined) {
            fn.Body.Add(new ReturnStatement(null));
        }
    } else {
        AdvanceIfSemicolon();
        fn.EndToken = Token;
    }

    return new TypeDeclarationMemberNode(fieldName, fn) {
        StartToken = fieldName.StartToken,
        EndToken   = fn.EndToken,
    };
}
TypeIdentifierExpression fieldType      = null;
var                      fieldTypeExpr  = Parser.ParseExpression();
var                      typedFieldType = fieldTypeExpr as TypeIdentifierExpression;
if (typedFieldType != null) {
    fieldType = typedFieldType;
} else {
    Console.WriteLine(fieldTypeExpr);
}
if (Token.Value == "Array" || Token.Value == "Data") {
    var fieldTypeExpr  = Parser.ParseExpression();
    var typedFieldType = fieldTypeExpr as TypeIdentifierExpression;
    if (typedFieldType != null) {
        fieldType = typedFieldType;
    } else {
        Console.WriteLine(fieldTypeExpr);
    }
}*/