using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.Interpreter.Execution.Statements;
using CSScriptingLang.Lexing;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.Parsing.Parsers;
using CSScriptingLang.Utils;

namespace CSScriptingLang.Parsing;

public partial class Parser
{
    private DefDeclaration_FunctionNode ParseFunctionDefDeclaration() {
        // Token start = EnsureAndConsume(TokenType.At, "Expected '@' before 'def' keyword");
        Token start = EnsureAndConsume(Keyword.Def, "Expected 'def' keyword");
        if (Token.IsFunctionKeyword) {
            EnsureAndConsume(Keyword.Function, "Expected 'function' keyword");
        }

        var functionName = EnsureAndConsume(TokenType.Identifier, "Expected function name");

        var fn = new DefDeclaration_FunctionNode(functionName.Value) {
            StartToken = start,
        };

        fn.Parameters = ParseArgumentListDeclaration();
        fn.EndToken   = Token;

        return fn;
    }

    public InlineFunctionDeclaration ParseLambdaFunction() {
        var fn = new InlineFunctionDeclaration {StartToken = Token};

        fn.Parameters = ParseArgumentListDeclaration();


        if (Token is {IsIdentifier: true, IsLBrace: false}) {
            fn.ReturnType = ParseTypeIdentifier("Expected return type");
        }
        EnsureAndConsume(TokenType.Arrow, "Expected '=>' after lambda function parameters");

        // At this point we could have:
        //   - `() => { .... }` -> Parse as block
        //   - `() => expr` -> Parse as expression(but wrap as a block + return statement)

        if (Token.IsLBrace) {
            fn.Body = ParseBlock();
        } else {
            var expr = ParseExpression();
            fn.Body = new BlockExpression {
                StartToken = expr.StartToken,
                EndToken   = expr.EndToken,
                Nodes      = {new ReturnStatement(expr)},
            };
        }

        if (Token.IsSemicolon) {
            Advance();
        }

        fn.EndToken = Prev;

        return fn;
    }

    private InlineFunctionDeclaration ParseInlineFunctionDeclaration(Action<BaseNode> OnNode = null) {
        var start = EnsureAndConsume(Keyword.Function, "Expected 'function' keyword");

        var fn = new InlineFunctionDeclaration {
            StartToken = start,
        };
        fn.Parameters = ParseArgumentListDeclaration();

        if (Token is {IsIdentifier: true, IsLBrace: false}) {
            fn.ReturnType = ParseTypeIdentifier("Expected return type");
        }

        fn.Body = ParseBlock(OnNode);

        fn.EndToken = Prev;

        return fn;
    }

    public List<Keywords.FunctionModifier> ParseFunctionModifiers() {
        var applyModifiers = new List<Keywords.FunctionModifier>();

        while(Keywords.HasFunctionModifiers(Token, out var applyFn)) {
            applyModifiers.Add(applyFn);
            Advance();
        }
        
        return applyModifiers;
    }

    public FunctionDeclaration ParseFunctionDeclarationWithKeywords(
        bool                            requiresFunctionKeyword = true,
        bool                            requiresBody            = true,
        List<Keywords.FunctionModifier> applyModifiers          = null
    ) {
        var start          = Token;

        if (applyModifiers == null) {
            applyModifiers = ParseFunctionModifiers();
        }
        /*var applyModifiers = new List<Action<InlineFunctionDeclaration>>();

        while (!Token.IsFunctionKeyword) {
            if (!Keywords.HasFunctionModifiers(Token, out var applyFn)) {
                break;
            }

            applyModifiers.Add(applyFn);
            Advance();
        }*/

        requiresBody = !applyModifiers.Any(x => x.Token.IsDefKeyword) && requiresBody;
        
        var fn = ParseFunctionDeclaration(
            requiresFunctionKeyword: requiresFunctionKeyword,
            requiresBody: requiresBody
        );
        foreach (var apply in applyModifiers) {
            apply.Apply(fn);
        }

        fn.StartToken = start;

        return fn;

        /*FunctionDeclaration fn;

        var isSeqFn = Token.IsSeqKeyword && Next.IsFunctionKeyword;
        if (isSeqFn) {
            var start = EnsureAndConsume(Keyword.Seq, "Expected 'seq' keyword");
            fn            = ParseFunctionDeclaration();
            fn.IsSeq      = true;
            fn.StartToken = start;
            return fn;
        }

        var isAsyncFn = Token.IsAsyncKeyword && Next.IsFunctionKeyword;
        if (isAsyncFn) {
            var start = EnsureAndConsume(Keyword.Async, "Expected 'async' keyword");
            fn            = ParseFunctionDeclaration();
            fn.IsAsync    = true;
            fn.StartToken = start;
            return fn;
        }

        var isCoroutine = Token.IsCoroutineKeyword && Next.IsFunctionKeyword;
        if (isCoroutine) {
            var start = EnsureAndConsume(Keyword.Coroutine, "Expected 'coroutine' keyword");
            fn             = ParseFunctionDeclaration();
            fn.IsCoroutine = true;
            fn.StartToken  = start;
            return fn;
        }

        var isAsyncCor = Token.IsAsyncKeyword && Next.IsCoroutineKeyword && NextNext.IsFunctionKeyword;
        if (isAsyncCor) {
            var start = EnsureAndConsume(Keyword.Async, "Expected 'async' keyword");
            EnsureAndConsume(Keyword.Coroutine, "Expected 'coroutine' keyword");
            fn             = ParseFunctionDeclaration();
            fn.IsAsync     = true;
            fn.IsCoroutine = true;
            fn.StartToken  = start;

            return fn;
        }*/

        // throw new Exception("Failed to parse function declaration");
    }

    private FunctionDeclaration ParseFunctionDeclaration(
        bool requiresFunctionKeyword = true,
        bool requiresBody            = true
    ) {

        Token start = null;
        if (requiresFunctionKeyword) {
            start = EnsureAndConsume(Keyword.Function, "Expected 'function' keyword");
        }

        var functionName = ParseIdentifier("Expected function name");
        if (!requiresFunctionKeyword) {
            start = functionName.StartToken;
        }

        var fn = new FunctionDeclaration(functionName) {
            StartToken = start,
        };

        fn.Parameters = ParseArgumentListDeclaration();
        fn.Attributes.PushRange(AttributeStack);

        // if (Token is {IsIdentifier: true, IsLBrace: false}) {
        //     fn.ReturnType = ParseTypeIdentifier("Expected return type");
        // }
        if (Token is {IsIdentifier: true, IsLBrace: false}) {
            fn.ReturnType = TypeIdentifierExpressionParser.ParseNode();
        }

        if (requiresBody) {
            fn.Body = ParseBlock();
        }

        fn.StartToken = start;
        fn.EndToken   = Prev;

        return fn;
    }

    private CallExpression ParseFunctionCall(Expression left = null) {
        var start = Token;
        var (isRegularFunction, hasTypeParameters) = IsFnCallLike(skipIdentifier: left != null);
        if (!isRegularFunction) {
            Expected("Expected function call", start);
            return null;
        }

        var ident = left ?? ParseIdentifier("Expected function name");

        var fnNode = new CallExpression(ident);

        if (hasTypeParameters) {
            fnNode.TypeParameters = ParseTypeParameters();
        }

        fnNode.Arguments = ParseArgumentsList();

        AdvanceIfSemicolon();

        fnNode.EndToken = Prev;

        return fnNode;
    }
}