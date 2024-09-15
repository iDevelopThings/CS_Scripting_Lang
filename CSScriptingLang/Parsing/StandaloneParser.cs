using CSScriptingLang.Interpreter.Execution.Declaration;
using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.Interpreter.Execution.Statements;
using CSScriptingLang.Interpreter.Modules;
using CSScriptingLang.Lexing;
using CSScriptingLang.Parsing.AST;

namespace CSScriptingLang.Parsing;

public class StandaloneParser : ParserBase
{
    public ProgramExpression Program = new();

    private NodeScopeStack<BlockExpression> blockScopeStack = new();

    private ASTParentLinker Linker { get; set; }

    public StandaloneParser(Script script) {
        Script = script;

        Lexer = script.AstData.Lexer;
        Lexer.ResetToStart();
    }

    public StandaloneParser(string input) {
        Lexer = new LexerTokenStream(input);
    }

    public int AppendInput(string input) {
        Lexer.AppendInput(input);

        Advance();

        var curStatements = Program.Nodes.Count;

        if (Token.IsImportKeyword) {
            Program.Imports = ParseImportStatements();
        }

        while (Token.Type != TokenType.EOF) {
            Program.Add(ParseStatement());

            if (Token.Type == TokenType.Semicolon) {
                Advance();
            }

            if (Next.IsEOF) {
                break;
            }
        }

        Program.EndToken = Token;

        Linker.ProcessNodes(Program);

        var endStatements = Program.Nodes.Count;

        Logger.Debug($"Parsed {endStatements - curStatements} statements, now at {endStatements} statements");

        return endStatements - curStatements;
    }

    public ProgramExpression Parse(bool link = true) {
        try {
            ParseProgram();
        }
        catch (SyntaxException e) {
            Logger.Exception(e);
        }

        if (link) {
            Linker = new ASTParentLinker(Script);
            Linker.ProcessNodes(Program);
        }

        return Program;
    }

    public List<Expression> ParseExpressionNodes(bool link = true) {
        try {
            ParseProgram(false);
        }
        catch (SyntaxException e) {
            Logger.Exception(e);
        }

        if (link) {
            Linker = new ASTParentLinker(Script);
            Linker.ProcessNodes(Program);
        }

        return Program.OfType<Expression>().ToList();
    }

    private void ParseProgram(bool requireFullProgram = true) {
        Program.StartToken = Token;

        if (requireFullProgram) {
            if (Token.IsModuleKeyword) {
                Program.ModuleDeclaration = ParseModuleDeclaration();
            } else {
                LogError("Expected `module \"name\";` declaration at the start of the file");
            }

            if (Token.IsImportKeyword) {
                Program.Imports = ParseImportStatements();
            }
        }
        while (Token.Type != TokenType.EOF) {
            if (Token.IsModuleKeyword) {
                LogError("Module declaration must be at the start of the file");
            }

            Program.Add(ParseStatement());

            if (Token.Type == TokenType.Semicolon) {
                Advance();
            }

            if (Next.IsEOF) {
                break;
            }
        }

        Program.EndToken = Token;
    }


    private BaseNode ParseStatement() {
        // using var _ = ScopeTimer.NewPrefixed(CurrentToken.ToString());
        BaseNode parseNode() {
            if (Token.IsAt && Next.IsDefKeyword && NextNext.IsIdentifier)
                return ParseFunctionDefDeclaration();
            if (Token.IsVarKeyword)
                return ParseVariableDeclaration();
            if (Token.IsForKeyword)
                return ParseForLoop();
            if (Token.IsIfKeyword)
                return ParseIfStatement();
            if (Token.IsReturnKeyword)
                return ParseReturnStatement();
            if (Token.IsDeferKeyword)
                return ParseDeferStatement();
            if (Token.IsSignalKeyword && Next.IsIdentifier && NextNext.IsLParen)
                return ParseSignalDeclaration();
            if (Token.IsIdentifier && Next.IsLParen)
                return ParseFunctionCall();
            if (Token.IsTypeKeyword && Next.IsIdentifier && NextNext.IsTypeDeclarationKeyword) {
                return ParseTypeDeclaration();
            }

            {
                var isRegularFn = Token.IsFunctionKeyword && Next.IsIdentifier;
                var isAsyncFn   = Token.IsAsyncKeyword && Next.IsFunctionKeyword;
                var isCoroutine = Token.IsCoroutineKeyword && Next.IsFunctionKeyword;
                var isAsyncCor  = Token.IsAsyncKeyword && Next.IsCoroutineKeyword && NextNext.IsFunctionKeyword;
                if (
                    isRegularFn || isAsyncFn || isCoroutine || isAsyncCor
                ) {
                    return ParseFunctionDeclarationWithKeywords();
                }
            }

            if (Token.IsVarKeyword)
                return ParseVariableDeclaration();

            if (Token.IsYieldKeyword)
                return ParseYieldStatement();
            if (Token.IsAwaitKeyword)
                return ParseAwaitStatement();

            return ParseExpression();
        }

        BaseNode node = parseNode();

        if (Token.IsSemicolon) {
            Advance();
        }

        return node;
    }


    private TypeDeclaration ParseTypeDeclaration() {
        var start = EnsureAndConsume(Keyword.Type, "Expected 'type' keyword");
        var ident = ParseIdentifier("Expected type name");
        var type  = EnsureAndConsume(Keyword.TypeDeclaration, "Expected type declaration(struct or interface)");

        TypeDeclaration node = type switch {
            {IsStructKeyword   : true} => new StructDeclaration(ident) {StartToken        = start},
            {IsInterfaceKeyword: true} => new InterfaceDeclarationNode(ident) {StartToken = start},
            _                          => null,
        };

        if (node == null) {
            LogError("Invalid type declaration", start);
            return null;
        }

        EnsureAndConsume(TokenType.LBrace, "Expected opening brace for type declaration");

        while (Token is {IsRBrace: false, IsEOF: false}) {
            var fieldName = ParseIdentifier("Expected field name");

            // Method decl; `FuncName() Type {}`
            if (Token.IsLParen) {

                var fn = new FunctionDeclaration(fieldName) {
                    StartToken = fieldName.StartToken,
                };

                fn.Parameters = ParseArgumentListDeclaration();
                fn.ReturnType.Set(Token.IsIdentifier ? EnsureAndConsume(TokenType.Identifier, "Expected return type").Value : "Unit");
                // fn.ReturnType = Token.IsIdentifier ? EnsureAndConsume(TokenType.Identifier, "Expected return type").Value : "void";

                if (type.IsStructKeyword) {
                    var body = ParseBlock();
                    fn.Body.Nodes.AddRange(body.Nodes);
                    AdvanceIfSemicolon();
                    fn.EndToken = body.EndToken;
                    if (!fn.HasReturnStatementDefined) {
                        fn.Body.Add(new ReturnStatement(null));
                    }
                } else {
                    AdvanceIfSemicolon();
                    fn.EndToken = Token;
                }

                node.Methods.Add(fn);

                continue;
            }


            var fieldType = ParseIdentifier("Expected field type");

            node.Members.Add(new TypeDeclarationMemberNode(fieldName, fieldType));


        }

        var blockEnd = EnsureAndConsume(TokenType.RBrace, "Expected closing brace for type declaration");

        node.StartToken = start;
        node.EndToken   = blockEnd;

        return node;
    }

    private YieldStatement ParseYieldStatement() {
        var start = EnsureAndConsume(Keyword.Yield, "Expected 'yield' keyword");

        var node = new YieldStatement {
            StartToken = start,
        };

        if (!Token.IsSemicolon)
            node.Value = ParseExpression();

        if (Token.IsSemicolon) {
            Advance();
        }

        return node;
    }
    private AwaitStatement ParseAwaitStatement() {
        var start = EnsureAndConsume(Keyword.Await, "Expected 'await' keyword");

        var node = new AwaitStatement {
            StartToken = start,
        };

        if (!Token.IsSemicolon)
            node.Value = ParseExpression();

        if (Token.IsSemicolon) {
            Advance();
        }

        return node;
    }

    private ReturnStatement ParseReturnStatement() {
        var start = EnsureAndConsume(Keyword.Return, "Expected 'return' keyword");

        Expression value = null;
        if (!Token.IsSemicolon) {
            value = ParseExpression();
        }

        if (Token.IsSemicolon) {
            Advance();
        }

        var node = new ReturnStatement(value) {
            StartToken = start,
            EndToken   = Token,
        };

        if (blockScopeStack.Count > 0) {
            blockScopeStack.Current.ReturnNode = node;
        }

        return node;
    }

    private IfStatementNode ParseIfStatement() {
        // Consume 'if'
        var start = EnsureAndConsume(Keyword.If, "Expected 'if' keyword");
        // Expect a '(' for the condition & consume it
        EnsureAndConsume(TokenType.LParen, "Expected '(' after 'if'");

        // Parse the condition expression
        var condition = ParseExpression();

        // Expect a ')' after the condition & consume it
        EnsureAndConsume(TokenType.RParen, "Expected ')' after condition");

        // Parse the 'then' branch, which is a block statement
        var thenBranch = ParseBlock();

        // Check for an 'else' part
        BlockExpression elseBranch = null;
        if (Token.IsElseKeyword) {
            Advance();                 // Consume 'else'
            elseBranch = ParseBlock(); // Parse the else block
        }

        return new IfStatementNode(condition, thenBranch, elseBranch) {
            StartToken = start,
            EndToken   = Token,
        };
    }

    private BaseNode ParseForLoop() {
        var start = EnsureAndConsume(Keyword.For, "Expected 'for' keyword");
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
            initializer = ParseVariableDeclaration();
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

                /*if (varDecl?.Assignment?.Variable is TupleListDeclarationNode tuple) {
                    if (tuple.Nodes.Count == 0) {
                        LogError("Expected at least one variable in tuple");
                    }

                    forRange.Indexers.StartToken = varDecl.StartToken;
                    forRange.Indexers.EndToken   = varDecl.EndToken;

                    // indexVar = ((VariableNode) tuple.Nodes[0]).Name;

                    foreach (var el in tuple.Nodes) {
                        var assignment = new AssignmentNode(el, null) {
                            StartToken = el.StartToken,
                            EndToken   = el.EndToken,
                        };
                        // var assignment = new AssignmentNode(((VariableNode) el).Name, null) {
                        //     StartToken = el.StartToken,
                        //     EndToken   = el.EndToken,
                        // };
                        var decl = new VariableDeclarationNode(assignment) {
                            StartToken = el.StartToken,
                            EndToken   = el.EndToken,
                        };
                        forRange.Indexers.Add(decl);
                    }
                } else {
                    forRange.Indexers = new TupleListDeclarationNode() {
                        StartToken = varDecl.StartToken,
                        EndToken   = varDecl.EndToken,
                    };
                    forRange.Indexers.Add(varDecl.Initializers[0]);

                    /*
                    var assignment = new AssignmentNode(varDecl.Assignment.Variable, null) {
                        StartToken = varDecl.Assignment.StartToken,
                        EndToken   = varDecl.Assignment.EndToken,
                    };

                    forRange.Indexers.Add(new VariableDeclarationNode(assignment) {
                        StartToken = varDecl.StartToken,
                        EndToken   = varDecl.EndToken,
                    });
                    #1#

                    // indexVar = varDecl.Assignment.VariableName;
                }*/

                EnsureAndConsume(TokenType.RParen, "Expected ')' after iteration");

                forRange.Range    = varDecl.Values.OfType<RangeExpression>().FirstOrDefault();
                forRange.Body     = ParseBlock();
                forRange.EndToken = Token;

                return forRange;
            }

            AdvanceIfSemicolon();

            hasInit = true;
        }


        // Parse the condition part
        var preCond   = Token;
        var condition = ParseExpression();
        var initSemi  = EnsureAndConsume(TokenType.Semicolon, "Expected ';' after condition");


        if (!hasInit && condition.Cursor.First.Child<IdentifierExpression>(out var variable)) {
            initializer = new IdentifierExpression(variable.Name) {
                StartToken = preCond,
                EndToken   = initSemi,
            };
        }

        // Parse the iteration part
        var iteration = ParseExpression();

        EnsureAndConsume(TokenType.RParen, "Expected ')' after iteration");

        // Parse the body of the loop
        var body = ParseBlock();

        return new ForLoopStatement(initializer, condition, iteration, body) {
            StartToken = start,
            EndToken   = Token,
        };
    }

    private InlineFunctionDeclaration ParseLambdaFunction() {
        var fn = new InlineFunctionDeclaration {
            StartToken = Token,
        };

        fn.Parameters = ParseArgumentListDeclaration();

        fn.ReturnType.Set(
            Token is {IsIdentifier: true, IsArrow: false}
                ? EnsureAndConsume(TokenType.Identifier, "Expected return type").Value
                : "Unit"
        );

        EnsureAndConsume(TokenType.Arrow, "Expected '=>' after lambda function parameters");

        fn.Body = ParseBlock();

        if (!fn.HasReturnStatementDefined) {
            fn.Body.Add(new ReturnStatement(null));
        }

        if (Token.IsSemicolon) {
            Advance();
        }

        fn.EndToken = Prev;

        return fn;
    }
    private InlineFunctionDeclaration ParseInlineFunctionDeclaration() {
        var start = EnsureAndConsume(Keyword.Function, "Expected 'function' keyword");

        var fn = new InlineFunctionDeclaration {
            StartToken = start,
        };
        fn.Parameters = ParseArgumentListDeclaration();

        fn.ReturnType.Set(
            Token is {IsIdentifier: true, IsLBrace: false}
                ? EnsureAndConsume(TokenType.Identifier, "Expected return type").Value
                : "Unit"
        );

        var body = ParseBlock();
        fn.Body.Nodes.AddRange(body.Nodes);

        fn.EndToken = Prev;

        if (!fn.HasReturnStatementDefined) {
            fn.Body.Add(new ReturnStatement(null));
        }


        return fn;
    }

    private SignalDeclarationNode ParseSignalDeclaration() {
        var start = EnsureAndConsume(Keyword.Signal, "Expected 'signal' keyword");
        var ident = EnsureAndConsume(TokenType.Identifier, "Expected signal name");

        var signal = new SignalDeclarationNode {
            Name       = ident.Value,
            StartToken = start,
        };

        signal.Parameters = ParseArgumentListDeclaration();

        signal.EndToken = Token;

        if (Token.IsSemicolon) {
            Advance();
        }


        return signal;
    }


    private FunctionDeclaration ParseFunctionDeclarationWithKeywords() {
        var isRegularFn = Token.IsFunctionKeyword && Next.IsIdentifier;
        if (isRegularFn) {
            return ParseFunctionDeclaration();
        }

        FunctionDeclaration fn;

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
        }

        throw new Exception("Failed to parse function declaration");
    }

    private DefDeclaration_FunctionNode ParseFunctionDefDeclaration() {
        Token start = EnsureAndConsume(TokenType.At, "Expected '@' before 'def' keyword");
        EnsureAndConsume(Keyword.Def, "Expected 'def' keyword");
        EnsureAndConsume(Keyword.Function, "Expected 'function' keyword");

        var functionName = EnsureAndConsume(TokenType.Identifier, "Expected function name");

        var fn = new DefDeclaration_FunctionNode(functionName.Value) {
            StartToken = start,
        };

        fn.Parameters = ParseArgumentListDeclaration();
        fn.EndToken   = Token;

        return fn;
    }
    private FunctionDeclaration ParseFunctionDeclaration() {
        var start        = EnsureAndConsume(Keyword.Function, "Expected 'function' keyword");
        var functionName = ParseIdentifier("Expected function name");

        var fn = new FunctionDeclaration(functionName) {
            StartToken = start,
        };

        fn.Parameters = ParseArgumentListDeclaration();

        fn.ReturnType.Set(
            Token is {IsIdentifier: true, IsLBrace: false}
                ? EnsureAndConsume(TokenType.Identifier, "Expected return type").Value
                : "Unit"
        );

        var body = ParseBlock();
        fn.Body.Nodes.AddRange(body.Nodes);

        fn.StartToken = start;
        fn.EndToken   = Prev;


        if (!fn.HasReturnStatementDefined) {
            fn.Body.Add(new ReturnStatement(null));
        }


        return fn;
    }


    private BlockExpression ParseBlock() {
        var start = EnsureAndConsume(TokenType.LBrace, "Expected opening brace");

        var node = new BlockExpression {
            StartToken = start,
        };

        using var _ = blockScopeStack.Using(node);

        while (Token.Type != TokenType.RBrace && Token.Type != TokenType.EOF) {
            var statement = ParseStatement();
            node.Add(statement);
        }

        var end = EnsureAndConsume(TokenType.RBrace, "Expected closing brace");

        node.EndToken = end;

        return node;
    }

    private (bool isRegularFunction, bool hasTypeParameters) IsFnCallLike() {
        if (Token.IsIdentifier && Next.IsLParen) {
            return (true, false);
        }

        if (Token.IsIdentifier && Next.IsLAngle) {
            if (
                LookAheadSequential(
                    [() => Token.IsRAngle, () => Token.IsLParen],
                    [() => Token.IsLParen || Token.IsLBrace]
                )
               .SetStartPosition(() => {
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
    private bool IsLambdaDeclLike() => Token.IsFunctionKeyword && Next.IsLParen;

    private DeferStatement ParseDeferStatement() {
        var start = EnsureAndConsume(Keyword.Defer, "Expected 'defer' keyword");

        Expression node;
        if (IsLambdaDeclLike()) {
            node = ParseInlineFunctionDeclaration();
            var args = ParseArgumentsList();
            node = new CallExpression((Expression) node, args);
        } else {
            node = ParseFunctionCall();
        }

        var defer = new DeferStatement(node) {
            StartToken = start,
            EndToken   = Prev,
        };

        return defer;
    }

    private CallExpression ParseFunctionCall() {
        var (isRegularFunction, hasTypeParameters) = IsFnCallLike();
        if (!isRegularFunction) {
            LogError("Expected function call");
            return null;
        }

        var ident = ParseIdentifier("Expected function name");

        var fnNode = new CallExpression(ident);

        if (hasTypeParameters) {
            fnNode.TypeParameters = ParseTypeParameters();
        }

        fnNode.Arguments = ParseArgumentsList();

        fnNode.EndToken = Token;

        return fnNode;
    }

    private TypeParametersListNode ParseTypeParameters() {
        var start = EnsureAndConsume(TokenType.LAngle, "Expected '<' before type parameters");

        var args = new TypeParametersListNode() {
            StartToken = start,
        };

        while (!Token.Is(TokenType.RAngle | TokenType.EOF)) {
            var id = EnsureAndConsume(TokenType.Identifier, "Expected type parameter");
            var node = new TypeParameterNode {
                StartToken = id,
                EndToken   = id,
                Name       = id.Value,
            };
            args.Add(node);

            if (Token.IsComma) {
                Advance();
            } else if (!Token.IsRAngle) {
                LogError("Expected comma or closing angle bracket");
                return null;
            }
        }

        EnsureAndConsume(TokenType.RAngle, "Expected closing angle bracket");

        args.EndToken = Token;

        return args;
    }

    private ExpressionListNode ParseArgumentsList() {
        var start = EnsureAndConsume(TokenType.LParen, "Expected opening parenthesis");

        var args = new ExpressionListNode() {
            StartToken = start,
        };

        while (!Token.Is(TokenType.RParen | TokenType.EOF)) {
            args.Add(ParseExpression());

            if (Token.IsComma) {
                Advance();
            } else if (!Token.IsRParen) {
                LogError("Expected comma or closing parenthesis");
                return null;
            }
        }

        EnsureAndConsume(TokenType.RParen, "Expected closing parenthesis");

        args.EndToken = Token;

        return args;
    }

    private ArgumentListDeclarationNode ParseArgumentListDeclaration() {
        var start = EnsureAndConsume(TokenType.LParen, "Expected opening parenthesis");

        var args = new ArgumentListDeclarationNode();

        while (!Token.IsRParen || Token.IsEOF) {
            var arg = new ArgumentDeclarationNode();

            if (Token.IsDotDotDot && Next.IsIdentifier) {
                arg.IsVariadic = true;
                arg.StartToken = Advance();

                var paramName = EnsureAndConsume(TokenType.Identifier, "Expected a parameter name");
                arg.Name     = paramName.Value;
                arg.EndToken = paramName;
            } else {
                var paramType = EnsureAndConsume(TokenType.Identifier, "Expected a type name identifier");
                arg.TypeName = paramType.Value;

                var paramName = EnsureAndConsume(TokenType.Identifier, "Expected a parameter name");
                arg.Name = paramName.Value;

                arg.StartToken = paramType;
                arg.EndToken   = paramName;
            }

            args.Add(arg);

            if (Token.IsComma)
                Advance();
            else if (!Token.IsRParen)
                LogError("Expected comma or closing parenthesis");
        }

        var end = EnsureAndConsume(TokenType.RParen, "Expected closing parenthesis");

        args.StartToken = start;
        args.EndToken   = end;

        return args;
    }

    private VariableDeclarationNode ParseVariableDeclaration() {
        var start = EnsureAndConsume(Keyword.Var, "Expected 'var' keyword");

        var assignment = new VariableDeclarationNode {
            StartToken = start,
            EndToken   = Token,
        };

        var varNodes = ParsePrimary();
        EnsureAndConsume(TokenType.Operator, "Expected '=' after variable name");
        var exprNodes = ParseExpression();

        assignment.AddPairs(varNodes, exprNodes);

        assignment.EndToken = AdvanceIfSemicolon();

        return assignment;
    }

    private RangeExpression ParseRangeExpression() {
        var start = EnsureAndConsume(Keyword.Range, "Expected 'range' keyword");

        var expr = ParseExpression();

        return new RangeExpression(expr) {
            StartToken = start,
            EndToken   = Token,
        };
    }

    private Expression ParseExpression() {
        if (Token.IsOp(OperatorType.Minus, OperatorType.Not)) {
            var op    = Advance();
            var right = ParseExpression();

            return new UnaryOpExpression(op.Op, right) {
                StartToken = op,
                EndToken   = Token,
            };
        }

        // if (Token.IsVarKeyword)
        //     return ParseVariableDeclaration();

        if (Token.IsRangeKeyword)
            return ParseRangeExpression();

        var expr = ParseAssignment();


        return expr;
    }

    private Expression ParseAssignment() {
        var node = ParseLogicalOr();

        if (Token.IsAssignmentOperator) {
            var s     = Advance();
            var right = ParseExpression();
            node = new BinaryOpExpression(node, OperatorType.Assignment, right) {
                StartToken = s,
                EndToken   = Token,
            };
        }

        return node;
    }

    private Expression ParseTerm() {
        var node = ParseFactor();

        while (
            /*Token.IsOp(
                   OperatorType.Plus, OperatorType.Minus,
                   OperatorType.PlusEquals, OperatorType.MinusEquals
               )*/
            Token.IsPlusOperator || Token.IsMinusOperator ||
            Token.IsPlusEqualsOperator || Token.IsMinusEqualsOperator
        ) {

            var operatorToken = Advance();
            var right         = ParseFactor();
            node = new BinaryOpExpression(node, operatorToken.Op, right) {
                StartToken = operatorToken,
                EndToken   = Token,
            };
        }

        return node;
        // return ParseComparison(node); // Move to comparison if applicable
    }

    private Expression ParseComparison() {
        var left = ParseTerm();

        while (
            // Token.IsOp(
            //     OperatorType.Equals, OperatorType.NotEquals, OperatorType.GreaterThan,
            //     OperatorType.LessThan, OperatorType.GreaterThanOrEqual, OperatorType.LessThanOrEqual
            // )
            Token.IsEqualsOperator || Token.IsNotEqualsOperator || Token.IsGreaterThanOperator ||
            Token.IsLessThanOperator || Token.IsGreaterThanOrEqualOperator || Token.IsLessThanOrEqualOperator
        ) {

            var operatorToken = Advance();
            var right         = ParseTerm(); // Parse the right side of the comparison
            left = new BinaryOpExpression(left, operatorToken.Op, right) {
                StartToken = operatorToken,
                EndToken   = Token,
            };
        }

        return left;
    }

    private Expression ParseFactor() {
        var node = ParsePrimary();

        while (
            // Token.IsOp(OperatorType.Multiply, OperatorType.Divide, OperatorType.Modulus)
            Token.IsMultiplyOperator || Token.IsDivideOperator || Token.IsModulusOperator
        ) {
            var operatorToken = Advance();
            var right         = ParsePrimary();
            node = new BinaryOpExpression(node, operatorToken.Op, right) {
                StartToken = operatorToken,
                EndToken   = Token,
            };
        }

        return node;
    }

    private Expression ParseLogicalOr() {
        var left = ParseLogicalAnd(); // Logical AND has higher precedence, parse it first

        while (Token.IsOrOperator || Token.IsOrKeyword) {
            var operatorToken = Advance();
            var right         = ParseLogicalAnd();
            left = new BinaryOpExpression(left, operatorToken.Op, right) {
                StartToken = operatorToken,
                EndToken   = Token,
            };
        }

        return left;
    }

    private Expression ParseLogicalAnd() {
        var left = ParseComparison(); // Comparison has higher precedence, parse it first

        while (Token.IsAndOperator) {
            var operatorToken = Advance();
            var right         = ParseComparison();
            left = new BinaryOpExpression(left, operatorToken.Op, right) {
                StartToken = operatorToken,
                EndToken   = Token,
            };
        }

        return left;
    }

    private Expression ParsePrimary() {

        if (Token.IsLBracket) {
            return ParseArrayLiteral();
        }

        Expression valueNode = Token switch {
            {IsString : true} => ParseString(),
            {IsBoolean: true} => ParseBoolean(),
            {IsNumber : true} => ParseNumber(),
            _                 => null,
        };
        if (valueNode != null) {
            return valueNode;
        }

        if (IsLambdaDeclLike()) {
            return ParseInlineFunctionDeclaration();
        }

        if (Token.IsMatchKeyword && Next.IsLParen) {
            return ParseMatchExpression();
        }

        var (isRegularFunction, _) = IsFnCallLike();
        if (isRegularFunction) {
            return ParseFunctionCall();
        }


        // `SomeVar` or `&SomeVar`
        if (Token.IsIdentifier || Token.IsAnd && Next.IsIdentifier) {
            Token start = Token;
            var   isRef = Token.IsAnd;
            if (isRef) {
                start = EnsureAndConsume(TokenType.And, "Expected '&' before variable name");
            }

            Expression variableNode = ParseIdentifier().AsRef(isRef);
            variableNode.SetStartToken(start);

            /*BaseNode variableNode = new VariableNode(id.Value) {
                StartToken = start,
                EndToken   = Token,
                IsRef      = isRef,
            };*/

            variableNode = TryParseMemberAccess((IdentifierExpression) variableNode);

            var op = Token.Op;

            // Check for postfix increment or decrement
            if (Token.IsOp(OperatorType.Increment)) {
                var s = Advance();
                return new UnaryOpExpression(op, variableNode, true) {
                    StartToken = s,
                    EndToken   = Token,
                };
            }

            if (Token.IsOp(OperatorType.PlusEquals)) {
                var s = Advance();
                return new BinaryOpExpression(variableNode, op, ParseExpression()) {
                    StartToken = s,
                    EndToken   = Token,
                };
            }

            if (Token.IsOp(OperatorType.Decrement)) {
                var s = Advance();
                return new UnaryOpExpression(op, variableNode, true) {
                    StartToken = s,
                    EndToken   = Token,
                };
            }

            if (Token.IsOp(OperatorType.MinusEquals)) {
                var s = Advance();
                return new BinaryOpExpression(variableNode, op, ParseExpression()) {
                    StartToken = s,
                    EndToken   = Token,
                };
            }

            return variableNode;
        }

        if (Token.IsOp(OperatorType.Increment) || Token.IsOp(OperatorType.Decrement)) {
            var opToken      = Advance();
            var variableNode = ParsePrimary();
            // Prefix increment/decrement
            return new UnaryOpExpression(opToken.Op, variableNode) {
                StartToken = opToken,
                EndToken   = Token,
            };
        }

        if (Token.IsLParen) {
            // We want to search for lambdas
            //   `(int a, int b) => { return a + b; }`
            // but to do that, we need to know if we have:
            //   `(... skip ...) => {`
            if (
                LookAheadSequential(
                    [() => Token.IsRParen, () => Token.IsArrow, () => Token.IsLBrace],
                    [() => Token.IsLParen || Token.IsRBrace]
                ).Execute((l, m) => l.Rollback())
            ) {
                return ParseLambdaFunction();
            }

            // tuple = var (x, y, z), we're searching for `(x,`
            if (Sequence(TokenType.LParen, TokenType.Identifier, TokenType.Comma)) {
                return ParseExpressionList();
            }
            var exprCursorPos = Cursor;
            var s             = EnsureAndConsume(TokenType.LParen, "Expected opening parenthesis"); // consume '('

            var expression = ParseExpression();

            if (Token.IsComma) {
                Lexer.Rollback(exprCursorPos);
                return ParseExpressionList();
            }

            var e = EnsureAndConsume(TokenType.RParen, "Expected closing parenthesis"); // consume ')'

            expression.StartToken = s;
            expression.EndToken   = e;

            return expression;

        }

        if (Token.IsLBrace) {
            return ParseObjectLiteral();
        }

        if (Token.IsError) {
            throw new SyntaxException("Unexpected token", Token)
               .WithCaller()
               .WithInput(Lexer.GetInput());
        }

        LogError("Unexpected token: " + Token);

        return null;
    }
    private Expression ParseMatchExpression() {
        var start = EnsureAndConsume(Keyword.Match, "Expected 'match' keyword");

        var match = new MatchExpression() {
            StartToken = start,
        };

        EnsureAndConsume(TokenType.LParen, "Expected opening parenthesis");
        match.MatchAgainstExpr = ParseExpression() as Expression;
        EnsureAndConsume(TokenType.RParen, "Expected closing parenthesis");

        EnsureAndConsume(TokenType.LBrace, "Expected opening brace");

        while (Token is {IsRBrace: false, IsEOF: false}) {
            var caseStart = EnsureAndConsume(Keyword.Case, "Expected 'case' keyword");

            BasePatternMatchNode caseExpr = ParsePatternMatch();

            EnsureAndConsume(TokenType.Arrow, "Expected '=>' after case expression");

            var caseNode = new MatchCaseNode() {
                StartToken = caseStart,
                EndToken   = Token,
                Pattern    = caseExpr,
                Body       = ParseExpression() as Expression,
            };

            match.Cases.Add(caseNode);

            if (caseExpr is DefaultPatternNode) {
                match.DefaultCase = caseNode;
            }

            /*if (Token.IsCaseKeyword) {
                Advance();
            } else if (!Token.IsRBrace) {
                LogError("Expected semicolon or closing brace");
            }*/
        }

        match.EndToken = EnsureAndConsume(TokenType.RBrace, "Expected closing brace");

        return match;
    }

    private BasePatternMatchNode ParsePatternMatch() {
        var exprCursorPos = Cursor;

        using var _ = UsingMode(ParserMode.PatternMatching);

        // `case is TypeName`
        if (Token.IsIsKeyword && Next.IsIdentifier) {
            var start = EnsureAndConsume(Keyword.Is, "Expected 'is' keyword");
            var type  = EnsureAndConsume(TokenType.Identifier, "Expected type name");

            return new TypePatternNode(type.Value) {
                StartToken = start,
                EndToken   = type,
            };
        }

        if (Token.IsNumber || Token.IsString || Token.IsBoolean) {
            LiteralValueExpression lit = null;
            switch (Token) {
                case {IsNumber: true}:
                    lit = ParseNumber();
                    break;
                case {IsString: true}:
                    lit = ParseString();
                    break;
                case {IsBoolean: true}:
                    lit = ParseBoolean();
                    break;
                default:
                    LogError("Invalid pattern match expression");
                    break;
            }

            return new LiteralPatternNode(lit) {
                StartToken = lit!.StartToken,
                EndToken   = lit!.EndToken,
            };
        }

        if (Token.IsIdentifier) {
            return new IdentifierPatternNode(ParseIdentifier());
        }

        if (Token.IsUnderscore) {
            var underscore = EnsureAndConsume(TokenType.Underscore, "Expected '_'");
            return new DefaultPatternNode() {
                StartToken = underscore,
                EndToken   = underscore,
            };
        }

        LogError("Invalid pattern match expression");

        return null;
    }

    private TupleListDeclarationNode ParseTupleList() {
        var start = EnsureAndConsume(TokenType.LParen, "Expected opening parenthesis");

        var args = new TupleListDeclarationNode() {
            StartToken = start,
        };

        while (!Token.IsRParen || Token.IsEOF) {
            var expr = ParseExpression();
            args.Add(expr);

            if (Token.IsComma)
                Advance();
            else if (!Token.IsRParen)
                LogError("Expected comma or closing parenthesis");
        }

        var end = EnsureAndConsume(TokenType.RParen, "Expected closing parenthesis");

        args.StartToken = start;
        args.EndToken   = end;

        return args;
    }

    private ExpressionList ParseExpressionList() {
        var start = EnsureAndConsume(TokenType.LParen, "Expected opening parenthesis");

        var args = new ExpressionList {StartToken = start};

        while (!Token.IsRParen || Token.IsEOF) {
            var expr = ParseExpression();
            args.Add(expr);

            if (Token.IsComma)
                Advance();
            else if (!Token.IsRParen)
                LogError("Expected comma or closing parenthesis");
        }

        var end = EnsureAndConsume(TokenType.RParen, "Expected closing parenthesis");

        args.StartToken = start;
        args.EndToken   = end;

        return args;
    }

    private Expression TryParseMemberAccess(IdentifierExpression variable) {
        Expression left = variable;

        bool IsMemberAccess() => Token.IsDot && Next.IsIdentifier;
        bool IsIndexAccess()  => Token.IsLBracket && (Next.IsNumber || Next.IsString || Next.IsIdentifier);
        bool IsFunctionCall() => Token.IsLParen;

        while ((IsMemberAccess() || IsIndexAccess() || IsFunctionCall()) && !Token.IsEOF) {
            // variable.x
            if (IsMemberAccess()) {
                var start = EnsureAndConsume(TokenType.Dot, "Expected '.' after variable name `variable*.*property`");

                var prop = ParseIdentifier("Expected property name after '.' `variable.*property*`");

                left = new MemberAccessExpression(left, prop) {
                    StartToken = start,
                    EndToken   = Prev,
                };
            }

            // variable[x] | variable['x'] | variable[0..9] | variable[expression]
            if (IsIndexAccess()) {
                var s = EnsureAndConsume(TokenType.LBracket, "Expected opening bracket after variable name `variable*[*`");

                var index = ParseExpression() as Expression;

                var e = EnsureAndConsume(TokenType.RBracket, "Expected closing bracket after index expression `variable[index*]*`");

                left = new IndexAccessExpression(left, index) {
                    StartToken = s,
                    EndToken   = e,
                };
            }

            // variable.fn() | variable['fn']() | variable[0..9]() | variable[expression]()
            if (IsFunctionCall()) {
                var s    = Token;
                var args = ParseArgumentsList();
                var e    = Token;
                left = new CallExpression(left, args) {
                    StartToken = s,
                    EndToken   = e,
                };
            }
        }

        return left;
    }
    private IdentifierExpression ParseIdentifier(string reason = "Expected identifier") {
        var ident = EnsureAndConsume(TokenType.Identifier, reason);
        return new IdentifierExpression(ident.Value) {
            StartToken = ident,
            EndToken   = ident,
        };
    }

    private ObjectLiteralExpression ParseObjectLiteral() {
        EnsureAndConsume(TokenType.LBrace, "Expected opening brace");

        var obj = new ObjectLiteralExpression();

        while (!Token.Is(TokenType.RBrace | TokenType.EOF)) {
            var key = EnsureAnyOfAndConsume(
                TokenType.Identifier | TokenType.String | TokenType.Int32 | TokenType.Int64,
                "Expected identifier as key"
            );
            EnsureAndConsume(TokenType.Colon, "Expected colon after key");

            var value = ParseExpression();

            obj.AddProperty(key.Value, value);

            if (Token.IsComma) {
                Advance();
            } else if (!Token.IsRBrace) {
                LogError("Expected comma or closing brace");
                return null;
            }
        }

        EnsureAndConsume(TokenType.RBrace, "Expected closing brace");

        return obj;
    }

    private ArrayLiteralExpression ParseArrayLiteral() {
        var start = EnsureAndConsume(TokenType.LBracket, "Expected opening bracket");

        var array = new ArrayLiteralExpression() {
            StartToken = start,
        };

        while (!Token.Is(TokenType.RBracket | TokenType.EOF)) {
            var expr = ParseExpression();
            if (expr == null) {
                LogError("Expected expression in array literal");
                return null;
            }

            array.Elements.Add(expr);

            if (Token.IsComma) {
                Advance();
            } else if (!Token.IsRBracket) {
                LogError("Expected comma or closing bracket");
                return null;
            }
        }

        var end = EnsureAndConsume(TokenType.RBracket, "Expected closing bracket");

        array.EndToken = end;

        return array;
    }
}