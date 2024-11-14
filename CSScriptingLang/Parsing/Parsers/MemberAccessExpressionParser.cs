using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.Lexing;

namespace CSScriptingLang.Parsing.Parsers;

public class MemberAccessExpressionParser : SubParserType<MemberAccessExpressionParser, MemberAccessExpression>, IInfixParser<MemberAccessExpression>
{
    public int Precedence => (int) PrecedenceValue.Postfix;

    public override MemberAccessExpression Parse(Expression left) {
        EnsureAndConsume(TokenType.Dot, "Expected '.'");
        
        
        if (Parser.TryGetInfixParser(out var parser, false)) {
            var ident = IdentifierExpressionParser.ParseNode();
            left = parser.Parse(ident);
            
            
        
            return new MemberAccessExpression(left, null);
        }
       
        if(Parser.TryGetPrefixParser(out var prefixParser)) {
            // var ident = IdentifierExpressionParser.Parse(this, "Expected member name");
            var l = prefixParser.Parse();
            if(l is IdentifierExpression m) {
                return new MemberAccessExpression(left, m);
            }
        }
        
        else {
            var ident = IdentifierExpressionParser.ParseNode();    
            
            return new MemberAccessExpression(left, ident);
        }





        return new MemberAccessExpression(left, null);
    }
}

public class IndexAccessExpressionParser : SubParserType<IndexAccessExpressionParser, IndexAccessExpression>, IInfixParser<IndexAccessExpression>
{
    public int Precedence => (int) PrecedenceValue.Postfix;

    public override IndexAccessExpression Parse(Expression left) {
        var s = EnsureAndConsume(TokenType.LBracket, "Expected opening bracket after variable name `variable*[*`");

        var index = Parser.ParseExpression() as Expression;

        var e = EnsureAndConsume(TokenType.RBracket, "Expected closing bracket after index expression `variable[index*]*`");

        return new IndexAccessExpression(left, index) {
            StartToken = s,
            EndToken   = e,
        };
    }
}

public class RangeExpressionParser : SubParserType<RangeExpressionParser, RangeExpression>, IPrefixParser<RangeExpression>, IParserMatcher
{
    public override RangeExpression Parse() {
        var start = EnsureAndConsume(Keyword.Range, "Expected 'range' keyword");

        var expr = Parser.ParseExpression();

        return new RangeExpression(expr) {
            StartToken = start,
            EndToken   = Token,
        };
    }

    public bool Matches() {
        return Token.Is(Keyword.Range);
    }
}