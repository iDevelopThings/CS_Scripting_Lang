using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.Lexing;

namespace CSScriptingLang.Parsing.Parsers;

public class PrefixOperatorParser : SubParserType<PrefixOperatorParser, UnaryOpExpression>, IPrefixParser<UnaryOpExpression>
{
    private readonly int _precedence;

    public PrefixOperatorParser() { }
    public PrefixOperatorParser(int precedence) {
        _precedence = precedence;
    }

    public override UnaryOpExpression Parse() {
        var opToken = EnsureAndConsume(TokenType.Operator, "Expected operator");

        var op    = Token.Op;
        var right = Parser.ParseExpression(_precedence);

        return new UnaryOpExpression(op, right) {
            StartToken = opToken,
            EndToken   = right.EndToken,
        };
    }
}

public class PostfixOperatorParser : SubParserType<PostfixOperatorParser, UnaryOpExpression>, IInfixParser<UnaryOpExpression>
{
    public int Precedence { get; }

    public PostfixOperatorParser() { }
    public PostfixOperatorParser(int precedence) {
        Precedence = precedence;
    }

    public override UnaryOpExpression Parse(Expression left) {
        var op = Advance();
        
        return new UnaryOpExpression(op.Op, left, true) {
            StartToken = left.StartToken,
            EndToken   = Prev,
        };
    }
}

public class BinaryOperatorParser : SubParserType<BinaryOperatorParser, BinaryOpExpression>, IInfixParser<BinaryOpExpression>
{
    public int  Precedence         { get; }
    public bool IsRightAssociative { get; }

    public BinaryOperatorParser() { }
    public BinaryOperatorParser(int precedence, bool isRightAssociative = false) {
        Precedence         = precedence;
        IsRightAssociative = isRightAssociative;
    }

    public override BinaryOpExpression Parse(Expression left) {
        var op    = Token.Op;
        Advance();
        
        var right = Parser.ParseExpression(Precedence - (IsRightAssociative ? 1 : 0));

        return new BinaryOpExpression(left, op, right) {
            StartToken = left.StartToken,
            EndToken   = right.EndToken,
        };
    }
}