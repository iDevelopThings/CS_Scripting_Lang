using CSScriptingLang.Lexing;
using CSScriptingLang.IncrementalParsing.Syntax;

namespace CSScriptingLang.IncrementalParsing.Grammar;

public class BlockParser
{
    public static CompleteMarker Block(IncrementalParser p, bool topLevel = false) {
        var m = p.Marker();

        if (!topLevel)
            p.EnsureAndConsume(TokenType.LBrace, "Expected opening brace");


        bool CanContinue() {
            if (p.Type is TokenType.EOF)
                return false;

            if (!topLevel && p.Type is TokenType.RBrace)
                return false;

            return true;
        }


        while (CanContinue()) {
            StatementParser.Statement(p);
        }

        if (!topLevel)
            p.EnsureAndConsume(TokenType.RBrace, "Expected closing brace");

        return m.Complete(SyntaxKind.Block);
    }

    public static CompleteMarker PotentiallyInlineBlock(IncrementalParser p) {
        if (p.Token.IsLBrace) {
            return Block(p);
        }

        if (p.Token.IsArrow) {
            var m = p.Marker();
            if(m.EnsureAndConsume(TokenType.Arrow, "Expected '=>'", out var cm))
                return cm;
            
            var expCm = ExpressionParser.Expression(p);
            expCm = expCm.Wrap(SyntaxKind.ReturnStatement);
            // expCm = expCm.Wrap(SyntaxKind.Block);

            return m.Complete(SyntaxKind.Block);
        }

        return p.Marker().Fail(SyntaxKind.Block, "Expected '{' or '=>'");
    }

}