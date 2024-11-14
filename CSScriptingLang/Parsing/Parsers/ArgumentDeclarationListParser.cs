using CSScriptingLang.Lexing;
using CSScriptingLang.Parsing.AST;

namespace CSScriptingLang.Parsing.Parsers;

public class ArgumentDeclarationListParser : SubParserType<ArgumentDeclarationListParser, ArgumentListDeclarationNode>
{
    public override ArgumentListDeclarationNode Parse() {
        var start = EnsureAndConsume(TokenType.LParen, "Expected opening parenthesis");

        var args = new ArgumentListDeclarationNode();

        while (!Token.IsRParen || Token.IsEOF) {
            var arg = new ArgumentDeclarationNode();

            if (Token.IsDotDotDot && Next.IsIdentifier) {
                arg.IsVariadic = true;
                arg.StartToken = Advance();
                arg.SetName(IdentifierExpressionParser.ParseNode());
            } else {
                arg.SetType(TypeIdentifierExpressionParser.ParseNode());
                arg.SetName(IdentifierExpressionParser.ParseNode());

                arg.StartToken = arg.TypeIdentifier.StartToken;
                arg.EndToken   = arg.Name.EndToken;
            }

            args.Add(arg);

            if (Token.IsComma) {
                Advance();
            }
            else if (!Token.IsRParen) {
                Diagnostic_Error_Fatal()
                   .Range(start)
                   .Message("Expected comma or closing parenthesis")
                   .Report();
            }
        }

        var end = EnsureAndConsume(TokenType.RParen, "Expected closing parenthesis");

        args.StartToken = start;
        args.EndToken   = end;

        return args;
    }

}