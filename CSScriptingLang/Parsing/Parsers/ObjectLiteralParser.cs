using CSScriptingLang.Interpreter.Execution.Declaration;
using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.Interpreter.Execution.Statements;
using CSScriptingLang.Lexing;
using CSScriptingLang.Utils;

namespace CSScriptingLang.Parsing.Parsers;

public class ObjectLiteralParser : SubParserType<ObjectLiteralParser, ObjectLiteralExpression>, IPrefixParser<ObjectLiteralExpression>
{
    public override ObjectLiteralExpression Parse() {
        EnsureAndConsume(TokenType.LBrace, "Expected opening brace");

        var obj = new ObjectLiteralExpression();

        while (!Token.Is(TokenType.RBrace | TokenType.EOF)) {
            var key = EnsureAnyOfAndConsume(
                TokenType.Identifier | TokenType.String | TokenType.Int32 | TokenType.Int64,
                "Expected identifier as key"
            );
            EnsureAndConsume(TokenType.Colon, "Expected colon after key");

            var value = Parser.ParseExpression();

            obj.AddProperty(key.Value, value);

            if (Token.IsComma) {
                Advance();
            } else if (!Token.IsRBrace) {
                Expected("Expected comma or closing brace");
                return null;
            }
        }

        EnsureAndConsume(TokenType.RBrace, "Expected closing brace");

        return obj;
    }

}

public class ArrayLiteralParser : SubParserType<ArrayLiteralParser, ArrayLiteralExpression>, IPrefixParser<ArrayLiteralExpression>
{
    public override ArrayLiteralExpression Parse() {
        var start = EnsureAndConsume(TokenType.LBracket, "Expected opening bracket");

        var array = new ArrayLiteralExpression() {
            StartToken = start,
        };

        while (!Token.Is(TokenType.RBracket | TokenType.EOF)) {
            var expr = Parser.ParseExpression();
            if (expr == null) {
                Expected("Expected expression in array literal");
                return null;
            }

            array.Elements.Add(expr);

            if (Token.IsComma) {
                Advance();
            } else if (!Token.IsRBracket) {
                Expected("Expected comma or closing bracket");
                return null;
            }
        }

        var end = EnsureAndConsume(TokenType.RBracket, "Expected closing bracket");

        array.EndToken = end;

        return array;
    }

}
