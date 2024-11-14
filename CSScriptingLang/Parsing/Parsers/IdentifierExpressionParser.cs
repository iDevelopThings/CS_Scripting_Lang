using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.Lexing;

namespace CSScriptingLang.Parsing.Parsers;

public class IdentifierExpressionParser : SubParserType<IdentifierExpressionParser, IdentifierExpression>,
                                          IPrefixParser<IdentifierExpression>,
                                          IParserMatcher
{
    public bool Matches() {
        return Token.Is(TokenType.Identifier) && !Next.IsLAngle;
    }

    public override IdentifierExpression Parse() {
        var ident = EnsureAndConsume(TokenType.Identifier, "Expected identifier");
        return new IdentifierExpression(ident.Value) {
            StartToken = ident,
            EndToken   = ident,
        };
    }

}

public class TypeIdentifierExpressionParser : SubParserType<TypeIdentifierExpressionParser, TypeIdentifierExpression>,
                                              IPrefixParser<TypeIdentifierExpression>,
                                              IParserMatcher
{
    public bool Matches() {
        return Token.Is(TokenType.Identifier) && Next.IsLAngle && NextNext.IsIdentifier;
    }
    public override TypeIdentifierExpression Parse() {
        var identToken = EnsureAndConsume(TokenType.Identifier, "Expected identifier");

        var ident = new TypeIdentifierExpression(identToken.Value) {
            StartToken = identToken,
            EndToken   = identToken,
        };

        if (Token.IsLAngle) {
            ident.TypeParameters = TypeParametersListParser.ParseNode(ident) as TypeParametersList;
        }

        ident.EndToken = Prev;

        return ident;
    }

}