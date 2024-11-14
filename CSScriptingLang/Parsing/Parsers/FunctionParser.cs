using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.Interpreter.Execution.Statements;
using CSScriptingLang.Lexing;

namespace CSScriptingLang.Parsing.Parsers;

public class FunctionParser : SubParserType<FunctionParser, InlineFunctionDeclaration>,
                              IPrefixParser<InlineFunctionDeclaration>,
                              IStatementParser<ExpressionStatement>,
                              IParserMatcher
{
    public override InlineFunctionDeclaration Parse() {
        var isRegularFn = Token.IsFunctionKeyword && Next.IsIdentifier;
        if (isRegularFn) {
            return ParseDeclaration();
        }

        FunctionDeclaration fn;

        var isAsyncFn = Token.IsAsyncKeyword && Next.IsFunctionKeyword;
        if (isAsyncFn) {
            var start = EnsureAndConsume(Keyword.Async, "Expected 'async' keyword");
            fn            = ParseDeclaration();
            fn.IsAsync    = true;
            fn.StartToken = start;
            return fn;
        }

        var isCoroutine = Token.IsCoroutineKeyword && Next.IsFunctionKeyword;
        if (isCoroutine) {
            var start = EnsureAndConsume(Keyword.Coroutine, "Expected 'coroutine' keyword");
            fn             = ParseDeclaration();
            fn.IsCoroutine = true;
            fn.StartToken  = start;
            return fn;
        }

        var isAsyncCor = Token.IsAsyncKeyword && Next.IsCoroutineKeyword && NextNext.IsFunctionKeyword;
        if (isAsyncCor) {
            var start = EnsureAndConsume(Keyword.Async, "Expected 'async' keyword");
            EnsureAndConsume(Keyword.Coroutine, "Expected 'coroutine' keyword");
            fn             = ParseDeclaration();
            fn.IsAsync     = true;
            fn.IsCoroutine = true;
            fn.StartToken  = start;

            return fn;
        }

        var isInlineFn = Token.IsFunctionKeyword && Next.IsLBrace;
        if (isInlineFn) {
            return ParseInlineDeclaration();
        }

        throw new Exception("Failed to parse function declaration");
    }

    public InlineFunctionDeclaration ParseDeclaration(InlineFunctionDeclaration fn) {
        fn.Parameters = ArgumentDeclarationListParser.ParseNode();

        if (Token is {IsIdentifier: true, IsLBrace: false}) {
            fn.ReturnType = TypeIdentifierExpressionParser.ParseNode();
        }

        var body = BlockExpressionParser.ParseNode();
        fn.Body.Nodes.AddRange(body.Nodes);

        fn.EndToken = Prev;


        if (!fn.HasReturnStatementDefined) {
            fn.Body.Add(new ReturnStatement(null));
        }

        return fn;
    }

    public InlineFunctionDeclaration ParseInlineDeclaration() {
        var start        = EnsureAndConsume(Keyword.Function, "Expected 'function' keyword");

        var fn = new InlineFunctionDeclaration {
            StartToken = start,
        };

        return ParseDeclaration(fn);
    }
    public FunctionDeclaration ParseDeclaration() {
        var start        = EnsureAndConsume(Keyword.Function, "Expected 'function' keyword");
        var functionName = IdentifierExpressionParser.ParseNode();

        var fn = new FunctionDeclaration(functionName) {
            StartToken = start,
        };

        return (FunctionDeclaration) ParseDeclaration(fn);
    }

    public bool Matches() {
        if (Token.IsFunctionKeyword && Next.IsIdentifier)
            return true;

        if (Token.IsAsyncKeyword && Next.IsFunctionKeyword)
            return true;

        if (Token.IsCoroutineKeyword && Next.IsFunctionKeyword)
            return true;

        if (Token.IsAsyncKeyword && Next.IsCoroutineKeyword && NextNext.IsFunctionKeyword)
            return true;

        return false;
    }

    ExpressionStatement IStatementParser<ExpressionStatement>.Parse() {
        return Parse();
    }
}