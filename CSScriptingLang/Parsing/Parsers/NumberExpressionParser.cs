using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.Lexing;

namespace CSScriptingLang.Parsing.Parsers;

public class NumberExpressionParser : SubParserType<NumberExpressionParser, LiteralNumberExpression>, IPrefixParser<LiteralNumberExpression>
{
    public override LiteralNumberExpression Parse() {
        var expr = LiteralNumberExpression.CreateFromToken(Token);
        expr.StartToken = Token;
        expr.EndToken   = Token;
        Advance();
        return expr;
    }
}
public class BoolExpressionParser : SubParserType<BoolExpressionParser, BooleanExpression>, IPrefixParser<BooleanExpression>
{
    public override BooleanExpression Parse() {
        var expr = new BooleanExpression(bool.Parse(Token.Value)) {
            StartToken = Token,
            EndToken   = Token,
        };
        Advance();
        return expr;
    }
}

public class StringExpressionParser : SubParserType<StringExpressionParser, StringExpression>, IPrefixParser<StringExpression>
{
    public override StringExpression Parse() {
        var tok = EnsureAndConsume(TokenType.String, "Expected string");
        var expr = new StringExpression(tok.Value) {
            StartToken = tok,
            EndToken   = Prev,
        };
        return expr;
    }
}