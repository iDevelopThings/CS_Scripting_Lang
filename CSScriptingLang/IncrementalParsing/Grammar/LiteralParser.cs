using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.Lexing;
using CSScriptingLang.IncrementalParsing.Syntax;

namespace CSScriptingLang.IncrementalParsing.Grammar;

public class LiteralParser
{
    public static bool Literal(IncrementalParser p, out CompleteMarker marker) {
        if (p.Token.IsLBracket) {
            marker = ArrayLiteral(p);
            return true;
        }

        if (p.Token.IsString) {
            marker = String(p);
            return true;
        }
        if (p.Token.IsBoolean) {
            marker = Boolean(p);
            return true;
        }
        if (p.Token.IsNumber) {
            marker = Number(p);
            return true;
        }
        if (p.Token.IsNull) {
            marker = Null(p);
            return true;
        }

        marker = CompleteMarker.Empty;
        return false;
    }

    public static CompleteMarker String(IncrementalParser p) {
        var m = p.Marker();
        return m.EnsureAndComplete(TokenType.String, SyntaxKind.StringExpression, "Expected string literal");
    }

    public static CompleteMarker Boolean(IncrementalParser p) {
        var m = p.Marker();
        return m.EnsureAndComplete(TokenType.Boolean, SyntaxKind.BooleanExpression, "Expected boolean literal");
    }

    public static CompleteMarker Number(IncrementalParser p) {
        var m = p.Marker();

        return p.Token.Type switch {
            TokenType.Int32  => m.EnsureAndComplete(TokenType.Int32, SyntaxKind.Int32Expression, "Expected int32 literal"),
            TokenType.Int64  => m.EnsureAndComplete(TokenType.Int64, SyntaxKind.Int64Expression, "Expected int64 literal"),
            TokenType.Float  => m.EnsureAndComplete(TokenType.Float, SyntaxKind.FloatExpression, "Expected float literal"),
            TokenType.Double => m.EnsureAndComplete(TokenType.Double, SyntaxKind.DoubleExpression, "Expected double literal"),

            _ => m.Fail(SyntaxKind.Failed, "Expected number literal"),
        };
    }

    public static CompleteMarker Null(IncrementalParser p) {
        var m = p.Marker();
        return m.EnsureAndComplete(TokenType.Null, SyntaxKind.NullValueExpression, "Expected null literal");
    }

    public static CompleteMarker ArrayLiteral(IncrementalParser p) {
        var m = p.Marker();

        p.EnsureAndConsume(TokenType.LBracket, "Expected opening bracket");

        while (!p.Token.Is(TokenType.RBracket | TokenType.EOF)) {
            var exprMarker = ExpressionParser.Expression(p);
            if (!exprMarker.IsComplete) {
                return m.Fail(SyntaxKind.ArrayLiteralExpression, "Expected expression in array literal");
            }

            if (p.Token.IsComma) {
                p.Advance();
            } else if (!p.Token.IsRBracket) {
                return m.Fail(SyntaxKind.ArrayLiteralExpression, "Expected comma or closing bracket");
            }
        }

        p.EnsureAndConsume(TokenType.RBracket, "Expected closing bracket");

        return m.Complete(SyntaxKind.ArrayLiteralExpression);
    }

    public static CompleteMarker ObjectLiteral(IncrementalParser p) {
        var m = p.Marker();
        if (m.EnsureAndConsume(TokenType.LBrace, "Expected opening brace", out var cm)) {
            return cm;
        }

        while (!p.Token.Is(TokenType.RBrace | TokenType.EOF)) {
            var m1 = p.Marker();

            if (m1.EnsureAnyOfAndConsume(
                    SyntaxKind.ObjectProperty,
                    TokenType.Identifier | TokenType.String | TokenType.Int32 | TokenType.Int64,
                    "Expected identifier as key",
                    out var cm1
                )) return cm1;

            if (m1.EnsureAndConsume(TokenType.Colon, "Expected colon after key", out cm1))
                return cm1;
            
            ExpressionParser.Expression(p);

            m1.Complete(SyntaxKind.ObjectProperty);
            
            if (p.Token.IsComma) {
                p.Advance();
            } else if (!p.Token.IsRBrace) {
                return m.Fail(SyntaxKind.ObjectLiteralExpression, "Expected comma or closing brace");
            }
        }

        if (m.EnsureAndConsume(TokenType.RBrace, "Expected closing brace", out cm))
            return cm;

        return m.Complete(SyntaxKind.ObjectLiteralExpression);
    }
}