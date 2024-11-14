using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.Lexing;
using CSScriptingLang.IncrementalParsing.Syntax;

namespace CSScriptingLang.IncrementalParsing.Grammar;

public class FunctionParser
{
    public static (bool isRegularFunction, bool hasTypeParameters) IsFnCallLike(
        IncrementalParser p,
        bool              skipIdentifier = false
    ) {
        var regularCallCheck = !skipIdentifier ? p.Token.IsIdentifier && p.Next.IsLParen : p.Token.IsLParen;
        if (regularCallCheck) {
            return (true, false);
        }

        var callWithTypeParamsCheck = !skipIdentifier ? p.Token.IsIdentifier && p.Next.IsLAngle : p.Token.IsLAngle;
        if (callWithTypeParamsCheck) {
            if (
                p.LookAheadSequentialLexer(
                    [t => t.IsRAngle, t => t.IsLParen],
                    [t => t.IsLParen || t.IsLBrace]
                )
               .RollbackOnEnd()
               .SetStartPosition(
                    () => {
                        if (skipIdentifier)
                            p.Lexer.Advance();
                        // if (!p.Token.IsLAngle)
                            // throw new Exception("Expected '<' after '->'");
                    }
                )
               .Execute()
            ) return (true, true);
            /*if (
                p.LookAheadSequential(
                    [() => p.Token.IsRAngle, () => p.Token.IsLParen],
                    [() => p.Token.IsLParen || p.Token.IsLBrace]
                )
               .SetStartPosition(
                    () => {
                        if (!skipIdentifier)
                            p.Advance();
                        if (!p.Token.IsLAngle)
                            throw new Exception("Expected '<' after '->'");
                    }
                )
               .Execute((l, m) => l.Rollback())
            ) {
                return (true, true);
            }*/
        }

        return (false, false);
    }

    public static bool IsLambdaDeclLike(IncrementalParser p) => p.Token.IsFunctionKeyword && p.Next.IsLParen;

    public static CompleteMarker FunctionCallWithMarker(
        IncrementalParser p,
        ref Marker        m,
        bool              skipIdentifier = false
    ) {
        var (isRegularFunction, hasTypeParameters) = IsFnCallLike(p, skipIdentifier: skipIdentifier);
        if (!isRegularFunction) {
            return m.Fail(SyntaxKind.CallExpression, "Expected function call");
        }

        if (!skipIdentifier) {
            ExpressionParser.Identifier(p);
        }

        if (hasTypeParameters) {
            ExpressionParser.TypeParameters(p);
        }

        ExpressionParser.CallArgumentList(p);

        p.AdvanceIfSemicolon();

        return m.Complete(SyntaxKind.CallExpression);
    }
    public static CompleteMarker FunctionCall(IncrementalParser p, bool skipIdentifier = false) {
        var m = p.Marker();
        return FunctionCallWithMarker(p, ref m, skipIdentifier: skipIdentifier);
    }

    public static CompleteMarker InlineFunctionDeclaration(IncrementalParser p) {
        var m = p.Marker();

        if (m.EnsureAndConsume(Keyword.Function, "Expected 'function' keyword", out var cm))
            return cm;

        ExpressionParser.ArgumentListDeclaration(p);

        if (p.Token is {IsIdentifier: true, IsLBrace: false}) {
            ExpressionParser.TypeIdentifier(p);
        }

        BlockParser.Block(p);

        return m.Complete(SyntaxKind.InlineFunctionDeclaration);
    }
    public static CompleteMarker LambdaFunction(IncrementalParser p) {
        var m = p.Marker();

        ExpressionParser.ArgumentListDeclaration(p);

        if (p.Token is {IsIdentifier: true, IsLBrace: false}) {
            ExpressionParser.TypeIdentifier(p);
        }

        if (m.EnsureAndConsume(TokenType.Arrow, "Expected '=>' after lambda function parameters", out var cm))
            return cm;

        // At this point we could have:
        //   - `() => { .... }` -> Parse as block
        //   - `() => expr` -> Parse as expression(but wrap as a block + return statement)

        // BlockParser.PotentiallyInlineBlock(p);
        
        if (p.Token.IsLBrace) {
            BlockParser.Block(p);
        } else {
            var expCm = ExpressionParser.Expression(p);
            expCm = expCm.Wrap(SyntaxKind.ReturnStatement);
            expCm = expCm.Wrap(SyntaxKind.Block);
        }

        p.AdvanceIfSemicolon();

        return m.Complete(SyntaxKind.InlineFunctionDeclaration);
    }

    public static CompleteMarker FunctionDefDeclaration(IncrementalParser p) {
        var m = p.Marker();
        if (m.EnsureAndConsume(Keyword.Def, "Expected 'def' keyword", out var cm))
            return cm;
        if (p.Token.IsFunctionKeyword)
            p.Advance();
        if (m.EnsureAndConsume(TokenType.Identifier, "Expected function name", out cm))
            return cm;

        ExpressionParser.CallArgumentList(p);

        return m.Complete(SyntaxKind.DefDeclaration_Function);
    }


    public static List<Keywords.FunctionModifier> FunctionModifiers(
        IncrementalParser p,
        bool              consumeTokens    = true,
        bool              includeFnKeyword = false
    ) {
        var applyModifiers = new List<Keywords.FunctionModifier>();
        var startPos       = p.Cursor;

        while (
            Keywords.HasFunctionModifiers(
                p.Token,
                out var applyFn,
                includeFnKeyword
            )
        ) {
            applyModifiers.Add(applyFn);

            if (consumeTokens) {
                p.Advance();
                continue;
            }

            // Note; we only advance lexer so we don't affect markers
            p.Lexer.Advance();
            p.Lexer.AdvanceTrivia();
        }

        if (!consumeTokens) {
            p.Lexer.Rollback(startPos);
        }

        return applyModifiers;
    }

    public static CompleteMarker FunctionDeclarationWithKeywords(
        IncrementalParser               p,
        bool                            requiresFunctionKeyword = true,
        bool                            requiresBody            = true,
        List<Keywords.FunctionModifier> applyModifiers          = null
    ) => FunctionDeclarationWithKeywords(
        p, out _,
        requiresFunctionKeyword,
        requiresBody,
        applyModifiers
    );

    public static CompleteMarker FunctionDeclarationWithKeywords(
        IncrementalParser               p,
        out string                      functionName,
        bool                            requiresFunctionKeyword = true,
        bool                            requiresBody            = true,
        List<Keywords.FunctionModifier> applyModifiers          = null
    ) {
        var m = p.Marker();

        applyModifiers ??= FunctionModifiers(p);
        requiresBody   =   !applyModifiers.Any(x => x.Token.IsDefKeyword) && requiresBody;

        var fnCompleteMarker = FunctionDeclaration(
            p, ref m,
            out functionName,
            requiresFunctionKeyword: requiresFunctionKeyword,
            requiresBody: requiresBody
        );

        // foreach (var apply in applyModifiers) {
        // apply.Apply(fn);
        // }

        return fnCompleteMarker;
    }

    private static CompleteMarker FunctionDeclaration(
        IncrementalParser p,
        ref Marker        m,
        out string        functionName,
        bool              requiresFunctionKeyword = true,
        bool              requiresBody            = true
    ) {
        functionName = null;

        if (requiresFunctionKeyword) {
            if (m.EnsureAndConsume(Keyword.Function, "Expected 'function' keyword", out var cm))
                return cm;
        }

        ExpressionParser.Identifier(p, out functionName, "Expected function name");
        ExpressionParser.ArgumentListDeclaration(p);

        if (p.Token is {IsIdentifier: true, IsLBrace: false}) {
            ExpressionParser.TypeIdentifier(p);
        }

        if (requiresBody) {
            BlockParser.Block(p);
        }

        return m.Complete(SyntaxKind.FunctionDeclaration);
    }


}