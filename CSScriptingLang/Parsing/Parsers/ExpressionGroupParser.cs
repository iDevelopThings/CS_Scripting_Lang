using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.Lexing;

namespace CSScriptingLang.Parsing.Parsers;

public class ExpressionGroupParser : SubParserType<ExpressionGroupParser, Expression>, IPrefixParser<Expression>
{
    public override Expression Parse() {
        // We want to search for lambdas
        //   `(int a, int b) => { return a + b; }`
        // but to do that, we need to know if we have:
        //   `(... skip ...) => {`
        if (
            LookAheadSequential(
                [() => Token.IsRParen, () => Token.IsArrow, /*() => Token.IsLBrace*/],
                [() => Token.IsLParen || Token.IsRBrace]
            ).Execute((l, m) => l.Rollback())
        ) {
            return ((Parser) ParentParser).ParseLambdaFunction();
        }

        // tuple = var (x, y, z), we're searching for `(x,`
        if (Sequence(TokenType.LParen, TokenType.Identifier, TokenType.Comma)) {
            return ((Parser) ParentParser).ParseExpressionList();
        }
        var exprCursorPos = Cursor;
        var s             = EnsureAndConsume(TokenType.LParen, "Expected opening parenthesis"); // consume '('

        var expression = Parser.ParseExpression();

        if (Token.IsComma) {
            Lexer.Rollback(exprCursorPos);
            return Parser.ParseExpressionList();
        }

        var e = EnsureAndConsume(TokenType.RParen, "Expected closing parenthesis"); // consume ')'

        expression.StartToken = s;
        expression.EndToken   = e;

        return expression;
    }
}