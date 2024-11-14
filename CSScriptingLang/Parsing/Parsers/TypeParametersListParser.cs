using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.Lexing;

namespace CSScriptingLang.Parsing.Parsers;

public class TypeParametersListParser : SubParserType<TypeParametersListParser, Expression>, IInfixParser<Expression>, IParserMatcher
{
    public int Precedence => (int) PrecedenceValue.Postfix;

    public bool Matches() {
        return Prev.Is(TokenType.Identifier) && Token.Is(TokenType.LAngle) && Next.Is(TokenType.Identifier);
    }

    public override Expression Parse(Expression left) {
        var start = EnsureAndConsume(TokenType.LAngle, "Expected '<' before type parameters");

        var args = new TypeParametersList() {
            StartToken = start,
        };

        while (!Token.Is(TokenType.RAngle | TokenType.EOF)) {
            var id = EnsureAndConsume(TokenType.Identifier, "Expected type parameter");
            var node = new TypeParameterNode {
                StartToken = id,
                EndToken   = id,
                Name       = id.Value,
            };
            args.Add(node);

            if (Token.IsComma) {
                Advance();
            } else if (!Token.IsRAngle) {
                Expected("Expected comma or closing angle bracket");
                return null;
            }
        }

        EnsureAndConsume(TokenType.RAngle, "Expected closing angle bracket");

        args.EndToken = Prev;

        if (left is TypeIdentifierExpression type) {
            type.TypeParameters = args;
            left.EndToken       = args.EndToken;
            return left;
            
        } else {
            Console.WriteLine("Expected type identifier before type parameters");
        }

        return args;
    }

}