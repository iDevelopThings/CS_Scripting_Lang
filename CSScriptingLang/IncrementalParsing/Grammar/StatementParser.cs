using System.Diagnostics;
using CSScriptingLang.Lexing;
using CSScriptingLang.IncrementalParsing.Syntax;

namespace CSScriptingLang.IncrementalParsing.Grammar;

public class StatementParser
{
    public static void Statements(IncrementalParser p) {
        var idx = p.Idx;

        var sw = Stopwatch.StartNew();

        while (p.Type is not TokenType.EOF) {
            Statement(p);

            if (sw.ElapsedMilliseconds > 3000) {
                break;
            }
        }

        if (p.Idx != idx)
            return;

        var m     = p.Marker();
        var token = p.Token;
        p.Advance();
        m.Fail(SyntaxKind.UnknownStatement, $"unexpected symbol {token}");
    }

    public static CompleteMarker Statement(IncrementalParser p) {
        var cm = TryParseStatement(p);
        p.AdvanceIfSemicolon();
        return cm;
    }
    private static CompleteMarker TryParseStatement(IncrementalParser p) {
        if (p.Next.IsDefKeyword && p.NextNext.IsIdentifier)
            return FunctionParser.FunctionDefDeclaration(p);
        if (p.Token.IsVarKeyword)
            return VariableDeclaration(p);
        if (p.Token.IsForKeyword)
            return ForLoop(p);
        if (p.Token.IsIfKeyword)
            return IfStatement(p);
        if (p.Token.IsReturnKeyword)
            return ReturnStatement(p);
        if (p.Token.IsBreakKeyword)
            return BreakStatement(p);
        if (p.Token.IsContinueKeyword)
            return ContinueStatement(p);
        if (p.Token.IsLBracket && p.Next.IsIdentifier)
            return Attribute(p);
        if (p.Token.IsDeferKeyword)
            return DeferStatement(p);
        if (p.Token.IsSignalKeyword && p.Next.IsIdentifier && p.NextNext.IsLParen)
            return SignalDeclaration(p);
        if (p.Token.IsMatchKeyword)
            return MatchExpressionParser.Match(p);
        if (p.Token.IsTypeKeyword && p.Next.IsIdentifier && p.NextNext.IsTypeDeclarationKeyword)
            return TypeDeclarationParser.Parse(p);
        if (p.Token.IsFunctionKeyword && p.Next.IsLParen)
            return FunctionParser.FunctionDeclarationWithKeywords(p);
        if (p.Token.IsIdentifier && p.Next.IsLParen)
            return FunctionParser.FunctionCall(p);
        if (p.Token.IsLBrace && !(p.Next.IsIdentifier && p.NextNext.IsColon))
            return BlockParser.Block(p);

        if (Keywords.IsFunctionWithModifiers(p.Token)) {
            return FunctionParser.FunctionDeclarationWithKeywords(p);
        }

        if (p.Token.IsVarKeyword)
            return VariableDeclaration(p);
        if (p.Token.IsYieldKeyword)
            return YieldStatement(p);
        if (p.Token.IsAwaitKeyword)
            return AwaitStatement(p);

        return ExpressionParser.Expression(p);
    }

    public static CompleteMarker IfStatement(IncrementalParser p) {
        var m = p.Marker();

        // Consume 'if'
        if (m.EnsureAndConsume(Keyword.If, "Expected 'if' keyword", out var cm))
            return cm;
        if (m.EnsureAndConsume(TokenType.LParen, "Expected '(' after 'if'", out cm))
            return cm;

        // Parse the condition expression
        ExpressionParser.Expression(p);

        // Expect a ')' after the condition & consume it
        if (m.EnsureAndConsume(TokenType.RParen, "Expected ')' after condition", out cm))
            return cm;

        // Parse the 'then' branch, which is a block statement
        BlockParser.Block(p);

        cm = m.Complete(SyntaxKind.IfClause);

        while (p.Token.IsElseKeyword || (p.Token.IsElseKeyword && p.Next.IsIfKeyword)) {
            IfBranch(p);
        }

        return cm.Wrap(SyntaxKind.IfStatement);
        // return m.Complete(SyntaxKind.IfStatement);
    }

    public static CompleteMarker IfBranch(IncrementalParser p) {
        var m = p.Marker();
        if (p.Token.IsElseKeyword && p.Next.IsIfKeyword) {
            p.Advance(); // Consume 'else'
            p.Advance(); // Consume 'if'

            if (m.EnsureAndConsume(TokenType.LParen, "Expected '(' after 'else if'", out var cm))
                return cm;

            ExpressionParser.Expression(p);

            if (m.EnsureAndConsume(TokenType.RParen, "Expected ')' after condition", out cm))
                return cm;

            BlockParser.Block(p);

            return m.Complete(SyntaxKind.IfClause);
        }

        if (p.Token.IsElseKeyword) {
            p.Advance(); // Consume 'else'
            BlockParser.Block(p);
            return m.Complete(SyntaxKind.IfClause);
        }

        return m.Fail(SyntaxKind.UnknownStatement, "Expected 'else' or 'else if' clause");
    }

    public static CompleteMarker VariableDeclaration(IncrementalParser p) {
        var m = p.Marker();
        if (m.EnsureAndConsume(Keyword.Var, "Expected 'var' keyword", out var cm))
            return cm;

        // We can have:
        //  - var x = 10;
        //  - var (x, y) = (10, 20);
        // 
        // In `for` loop parser state, we can use:
        //  - var a = range expr;
        //  - var (a, b) = range expr;

        if (p.Token.IsTupleLike()) {
            ExpressionParser.TupleExpression(p, true);
        } else {
            ExpressionParser.Identifier(p, "Expected identifier for variable name");
        }

        if (!p.Token.IsAssignmentOp())
            return m.Fail(SyntaxKind.VariableDeclaration, "Expected assignment operator");

        p.Advance();

        if (p.Token.IsTupleLike()) {
            ExpressionParser.TupleExpression(p, false);
        } else {
            ExpressionParser.Expression(p);
        }

        // p.AdvanceIfSemicolon();

        return m.Complete(SyntaxKind.VariableDeclaration);
    }

    public static CompleteMarker ForLoop(IncrementalParser p) {
        using var _ = p.UsingState(ParseState.ForLoopCondition);

        var m = p.Marker();
        if (m.EnsureAndConsume(Keyword.For, "Expected 'for' keyword", out var cm))
            return cm;

        // `for { ... }` is a `while` loop
        if (p.Token.IsLBrace) {
            BlockParser.Block(p);
            return m.Complete(SyntaxKind.ForWhileLoop);
        }

        if (m.EnsureAndConsume(TokenType.LParen, "Expected '(' after 'for'", out cm))
            return cm;

        // ---------------------------------------------
        // Handles:
        // `for (var i = 0; i < 10; i++) { ... }`
        //       ~~~~~~~~~
        // 
        // Possibilities:
        //  - for (var i = 0; ...)
        //  - for (i = 0; ...)
        //  - for (var (a, b) = range ...)
        //  - for (var a = range ...)
        // ---------------------------------------------

        var isForRange = false;

        // We can also skip the var if it's defined outside the loop init
        // var i = 0; for(; i < 10; i++) { }
        if (p.Token.IsSemicolon) {
            p.Advance();
        } else {
            var varCm = VariableDeclaration(p);
            if (!varCm.IsComplete)
                return varCm;

            isForRange = varCm.Contains(TokenType.KeywordIdentifier, (et, t) => t.IsRangeKeyword);

            p.AdvanceIfSemicolon();
        }

        if (!isForRange) {
            // ---------------------------------------------
            // Handles:
            // `for (var i = 0; i < 10; i++) { ... }`
            //                  ~~~~~~
            // ---------------------------------------------

            // Parse the condition expression
            ExpressionParser.Expression(p);
            if (m.EnsureAndConsume(TokenType.Semicolon, "Expected ';' after condition", out cm))
                return cm;

            // ---------------------------------------------
            // Handles:
            // `for (var i = 0; i < 10; i++) { ... }`
            //                          ~~~
            // ---------------------------------------------

            // Parse the increment iterator expr
            ExpressionParser.Expression(p);
        }

        if (m.EnsureAndConsume(TokenType.RParen, "Expected ')' after iterator expr", out cm))
            return cm;

        BlockParser.Block(p);

        return m.Complete(
            isForRange
                ? SyntaxKind.ForRange
                : SyntaxKind.ForIndexedLoop
        );
    }

    public static CompleteMarker TermStatement(
        IncrementalParser p,
        ref Marker        m,
        SyntaxKind        syntaxKind,
        Func<bool>        action      = null, // return true to complete the statement
        string            failMessage = "Failed to parse statement"
    ) {
        if (p.Token.IsSemicolon) {
            p.Advance();
            return m.Complete(syntaxKind);
        }
        if (p.Token.IsEOF || p.Token.IsRBrace || p.Token.IsRBracket || p.Token.IsRParen) {
            return m.Complete(syntaxKind);
        }

        if (action != null && action()) {
            p.AdvanceIfSemicolon();
            return m.Complete(syntaxKind);
        }

        return m.Fail(syntaxKind, failMessage);
    }

    public static CompleteMarker ReturnStatement(IncrementalParser p) {
        var m = p.Marker();
        if (m.EnsureAndConsume(Keyword.Return, "Expected 'return' keyword", out var cm))
            return cm;

        // We need to detect if we have some expression or just a regular empty `return` statement, making some simple assumptions
        return TermStatement(
            p, ref m, SyntaxKind.ReturnStatement,
            () => {
                ExpressionParser.Expression(p);
                return true;
            }
        );
    }
    public static CompleteMarker BreakStatement(IncrementalParser p) {
        var m = p.Marker();

        if (m.EnsureAndConsume(Keyword.Break, "Expected 'break' keyword", out var cm))
            return cm;

        return TermStatement(
            p, ref m, SyntaxKind.BreakStatement,
            () => {
                ExpressionParser.Expression(p);
                return true;
            }
        );
    }
    public static CompleteMarker ContinueStatement(IncrementalParser p) {
        var m = p.Marker();
        if (m.EnsureAndConsume(Keyword.Continue, "Expected 'continue' keyword", out var cm))
            return cm;
        return m.Complete(SyntaxKind.ContinueStatement);
    }

    public static CompleteMarker Attribute(IncrementalParser p) {
        var m = p.Marker();
        if (m.EnsureAndConsume(TokenType.LBracket, "Expected '['", out var cm))
            return cm;

        ExpressionParser.Identifier(p, "Expected identifier for attribute name");

        if (p.Token.IsLParen) {
            ExpressionParser.CallArgumentList(p);
        }

        if (m.EnsureAndConsume(TokenType.RBracket, "Expected ']'", out cm))
            return cm;

        return m.Complete(SyntaxKind.AttributeDeclaration);
    }
    public static void Attributes(IncrementalParser p) {
        while (p.Token.IsLBracket && p.Next.IsIdentifier) {
            Attribute(p);
        }
    }

    public static CompleteMarker DeferStatement(IncrementalParser p) {
        var m = p.Marker();
        if (m.EnsureAndConsume(Keyword.Defer, "Expected 'defer' keyword", out var cm))
            return cm;

        cm = m.Complete(SyntaxKind.DeferStatement);

        if (FunctionParser.IsLambdaDeclLike(p)) {
            m = cm.Precede();
            FunctionParser.InlineFunctionDeclaration(p);
            ExpressionParser.CallArgumentList(p);
            return cm = m.Complete(SyntaxKind.CallExpression);
        }

        m = cm.Precede();
        FunctionParser.FunctionCall(p);
        return cm = m.Complete(SyntaxKind.CallExpression);
    }
    public static CompleteMarker SignalDeclaration(IncrementalParser p) {
        var m = p.Marker();
        if (m.EnsureAndConsume(Keyword.Signal, "Expected 'signal' keyword", out var cm))
            return cm;

        ExpressionParser.Identifier(p, "Expected identifier for signal name");

        ExpressionParser.ArgumentListDeclaration(p);

        p.AdvanceIfSemicolon();

        return m.Complete(SyntaxKind.SignalDeclaration);
    }

    public static CompleteMarker YieldStatement(IncrementalParser p) {
        var m = p.Marker();
        if (m.EnsureAndConsume(Keyword.Yield, "Expected 'yield' keyword", out var cm))
            return cm;

        return TermStatement(
            p, ref m, SyntaxKind.YieldStatement,
            () => {
                ExpressionParser.Expression(p);
                return true;
            }
        );
    }
    public static CompleteMarker AwaitStatement(IncrementalParser p) {
        var m = p.Marker();
        if (m.EnsureAndConsume(Keyword.Await, "Expected 'await' keyword", out var cm))
            return cm;

        ExpressionParser.Expression(p);
        p.AdvanceIfSemicolon();

        return m.Complete(SyntaxKind.AwaitStatement);
    }

}