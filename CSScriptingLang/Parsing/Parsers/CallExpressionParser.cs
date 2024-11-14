using CSScriptingLang.Interpreter.Execution.Expressions;

namespace CSScriptingLang.Parsing.Parsers;

public class CallExpressionParser : SubParserType<CallExpressionParser, CallExpression>, IInfixParser<CallExpression>
{
    public int Precedence => (int) PrecedenceValue.Postfix;

    public override CallExpression Parse(Expression left) {
        var start = Token;
        var (isRegularFunction, hasTypeParameters) = IsFnCallLike(skipIdentifier: left != null);
        if (!isRegularFunction) {
            Expected("Expected function call");
            return null;
        }

        var ident = left ?? IdentifierExpressionParser.ParseNode();

        var fnNode = new CallExpression(ident);

        if (hasTypeParameters) {
            fnNode.TypeParameters = Parser.ParseTypeParameters();
        }

        fnNode.Arguments = Parser.ParseArgumentsList();

        fnNode.EndToken = Token;

        return fnNode;
    }

    private (bool isRegularFunction, bool hasTypeParameters) IsFnCallLike(bool skipIdentifier = false) {
        var regularCallCheck = !skipIdentifier ? Token.IsIdentifier && Next.IsLParen : Token.IsLParen;
        if (regularCallCheck) {
            return (true, false);
        }

        var callWithTypeParamsCheck = !skipIdentifier ? Token.IsIdentifier && Next.IsLAngle : Token.IsLAngle;
        if (callWithTypeParamsCheck) {
            if (
                LookAheadSequential(
                    [() => Token.IsRAngle, () => Token.IsLParen],
                    [() => Token.IsLParen || Token.IsLBrace]
                )
               .SetStartPosition(() => {
                    if (!skipIdentifier)
                        Advance();
                    if (!Token.IsLAngle)
                        throw new Exception("Expected '<' after '->'");
                })
               .Execute((l, m) => l.Rollback())
            ) {
                return (true, true);
            }
        }

        return (false, false);
    }

}