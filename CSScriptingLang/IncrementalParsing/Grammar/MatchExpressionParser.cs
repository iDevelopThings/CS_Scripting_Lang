using CSScriptingLang.Lexing;
using CSScriptingLang.IncrementalParsing.Syntax;
using CSScriptingLang.Parsing;

namespace CSScriptingLang.IncrementalParsing.Grammar;

public class MatchExpressionParser
{
    public static CompleteMarker Match(IncrementalParser p) {
        var m = p.Marker();
        
        if(m.EnsureAndConsume(Keyword.Match, "Expected 'match' keyword", out var cm))
            return cm;
        
        if(m.EnsureAndConsume(TokenType.LParen, "Expected '(' after 'match'", out cm))
            return cm;
        
        ExpressionParser.Expression(p);
        
        if(m.EnsureAndConsume(TokenType.RParen, "Expected ')' after match expression", out cm))
            return cm;
        if(m.EnsureAndConsume(TokenType.LBrace, "Expected '{' after match expression", out cm))
            return cm;

        while (p.Token is {IsRBrace: false, IsEOF: false}) {
            var matchCaseMarker = p.Marker();
            
            if(matchCaseMarker.EnsureAndConsume(Keyword.Case, "Expected 'case' keyword", out cm))
                return cm;

            PatternMatch(p);

            if (!BlockParser.PotentiallyInlineBlock(p).IsComplete) {
                return matchCaseMarker.Fail(SyntaxKind.MatchExpression, "Expected '=>' or '{' after case pattern");
            }
            
            /*if (p.Token.IsArrow) {
                if(matchCaseMarker.EnsureAndConsume(TokenType.Arrow, "Expected '=>' after case pattern", out cm))
                    return cm;
                ExpressionParser.Expression(p);
            } else if (p.Token.IsLBrace) {
                BlockParser.Block(p);
            } else {
                return matchCaseMarker.Fail(SyntaxKind.MatchExpression, "Expected '=>' or '{' after case pattern");
            }*/
            
            matchCaseMarker.Complete(SyntaxKind.MatchCase);
        }

        if(m.EnsureAndConsume(TokenType.RBrace, "Expected '}' after match expression", out cm))
            return cm;
        
        return m.Complete(SyntaxKind.MatchExpression);
    }
    
    public static CompleteMarker PatternMatch(IncrementalParser p) {
        using var _ = p.UsingState(ParseState.PatternMatch);
        
        // `case is TypeName`
        if (p.Token.IsIsKeyword && p.Next.IsIdentifier) {
            var m = p.Marker();
            if(m.EnsureAndConsume(Keyword.Is, "Expected 'is' keyword", out var cm))
                return cm;
            ExpressionParser.TypeIdentifier(p, "Expected type name");
            return m.Complete(SyntaxKind.MatchPattern_IsType);
        }
        
        if (p.Token.IsNumber || p.Token.IsString || p.Token.IsBoolean) {
            var m = p.Marker();
            
            switch (p.Token) {
                case {IsNumber: true}:
                    LiteralParser.Number(p);
                    break;
                case {IsString: true}:
                    LiteralParser.String(p);
                    break;
                case {IsBoolean: true}:
                    LiteralParser.Boolean(p);
                    break;
                default:
                    return m.Fail(SyntaxKind.MatchPattern_Literal, "Expected literal value");
            }
            
            return m.Complete(SyntaxKind.MatchPattern_Literal);
        }

        if (p.Token.IsIdentifier && !p.Token.Value.Equals("default")) {
            var m = p.Marker();
            ExpressionParser.Identifier(p);
            return m.Complete(SyntaxKind.MatchPattern_Identifier);
        }

        if (p.Token.IsUnderscore) {
            var m = p.Marker();
            if(m.EnsureAndConsume(TokenType.Underscore, "Expected '_'", out var cm))
                return cm;
            return m.Complete(SyntaxKind.MatchPattern_Default);
        }
        if (p.Token.IsIdentifier && p.Token.Value.Equals("default")) {
            var m = p.Marker();
            if(m.EnsureAndConsume(TokenType.Identifier, "Expected 'default' keyword", out var cm))
                return cm;
            return m.Complete(SyntaxKind.MatchPattern_Default);
        }

        return p.Marker().Fail(SyntaxKind.MatchCase, "Expected pattern");
    }
}