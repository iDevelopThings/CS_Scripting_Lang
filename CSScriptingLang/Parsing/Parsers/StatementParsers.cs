using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.Interpreter.Execution.Statements;
using CSScriptingLang.Lexing;
using CSScriptingLang.Parsing.AST;

namespace CSScriptingLang.Parsing.Parsers;

public class ReturnStatementParser : SubParserType<ReturnStatementParser, ReturnStatement>,
                                     IStatementParser<ReturnStatement>,
                                     IParserMatcher
{
    public override ReturnStatement Parse() {
        var start = EnsureAndConsume(Keyword.Return, "Expected 'return' keyword");

        Expression value = null;
        if (!Token.IsSemicolon) {
            value = Parser.ParseExpression();
        }

        if (Token.IsSemicolon) {
            Advance();
        }

        var node = new ReturnStatement(value) {
            StartToken = start,
            EndToken   = Token,
        };

        if (BlockScopeStack.Count > 0) {
            BlockScopeStack.Current.ReturnNode = node;
        }

        return node;
    }

    public bool Matches() {
        return Token.Is(Keyword.Return);
    }

}

public class VarStatementParser : SubParserType<VarStatementParser, VariableDeclarationNode>,
                                  IStatementParser<VariableDeclarationNode>,
                                  IParserMatcher
{
    public override VariableDeclarationNode Parse() {
        var start = EnsureAndConsume(Keyword.Var, "Expected 'var' keyword");

        var assignment = new VariableDeclarationNode {
            StartToken = start,
            EndToken   = Token,
        };

        var expr = (Parser.ParseExpression() as BinaryOpExpression)!;
        assignment.AddPairs(expr.Left, expr.Right);

        // EnsureAndConsume(TokenType.Operator, "Expected '=' after variable name");
        // var exprNodes = Parser.ParseExpression();
        // assignment.AddPairs(varNodes, exprNodes);

        assignment.EndToken = AdvanceIfSemicolon();

        return assignment;
    }

    public bool Matches() {
        return Token.Is(Keyword.Var);
    }

}

public class IfStatementParser : SubParserType<IfStatementParser, IfStatementNode>,
                                 IStatementParser<IfStatementNode>,
                                 IParserMatcher
{
    public override IfStatementNode Parse() {
        // Consume 'if'
        var start = EnsureAndConsume(Keyword.If, "Expected 'if' keyword");
        // Expect a '(' for the condition & consume it
        EnsureAndConsume(TokenType.LParen, "Expected '(' after 'if'");

        // Parse the condition expression
        var condition = Parser.ParseExpression();

        // Expect a ')' after the condition & consume it
        EnsureAndConsume(TokenType.RParen, "Expected ')' after condition");

        // Parse the 'then' branch, which is a block statement
        var thenBranch = BlockExpressionParser.ParseNode();

        // Check for an 'else' part
        BlockExpression elseBranch = null;
        if (Token.IsElseKeyword) {
            Advance();                                      // Consume 'else'
            elseBranch = BlockExpressionParser.ParseNode(); // Parse the else block
        }

        return new IfStatementNode(condition, thenBranch, elseBranch) {
            StartToken = start,
            EndToken   = Token,
        };
    }

    public bool Matches() {
        return Token.Is(Keyword.If);
    }

}

public class ForLoopParser : SubParserType<ForLoopParser, Statement>,
                             IStatementParser<Statement>,
                             IParserMatcher
{
    public bool Matches() {
        return Token.Is(Keyword.For);
    }
    public override Statement Parse() {
        var start = EnsureAndConsume(Keyword.For, "Expected 'for' keyword");

        if (Token.IsLBrace) {
            var whileBody = BlockExpressionParser.ParseNode();

            return new ForWhileLoopStatement(whileBody) {
                StartToken = start,
                EndToken   = Prev,
            };
        }


        EnsureAndConsume(TokenType.LParen, "Expected '(' after 'for'");

        // Parse the initialization part
        BaseNode initializer = null;

        var hasInit = false;

        // We can also skip the var if it's defined outside the loop init
        // var i = 0; for(; i < 10; i++) { }
        if (Token.IsSemicolon) {
            Advance();
        } else {
            // var i = 0;
            initializer = VarStatementParser.ParseNode();
        }

        if (initializer != null) {
            if (initializer is VariableDeclarationNode varDecl && varDecl.HasInitializer<RangeExpression>()) {

                // for(var (index, value) = range arr) { }
                // for(var (index) = range arr) { }
                // for(var index = range arr) { }
                // for(var i = range 10) { }

                var forRange = new ForRangeStatement {
                    StartToken  = start,
                    Initializer = varDecl,
                    Indexers = new TupleListDeclarationNode() {
                        StartToken = varDecl.StartToken,
                        EndToken   = varDecl.EndToken,
                    },
                };


                varDecl.Initializers.ForEach(init => forRange.Indexers.Add(init));

                EnsureAndConsume(TokenType.RParen, "Expected ')' after iteration");

                forRange.RangeExpr = varDecl.Values.OfType<RangeExpression>().FirstOrDefault();
                forRange.Body      = BlockExpressionParser.ParseNode();
                forRange.EndToken  = Token;

                return forRange;
            }

            AdvanceIfSemicolon();

            hasInit = true;
        }


        // Parse the condition part
        var preCond   = Token;
        var condition = Parser.ParseExpression();
        var initSemi  = EnsureAndConsume(TokenType.Semicolon, "Expected ';' after condition");


        if (!hasInit && condition.Child<IdentifierExpression>(out var variable)) {
            initializer = new IdentifierExpression(variable.Name) {
                StartToken = preCond,
                EndToken   = initSemi,
            };
        }

        // Parse the iteration part
        var iteration = Parser.ParseExpression();

        EnsureAndConsume(TokenType.RParen, "Expected ')' after iteration");

        // Parse the body of the loop
        var body = BlockExpressionParser.ParseNode();

        return new ForLoopStatement(initializer, condition, iteration, body) {
            StartToken = start,
            EndToken   = Token,
        };
    }


}

public class AwaitStatementParser : SubParserType<AwaitStatementParser, AwaitStatement>,
                                    IStatementParser<AwaitStatement>,
                                    IParserMatcher
{
    public bool Matches() {
        return Token.Is(Keyword.Await);
    }

    public override AwaitStatement Parse() {
        var start = EnsureAndConsume(Keyword.Await, "Expected 'await' keyword");

        var node = new AwaitStatement {
            StartToken = start,
        };

        if (!Token.IsSemicolon)
            node.Value = Parser.ParseExpression();

        AdvanceIfSemicolon();

        node.EndToken = Prev;

        return node;
    }


}

public class BreakStatementParser : SubParserType<BreakStatementParser, BreakStatement>,
                                    IStatementParser<BreakStatement>,
                                    IParserMatcher
{
    public bool Matches() {
        return Token.Is(Keyword.Break);
    }

    public override BreakStatement Parse() {
        var start = EnsureAndConsume(Keyword.Break, "Expected 'break' keyword");

        Expression value = null;
        if (!Token.IsSemicolon) {
            value = NumberExpressionParser.ParseNode();
        }

        AdvanceIfSemicolon();

        var node = new BreakStatement(value as Int32Expression) {
            StartToken = start,
            EndToken   = Prev,
        };

        AdvanceIfSemicolon();
        
        return node;
    }


}

public class ContinueStatementParser : SubParserType<ContinueStatementParser, ContinueStatement>,
                                       IStatementParser<ContinueStatement>,
                                       IParserMatcher
{
    public bool Matches() {
        return Token.Is(Keyword.Continue);
    }

    public override ContinueStatement Parse() {
        var start = EnsureAndConsume(Keyword.Continue, "Expected 'continue' keyword");

        AdvanceIfSemicolon();

        var node = new ContinueStatement() {
            StartToken = start,
            EndToken   = Prev,
        };

        return node;
    }


}