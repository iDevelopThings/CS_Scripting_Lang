using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.Lexing;

namespace CSScriptingLang.Parsing.Parsers;

public class BlockExpressionParser : SubParserType<BlockExpressionParser, BlockExpression>
{
    public override BlockExpression Parse() {
        var start = EnsureAndConsume(TokenType.LBrace, "Expected opening brace");

        var node = new BlockExpression {
            StartToken = start,
        };

        using var _ = Parser.BlockScopeStack.Using(node);

        while (Token.Type != TokenType.RBrace && Token.Type != TokenType.EOF) {
            var statement = Parser.ParseStatement();
            node.Add(statement);
        }

        var end = EnsureAndConsume(TokenType.RBrace, "Expected closing brace");

        node.EndToken = end;

        return node;
    }

}