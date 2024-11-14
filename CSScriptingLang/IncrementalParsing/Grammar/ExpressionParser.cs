using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.Lexing;
using CSScriptingLang.IncrementalParsing.Syntax;

namespace CSScriptingLang.IncrementalParsing.Grammar;

public class ExpressionParser
{
    public static CompleteMarker Expression(IncrementalParser p) {
        return Assignment(p);
    }

    public static CompleteMarker Assignment(IncrementalParser p) {
        var cm = Or(p);

        // =, +=, -=, *=, /=, %=
        if (p.Token.IsAssignmentOp() || p.Token.IsBitwiseShiftOp()) {
            var m = cm.Precede();
            p.Advance();
            Assignment(p);
            cm = m.Complete(SyntaxKind.BinaryOpExpression);
        }

        return cm;
    }

    public static CompleteMarker Or(IncrementalParser p) {
        var cm = And(p);

        // bool ||, bitwise |
        while (p.Token.IsOrOp()) {
            var m = cm.Precede();
            p.Advance();
            And(p);
            cm = m.Complete(SyntaxKind.BinaryOpExpression);
        }

        return cm;
    }
    public static CompleteMarker And(IncrementalParser p) {
        var cm = Equality(p);

        // bool &&, bitwise &
        while (p.Token.IsAndOp()) {
            var m = cm.Precede();
            p.Advance();
            Equality(p);
            cm = m.Complete(SyntaxKind.BinaryOpExpression);
        }

        return cm;
    }
    public static CompleteMarker Equality(IncrementalParser p) {
        var cm = Comparison(p);

        // ==, !=, ===, !==
        while (p.Token.IsEqualityOp()) {
            var m = cm.Precede();
            p.Advance();
            Comparison(p);
            cm = m.Complete(SyntaxKind.BinaryOpExpression);
        }

        return cm;
    }

    public static CompleteMarker Comparison(IncrementalParser p) {
        var cm = Term(p);

        // <, >, <=, >=
        while (p.Token.IsComparisonOp()) {
            var m = cm.Precede();
            p.Advance();
            Term(p);
            cm = m.Complete(SyntaxKind.BinaryOpExpression);
        }

        return cm;
    }

    public static CompleteMarker Term(IncrementalParser p) {
        var cm = Factor(p);

        // +, -
        while (p.Token.IsTermOp()) {
            var m = cm.Precede();
            p.Advance();
            Factor(p);
            cm = m.Complete(SyntaxKind.BinaryOpExpression);
        }

        return cm;
    }

    public static CompleteMarker Factor(IncrementalParser p) {
        var cm = Unary(p);

        // *, /, %, bitwise ^
        while (p.Token.IsFactorOp()) {
            var m = cm.Precede();
            p.Advance();
            Unary(p);
            cm = m.Complete(SyntaxKind.BinaryOpExpression);
        }

        return cm;
    }

    public static CompleteMarker Unary(IncrementalParser p) {
        var cm = Prefix(p);

        if (cm.IsEmptyMarker) {
            cm = Primary(p);
        }

        return Postfix(p, cm);

        // var primary = Primary(p);
        // Postfix increment/decrement
        // if (p.Token.IsOp(OperatorType.Increment, OperatorType.Decrement)) {
        //     var m = primary.Precede();
        //     p.Advance();
        //     return m.Complete(SyntaxKind.UnaryOpExpression);
        // }
        // return primary;
    }

    public static CompleteMarker Prefix(IncrementalParser p) {

        if (p.IsInForLoopConditionState && p.Token.IsRangeKeyword) {
            var m = p.Marker();
            p.Advance();
            Unary(p);
            return m.Complete(SyntaxKind.RangeExpression);
        }
        
        if (p.Token.IsAwaitKeyword) {
            return StatementParser.AwaitStatement(p);
        }

        // !, -, bitwise ~
        if (p.Token.IsUnaryOp()) {
            var m = p.Marker();
            p.Advance();
            Primary(p);
            return m.Complete(SyntaxKind.PrefixUnaryOpExpression);
        }

        return CompleteMarker.Empty;
    }

    public static CompleteMarker Postfix(IncrementalParser p, CompleteMarker cm) {
        // Postfix increment/decrement
        if (p.Token.IsOp(OperatorType.Increment, OperatorType.Decrement)) {
            var m = cm.Precede();
            p.Advance();
            return m.Complete(SyntaxKind.PostfixUnaryOpExpression);
        }

        return cm;
    }

    /*public static CompleteMarker ExpressionList(IncrementalParser p) {
        var m = p.Marker();

        if (m.EnsureAndConsume(TokenType.LParen, "Expected opening parenthesis", out var cm))
            return cm;

        while (!p.Token.IsRParen && !p.Token.IsEOF) {
            Expression(p);

            if (p.Token.IsComma)
                p.Advance();
            else if (!p.Token.IsRParen)
                return m.Fail(SyntaxKind.ExpressionList, "Expected comma or closing parenthesis");
        }

        if (m.EnsureAndConsume(TokenType.RParen, "Expected closing parenthesis", out cm))
            return cm;

        return m.Complete(SyntaxKind.ExpressionList);
    }*/

    public static CompleteMarker TupleExpression(IncrementalParser p, bool onlyIdentifiers) {
        var m = p.Marker();

        if (m.EnsureAndConsume(TokenType.LParen, "Expected opening parenthesis", out var cm))
            return cm;

        var isRangeTuple = p.IsInForLoopConditionState && p.Token.IsRangeKeyword;
        if (isRangeTuple)
            p.Advance();

        while (!p.Token.IsRParen && !p.Token.IsEOF) {
            if (onlyIdentifiers) {
                Identifier(p);
            } else {
                Expression(p);
            }

            if (p.Token.IsComma)
                p.Advance();
            else if (!p.Token.IsRParen)
                return m.Fail(SyntaxKind.TupleExpression, "Expected comma or closing parenthesis");
        }

        if (m.EnsureAndConsume(TokenType.RParen, "Expected closing parenthesis", out cm))
            return cm;

        cm = m.Complete(SyntaxKind.TupleExpression);

        if (isRangeTuple) {
            return cm.Wrap(SyntaxKind.RangeExpression);
        }

        return cm;
    }

    public static CompleteMarker Primary(IncrementalParser p) {
        // var m = p.Marker();

        if (LiteralParser.Literal(p, out var cm)) {
            // m = cm.Precede()
            return cm;
        }

        if (FunctionParser.IsLambdaDeclLike(p))
            return FunctionParser.InlineFunctionDeclaration(p);

        if (p.Token.IsMatchKeyword && p.Next.IsLParen)
            return MatchExpressionParser.Match(p);

        var (isRegularFunction, _) = FunctionParser.IsFnCallLike(p);
        if (isRegularFunction) {
            return FunctionParser.FunctionCall(p);
        }

        // `SomeVar` or `&SomeVar`
        if (p.Token.IsIdentifier || p.Token.IsAnd && p.Next.IsIdentifier) {
            var m = p.Marker();

            if (p.Token.IsAnd)
                if (m.EnsureAndConsume(TokenType.And, "Expected '&' before variable name", out cm))
                    return cm;

            var identCm = Identifier(p);
            identCm = MemberAccess(p, identCm);
            if (identCm.IsComplete)
                return identCm;

            return m.Complete(SyntaxKind.IdentifierExpression);
        }

        if (p.Token.IsLParen) {
            // We want to search for lambdas
            //   `(int a, int b) => { return a + b; }`
            // but to do that, we need to know if we have:
            //   `(... skip ...) => {`
            // TODO: Reimplement this
            if (
                p.LookAheadSequentialLexer(
                    [t => t.IsRParen, t => t.IsArrow, /*() => t.IsLBrace*/],
                    [t => t.IsLParen || t.IsRBrace]
                )
               .RollbackOnEnd()
               .Execute()
            ) {
                return FunctionParser.LambdaFunction(p);
            }

            // tuple = var (x, y, z), we're searching for `(x,`
            if (p.Token.IsTupleLike()) {
                return TupleExpression(p, onlyIdentifiers: false);
            }

            // var m = p.Marker();
            // if (m.EnsureAndConsume(TokenType.LParen, "Expected opening parenthesis", out cm))
            // return cm;

            p.EnsureAndConsume(TokenType.LParen, "Expected opening parenthesis");

            var expCm = Expression(p);

            if (p.Token.IsComma) {
                // m.Rollback();
                // return ExpressionList(p);
                // p.Unexpected("Expected closing parenthesis");
                p.Advance();

                while (!p.Token.IsRParen && !p.Token.IsEOF) {
                    expCm = Expression(p);

                    if (p.Token.IsComma)
                        p.Advance();
                    else if (!p.Token.IsRParen)
                        p.Unexpected("Expected comma or closing parenthesis");
                }
            }

            p.EnsureAndConsume(TokenType.RParen, "Expected closing parenthesis");

            // if (m.EnsureAndConsume(TokenType.RParen, "Expected closing parenthesis", out cm))
            // return cm;
            // return m.Complete(SyntaxKind.ParenExpression);

            return expCm;
        }

        if (p.Token.IsLBrace) {
            return LiteralParser.ObjectLiteral(p);
        }

        if (p.Token.IsError) {
            throw new UnexpectedTokenException($"Unexpected; Error: {p.Token.ErrorMessage}");
        }

        throw new UnexpectedTokenException($"Unhandled expression/Unexpected token: {p.Token}, prev,cur,next={p.Token?.Previous?.Value ?? ""} {p.Token?.Value ?? ""} {p.Token?.Next?.Value ?? ""}");
    }

    public static CompleteMarker TypeParameter(IncrementalParser p) {
        var m = p.Marker();
        return m.EnsureAndComplete(TokenType.Identifier, SyntaxKind.TypeParameter, "Expected type parameter");
    }
    public static CompleteMarker TypeParameters(IncrementalParser p) {
        var m = p.Marker();

        if (m.EnsureAndConsume(TokenType.LAngle, "Expected '<' before type parameters", out var cm))
            return cm;

        while (!p.Token.Is(TokenType.RAngle | TokenType.EOF)) {

            if (!TypeParameter(p).IsComplete)
                return m.Fail(SyntaxKind.TypeParametersList, "Expected type parameter");

            if (p.Token.IsComma) {
                p.Advance();
            } else if (!p.Token.IsRAngle) {
                return m.Fail(SyntaxKind.TypeParametersList, "Expected ',' or '>' after type parameter");
            }
        }

        if (m.EnsureAndConsume(TokenType.RAngle, "Expected '>' after type parameters", out cm))
            return cm;

        return m.Complete(SyntaxKind.TypeParametersList);
    }

    // This would be function arguments, ie `(type name, type name, type name)`
    public static CompleteMarker ArgumentListDeclaration(IncrementalParser p) {
        var m = p.Marker();
        if (m.EnsureAndConsume(TokenType.LParen, "Expected '(' before arguments", out var cm))
            return cm;

        while (!p.Token.IsRParen || p.Token.IsEOF) {
            var m1 = p.Marker();

            // Formats are:
            // - `{Type} {Name}`
            // - `... {Type} {Name}`

            if (p.Token.IsDotDotDot) {
                p.Advance(); // Consume '...'
            }

            TypeIdentifier(p, "Expected a type name identifier");
            Identifier(p, "Expected a parameter name");

            // if (p.Token.IsDotDotDot && p.Next.IsIdentifier) {
            // p.Advance(); // Consume '...'
            // Identifier(p, "Expected a parameter name");
            // } else {
            // TypeIdentifier(p, "Expected a type name identifier");
            // Identifier(p, "Expected a parameter name");
            // }

            if (!p.Token.IsComma && !p.Token.IsRParen) {
                m1.Fail(SyntaxKind.ArgumentDeclaration, "Expected comma or closing parenthesis");
                return m.Fail(SyntaxKind.ArgumentListDeclaration, "Expected comma or closing parenthesis");
            }

            m1.Complete(SyntaxKind.ArgumentDeclaration);

            if (p.Token.IsComma)
                p.Advance();
            else if (!p.Token.IsRParen) {
                m1.Fail(SyntaxKind.ArgumentDeclaration, "Expected comma or closing parenthesis");
                return m.Fail(SyntaxKind.ArgumentListDeclaration, "Expected comma or closing parenthesis");
            }

            // if (p.Token.IsComma)
            //     p.Advance();
            // else if (!p.Token.IsRParen)
            //     return m.Fail(SyntaxKind.ArgumentListDeclaration, "Expected comma or closing parenthesis");
        }

        if (m.EnsureAndConsume(TokenType.RParen, "Expected ')' after arguments", out cm))
            return cm;

        return m.Complete(SyntaxKind.ArgumentListDeclaration);
    }

    // This would be call expr arguments, ie `(expr, expr, expr)` `(1, 2, 3 * 4)`
    public static CompleteMarker CallArgumentList(IncrementalParser p) {
        var m = p.Marker();

        if (m.EnsureAndConsume(TokenType.LParen, "Expected '(' before arguments", out var cm))
            return cm;

        while (!p.Token.Is(TokenType.RParen | TokenType.EOF)) {
            if (!Expression(p).IsComplete)
                return m.Fail(SyntaxKind.ArgumentList, "Expected argument");

            if (p.Token.IsComma) {
                p.Advance();
            } else if (!p.Token.IsRParen) {
                return m.Fail(SyntaxKind.ArgumentList, "Expected ',' or ')' after argument");
            }
        }

        if (m.EnsureAndConsume(TokenType.RParen, "Expected ')' after arguments", out cm))
            return cm;

        return m.Complete(SyntaxKind.ArgumentList);
    }

    public static CompleteMarker Identifier(IncrementalParser p, string reason = "Expected identifier") {
        var m = p.Marker();
        return m.EnsureAndComplete(TokenType.Identifier, SyntaxKind.IdentifierExpression, reason);
    }
    public static CompleteMarker Identifier(IncrementalParser p, out string identifierStr, string reason = "Expected identifier") {
        var m = p.Marker();
        if (p.Token.IsIdentifier) {
            identifierStr = p.Token.Value;
            p.Advance();
            return m.Complete(SyntaxKind.IdentifierExpression);
        }
        identifierStr = null;
        return m.Fail(SyntaxKind.IdentifierExpression, reason);
    }
    public static CompleteMarker TypeIdentifier(IncrementalParser p, string reason = "Expected identifier") {
        var m = p.Marker();

        if (m.EnsureAndConsume(TokenType.Identifier, reason, out var cm))
            return cm;


        if (p.Token.IsLAngle) {
            TypeParameters(p);
        }

        return m.Complete(SyntaxKind.TypeIdentifierExpression);
    }

    public static CompleteMarker MemberAccess(IncrementalParser p, CompleteMarker cm) {

        bool IsMemberAccess() => p.Token.IsDot && p.Next.IsIdentifier;
        bool IsIndexAccess()  => p.Token.IsLBracket && (p.Next.IsNumber || p.Next.IsString || p.Next.IsIdentifier);
        bool IsFunctionCall() => FunctionParser.IsFnCallLike(p, skipIdentifier: true).isRegularFunction;

        var m = cm.Precede();

        while ((IsMemberAccess() || IsIndexAccess() || IsFunctionCall()) && !p.Token.IsEOF) {
            // variable.x
            if (IsMemberAccess()) {
                m = cm.Precede();
                if (m.EnsureAndConsume(TokenType.Dot, "Expected '.' after variable name `variable*.*property`", out cm))
                    return cm;

                Identifier(p, "Expected property name after '.' `variable.*property*`");
                p.Events.Add(new MarkEvent.RemapTokenType(TokenType.KeywordIdentifier, TokenType.Identifier));

                cm = m.Complete(SyntaxKind.MemberAccessExpression);
            }

            // variable[x] | variable['x'] | variable[0..9] | variable[expression]
            if (IsIndexAccess()) {
                m = cm.Precede();
                if (m.EnsureAndConsume(TokenType.LBracket, "Expected opening bracket after variable name `variable*[*`", out cm))
                    return cm;

                Expression(p);

                if (m.EnsureAndConsume(TokenType.RBracket, "Expected closing bracket after index expression `variable[index*]*`", out cm))
                    return cm;

                cm = m.Complete(SyntaxKind.IndexAccessExpression);
            }

            // variable.fn() | variable['fn']() | variable[0..9]() | variable[expression]()
            if (IsFunctionCall()) {
                m  = cm.Precede();
                cm = FunctionParser.FunctionCallWithMarker(p, ref m, cm.IsComplete);
                // cm = m.Complete(SyntaxKind.CallExpression);
            }
        }

        return cm;
    }


}



/*
    public static CompleteMarker Unary(IncrementalParser p) {
        var            m = p.Marker();
        CompleteMarker cm;

        if (p.Token.IsOp(OperatorType.Minus, OperatorType.Not)) {
            p.Advance();
            if (!Expression(p).IsComplete) {
                return m.Fail(SyntaxKind.UnaryOpExpression, "Expected expression after unary operator");
            }

            cm = m.Complete(SyntaxKind.UnaryOpExpression);
            m  = cm.Precede(p);
        } else {
            cm = Primary(p);
        }

        return cm;
    }

    public static CompleteMarker Assignment(IncrementalParser p) {
        var m = p.Marker();

        var cm = LogicalOr(p);

        if (p.Token.IsOp(OperatorType.Assignment)) {
            p.Advance();
            Expression(p);
            return m.Complete(SyntaxKind.BinaryOpExpression);
        }

        return cm;
    }

    public static CompleteMarker Term(IncrementalParser p) {
        var m = p.Marker();

        var cm = Factor(p);

        while (p.Token.IsOp(OperatorType.Plus, OperatorType.Minus, OperatorType.PlusEquals, OperatorType.MinusEquals)) {
            p.Advance();
            Factor(p);
            return m.Complete(SyntaxKind.BinaryOpExpression);
        }

        return cm;
    }

    public static CompleteMarker Comparison(IncrementalParser p) {
        var m = p.Marker();

        var cm = Term(p);

        while (p.Token.IsComparisonOp()) {

            var opTok = p.Advance();
            var right = Term(p); // Parse the right side of the comparison
            left = new BinaryOpExpression(left, opTok.Op, right) {
                StartToken = opTok,
                EndToken   = Token,
            };
        }

        return left;
    }

    public static CompleteMarker Factor(IncrementalParser p) {
        var m = p.Marker();

        var node = ParsePrimary();

        while (
            p.Token.IsMultiplyOperator || p.Token.IsDivideOperator || p.Token.IsModulusOperator
        ) {
            var opTok = p.Advance();
            var right = ParsePrimary();
            node = new BinaryOpExpression(node, opTok.Op, right) {
                StartToken = opTok,
                EndToken   = Token,
            };
        }

        return node;
    }

    public static CompleteMarker LogicalOr(IncrementalParser p) {
        var m = p.Marker();

        var left = ParseLogicalAnd(); // Logical AND has higher precedence, parse it first

        while (p.Token.IsOrOperator || p.Token.IsOrKeyword || p.Token.IsPipeOperator) {
            var opTok = p.Advance();
            var right = ParseLogicalAnd();
            left = new BinaryOpExpression(left, opTok.Op, right) {
                StartToken = opTok,
                EndToken   = Token,
            };
        }

        return left;
    }

    public static CompleteMarker LogicalAnd(IncrementalParser p) {
        var m = p.Marker();

        var left = ParseComparison(); // Comparison has higher precedence, parse it first

        while (p.Token.IsAndOperator || p.Token.IsBitwiseAndOperator) {
            var opTok = p.Advance();
            var right = ParseComparison();
            left = new BinaryOpExpression(left, opTok.Op, right) {
                StartToken = opTok,
                EndToken   = Token,
            };
        }

        return left;
    }
    */