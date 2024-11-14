using CSScriptingLang.Interpreter.Execution.Declaration;
using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.Interpreter.Execution.Statements;
using CSScriptingLang.Interpreter.Modules;
using CSScriptingLang.Lexing;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.Parsing.Parsers;

namespace CSScriptingLang.Parsing;

public partial class Parser : ParserBase
{
    public  ProgramExpression Program = new();
    private ASTParentLinker   Linker { get; set; }

    public Stack<AttributeDeclaration> AttributeStack { get; } = new();

    public bool CanParseModuleDeclaration  { get; set; } = true;
    public bool CanParseImportDeclarations { get; set; } = true;

    public Parser() {
        Parsers.AddPrefixParser<IdentifierExpressionParser>(TokenType.Identifier);
        // Parsers.AddPrefixParser<TypeIdentifierExpressionParser>(TokenType.Identifier);

        Parsers.AddPrefixParser<NumberExpressionParser>(TokenType.Int32);
        Parsers.AddPrefixParser<NumberExpressionParser>(TokenType.Int64);
        Parsers.AddPrefixParser<NumberExpressionParser>(TokenType.Double);
        Parsers.AddPrefixParser<NumberExpressionParser>(TokenType.Float);
        Parsers.AddPrefixParser<StringExpressionParser>(TokenType.String);
        Parsers.AddPrefixParser<BoolExpressionParser>(TokenType.Identifier | TokenType.KeywordIdentifier | TokenType.Boolean);

        // BinaryOperatorParser

        Parsers.AddInfixParser(OperatorType.Plus, new BinaryOperatorParser((int) PrecedenceValue.Addition));
        Parsers.AddInfixParser(OperatorType.Minus, new BinaryOperatorParser((int) PrecedenceValue.Addition));
        Parsers.AddInfixParser(OperatorType.Multiply, new BinaryOperatorParser((int) PrecedenceValue.Multiplication));
        Parsers.AddInfixParser(OperatorType.Divide, new BinaryOperatorParser((int) PrecedenceValue.Multiplication));
        Parsers.AddInfixParser(OperatorType.Modulus, new BinaryOperatorParser((int) PrecedenceValue.Multiplication));
        Parsers.AddInfixParser(OperatorType.Pow, new BinaryOperatorParser((int) PrecedenceValue.Multiplication));
        Parsers.AddInfixParser(OperatorType.BitLeftShift, new BinaryOperatorParser((int) PrecedenceValue.BitShift));
        Parsers.AddInfixParser(OperatorType.BitRightShift, new BinaryOperatorParser((int) PrecedenceValue.BitShift));
        Parsers.AddInfixParser(OperatorType.BitwiseAnd, new BinaryOperatorParser((int) PrecedenceValue.BitAnd));
        Parsers.AddInfixParser(OperatorType.Pipe, new BinaryOperatorParser((int) PrecedenceValue.BitOr));
        Parsers.AddInfixParser(OperatorType.BitXor, new BinaryOperatorParser((int) PrecedenceValue.BitXor));
        Parsers.AddPrefixParser(OperatorType.Minus, new PrefixOperatorParser((int) PrecedenceValue.Prefix));
        Parsers.AddPrefixParser(OperatorType.BitNot, new PrefixOperatorParser((int) PrecedenceValue.Prefix));

        // conditional operations
        Parsers.AddPrefixParser(OperatorType.Not, new PrefixOperatorParser((int) PrecedenceValue.Prefix));
        Parsers.AddInfixParser(OperatorType.And, new BinaryOperatorParser((int) PrecedenceValue.ConditionalAnd));
        Parsers.AddInfixParser(OperatorType.Or, new BinaryOperatorParser((int) PrecedenceValue.ConditionalOr));

        // relational operations
        Parsers.AddInfixParser(OperatorType.Equals, new BinaryOperatorParser((int) PrecedenceValue.Equality));
        Parsers.AddInfixParser(OperatorType.NotEquals, new BinaryOperatorParser((int) PrecedenceValue.Equality));
        Parsers.AddInfixParser(OperatorType.GreaterThan, new BinaryOperatorParser((int) PrecedenceValue.Relational));
        Parsers.AddInfixParser(OperatorType.GreaterThanOrEqual, new BinaryOperatorParser((int) PrecedenceValue.Relational));
        Parsers.AddInfixParser(OperatorType.LessThan, new BinaryOperatorParser((int) PrecedenceValue.Relational));
        Parsers.AddInfixParser(OperatorType.LessThanOrEqual, new BinaryOperatorParser((int) PrecedenceValue.Relational));

        Parsers.AddPrefixParser(TokenType.PlusPlus | TokenType.Operator, new PrefixOperatorParser((int) PrecedenceValue.Prefix));
        Parsers.AddPrefixParser(TokenType.MinusMinus | TokenType.Operator, new PrefixOperatorParser((int) PrecedenceValue.Prefix));

        Parsers.AddInfixParser(TokenType.PlusPlus | TokenType.Operator, new PostfixOperatorParser((int) PrecedenceValue.Postfix));
        Parsers.AddInfixParser(TokenType.MinusMinus | TokenType.Operator, new PostfixOperatorParser((int) PrecedenceValue.Postfix));

        // assignment
        Parsers.AddInfixParser(OperatorType.Assignment, new BinaryOperatorParser((int) PrecedenceValue.Assign, true));
        Parsers.AddInfixParser(OperatorType.PlusEquals, new BinaryOperatorParser((int) PrecedenceValue.Assign, true));
        Parsers.AddInfixParser(OperatorType.MinusEquals, new BinaryOperatorParser((int) PrecedenceValue.Assign, true));
        Parsers.AddInfixParser(OperatorType.MultiplyAssign, new BinaryOperatorParser((int) PrecedenceValue.Assign, true));
        Parsers.AddInfixParser(OperatorType.DivideAssign, new BinaryOperatorParser((int) PrecedenceValue.Assign, true));
        Parsers.AddInfixParser(OperatorType.ModulusAssign, new BinaryOperatorParser((int) PrecedenceValue.Assign, true));
        Parsers.AddInfixParser(OperatorType.PowAssign, new BinaryOperatorParser((int) PrecedenceValue.Assign, true));
        Parsers.AddInfixParser(OperatorType.BitLeftShiftAssign, new BinaryOperatorParser((int) PrecedenceValue.Assign, true));
        Parsers.AddInfixParser(OperatorType.BitRightShiftAssign, new BinaryOperatorParser((int) PrecedenceValue.Assign, true));
        Parsers.AddInfixParser(OperatorType.BitAndAssign, new BinaryOperatorParser((int) PrecedenceValue.Assign, true));
        Parsers.AddInfixParser(OperatorType.BitOrAssign, new BinaryOperatorParser((int) PrecedenceValue.Assign, true));
        Parsers.AddInfixParser(OperatorType.BitXorAssign, new BinaryOperatorParser((int) PrecedenceValue.Assign, true));

        Parsers.AddPrefixParser<ExpressionGroupParser>(TokenType.LParen);
        Parsers.AddInfixParser<CallExpressionParser>(TokenType.LParen);
        Parsers.AddInfixParser<MemberAccessExpressionParser>(TokenType.Dot);
        Parsers.AddInfixParser<IndexAccessExpressionParser>(TokenType.LBracket);
        Parsers.AddPrefixParser<ObjectLiteralParser>(TokenType.LBrace);
        Parsers.AddPrefixParser<ArrayLiteralParser>(TokenType.LBracket);
        Parsers.AddPrefixParser<FunctionParser>();
        Parsers.AddPrefixParser<RangeExpressionParser>();
        // Parsers.AddInfixParser<TypeParametersListParser>();

        Parsers.AddStatementParser<TypeDeclarationParser>();
        // Parsers.AddStatementParser<BlockExpressionParser>();
        Parsers.AddStatementParser<FunctionParser>();
        Parsers.AddStatementParser<ReturnStatementParser>();
        Parsers.AddStatementParser<VarStatementParser>();
        Parsers.AddStatementParser<IfStatementParser>();
        Parsers.AddStatementParser<ForLoopParser>();
        Parsers.AddStatementParser<AwaitStatementParser>();
        Parsers.AddStatementParser<BreakStatementParser>();
        Parsers.AddStatementParser<ContinueStatementParser>();

    }

    public Parser(Script script) : this() {
        Script = script;

        Lexer = script.Lexer;
        Lexer.ResetToStart();
    }

    public Parser(string input) : this() {
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

        LinkNodes(link);

        return Program;
    }

    public ProgramExpression ParseModule(bool link = true) {
        try {
            Program.StartToken = Token;

            Parsers.LoadAllParsers(this, true);

            if (Token.IsFunctionKeyword) {

                var moduleFn = ParseInlineFunctionDeclaration();

                Program.Add(moduleFn);
            } else {
                Expected("Expected module function declaration", Program.StartToken);
            }

            Program.EndToken = Prev;
        }
        catch (SyntaxException e) {
            Logger.Exception(e);
        }

        LinkNodes(link);

        return Program;
    }

    public void LinkNodes(bool link = true) {
        if (!link) return;
        Linker = new ASTParentLinker(Script);
        Linker.ProcessNodes(Program);
    }

    public List<Expression> ParseExpressionNodes(bool link = true) {
        try {
            ParseProgram(false);
        }
        catch (SyntaxException e) {
            Logger.Exception(e);
        }

        LinkNodes(link);

        return Program.AllOfType<Expression>().ToList();
    }

    private void ParseProgram(bool requireFullProgram = true) {
        Program.StartToken = Token;

        Parsers.LoadAllParsers(this, true);

        if (requireFullProgram) {
            if (Token.IsImportKeyword) {
                Program.Imports = ParseImportStatements();
            }
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
    }


    private new BaseNode ParseStatement() {
        // using var _ = ScopeTimer.NewPrefixed(CurrentToken.ToString());
        BaseNode parseNode() {
            // if (TryParseStatement(out var stmt)) {
            //     return stmt;
            // }
            if (CanParseModuleDeclaration && Token.IsModuleKeyword) {
                Program.ModuleDeclaration = ParseModuleDeclaration();
            }

            if (CanParseImportDeclarations && Token.IsImportKeyword) {
                Program.Imports = ParseImportStatements();
            }

            if (/*Token.IsAt &&*/ Next.IsDefKeyword && NextNext.IsIdentifier)
                return ParseFunctionDefDeclaration();
            if (Token.IsVarKeyword)
                return ParseVariableDeclaration();
            if (Token.IsForKeyword)
                return ParseForLoop();
            if (Token.IsIfKeyword)
                return ParseIfStatement();
            if (Token.IsReturnKeyword)
                return ParseReturnStatement();
            if (Token.IsBreakKeyword)
                return ParseBreakStatement();
            if (Token.IsContinueKeyword)
                return ParseContinueStatement();
            if (Token.IsLBracket && Next.IsIdentifier)
                return ParseAttribute();
            if (Token.IsDeferKeyword)
                return ParseDeferStatement();
            if (Token.IsSignalKeyword && Next.IsIdentifier && NextNext.IsLParen)
                return ParseSignalDeclaration();
            if (Token.IsMatchKeyword)
                return ParseMatchExpression();
            if (Token.IsTypeKeyword && Next.IsIdentifier && NextNext.IsTypeDeclarationKeyword)
                return TypeDeclarationParser.ParseNode();
            if (Token.IsIdentifier && Next.IsLParen)
                return ParseFunctionCall();
            if(Token.IsLBrace && !(Next.IsIdentifier && NextNext.IsColon))
                return ParseBlock();

            if (Keywords.IsFunctionWithModifiers(Token)) {
                return ParseFunctionDeclarationWithKeywords();
            }

            if (Token.IsVarKeyword)
                return ParseVariableDeclaration();

            if (Token.IsYieldKeyword)
                return ParseYieldStatement();
            if (Token.IsAwaitKeyword)
                return ParseAwaitStatement();

            // if (TryParseExpression(out var expr)) {
            // return expr;
            // }
            // LogError("Failed to parse statement");

            return ParseExpression();
        }

        BaseNode node = parseNode();

        AdvanceIfSemicolon();

        return node;
    }

    private AttributeDeclaration ParseAttribute() {
        var start = EnsureAndConsume(TokenType.LBracket, "Expected opening bracket for attribute");
        var ident = ParseIdentifier("Expected attribute name");

        var attr = new AttributeDeclaration(ident) {
            StartToken = start,
        };

        if (Token.IsLParen) {
            var args = ParseExpressionList();
            attr.Args = args;
        }

        EnsureAndConsume(TokenType.RBracket, "Expected closing bracket for attribute");

        attr.EndToken = Prev;

        AttributeStack.Push(attr);

        return attr;
    }
    public void ParseAttributes() {
        if (Token.IsLBracket && Next.IsIdentifier) {
            while (Token.IsLBracket && Next.IsIdentifier) {
                ParseAttribute();

                if (Token.IsSemicolon) {
                    Advance();
                }
                if (Token.IsEOF) {
                    break;
                }
            }
        }


    }


    /*private TypeDeclaration ParseTypeDeclaration() {
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

        node.Attributes.AddRange(AttributeStack);

        EnsureAndConsume(TokenType.LBrace, "Expected opening brace for type declaration");

        while (Token is {IsRBrace: false, IsEOF: false}) {

            ParseAttributes();

            var fieldName = ParseIdentifier("Expected field name");

            // Method decl; `FuncName() Type {}`
            if (Token.IsLParen) {

                var fn = new FunctionDeclaration(fieldName) {
                    StartToken = fieldName.StartToken,
                };

                fn.Attributes.AddRange(AttributeStack);

                fn.Parameters = ParseArgumentListDeclaration();

                if (Token is {IsIdentifier: true, IsLBrace: false}) {
                    fn.ReturnType = ParseTypeIdentifier("Expected return type");
                }

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

            var fieldType = ParseTypeIdentifier("Expected field type");

            var member = new TypeDeclarationMemberNode(fieldName, fieldType);
            member.Attributes.AddRange(AttributeStack);

            node.Members.Add(member);
        }

        var blockEnd = EnsureAndConsume(TokenType.RBrace, "Expected closing brace for type declaration");

        node.StartToken = start;
        node.EndToken   = blockEnd;

        return node;
    }*/

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
        
        node.EndToken = Prev;

        return node;
    }
    private AwaitStatement ParseAwaitStatement() {
        var start = EnsureAndConsume(Keyword.Await, "Expected 'await' keyword");

        var node = new AwaitStatement {
            StartToken = start,
        };

        if (!Token.IsSemicolon)
            node.Value = ParseExpression();

        AdvanceIfSemicolon();

        node.EndToken = Prev;

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
            EndToken   = Prev,
        };

        if (BlockScopeStack.Count > 0) {
            BlockScopeStack.Current.ReturnNode = node;
        }

        return node;
    }

    private BreakStatement ParseBreakStatement() {
        var start = EnsureAndConsume(Keyword.Break, "Expected 'break' keyword");

        Expression value = null;
        if (!Token.IsSemicolon) {
            value = ParseNumber();
        }

        AdvanceIfSemicolon();

        var node = new BreakStatement(value as Int32Expression) {
            StartToken = start,
            EndToken   = Prev,
        };

        AdvanceIfSemicolon();


        return node;
    }
    private ContinueStatement ParseContinueStatement() {
        var start = EnsureAndConsume(Keyword.Continue, "Expected 'continue' keyword");

        AdvanceIfSemicolon();

        var node = new ContinueStatement() {
            StartToken = start,
            EndToken   = Prev,
        };

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
            EndToken   = Prev,
        };
    }

    private BaseNode ParseForLoop() {
        var start = EnsureAndConsume(Keyword.For, "Expected 'for' keyword");

        if (Token.IsLBrace) {
            var whileBody = ParseBlock();

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

                forRange.RangeExpr = varDecl.Values.OfType<RangeExpression>().FirstOrDefault();
                forRange.Body      = ParseBlock();
                forRange.EndToken  = Token;

                return forRange;
            }

            AdvanceIfSemicolon();

            hasInit = true;
        }


        // Parse the condition part
        var preCond   = Token;
        var condition = ParseExpression();
        var initSemi  = EnsureAndConsume(TokenType.Semicolon, "Expected ';' after condition");


        if (!hasInit && condition.Child<IdentifierExpression>(out var variable)) {
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

    private SignalDeclaration ParseSignalDeclaration() {
        var start = EnsureAndConsume(Keyword.Signal, "Expected 'signal' keyword");
        var ident = EnsureAndConsume(TokenType.Identifier, "Expected signal name");

        var signal = new SignalDeclaration {
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

    private BlockExpression ParseBlock(Action<BaseNode> OnNode = null) {
        var start = EnsureAndConsume(TokenType.LBrace, "Expected opening brace");

        var node = new BlockExpression {
            StartToken = start,
        };

        using var _ = BlockScopeStack.Using(node);

        while (Token.Type != TokenType.RBrace && Token.Type != TokenType.EOF) {
            var statement = ParseStatement();
            node.Add(statement);

            OnNode?.Invoke(statement);
        }

        var end = EnsureAndConsume(TokenType.RBrace, "Expected closing brace");

        node.EndToken = end;

        return node;
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

    public TypeParametersList ParseTypeParameters() {
        var start = EnsureAndConsume(TokenType.LAngle, "Expected '<' before type parameters");

        var args = new TypeParametersList() {
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
                Expected("Expected comma or closing angle bracket");
                return null;
            }
        }

        EnsureAndConsume(TokenType.RAngle, "Expected closing angle bracket");

        args.EndToken = Prev;

        return args;
    }

    public ExpressionListNode ParseArgumentsList() {
        var start = EnsureAndConsume(TokenType.LParen, "Expected opening parenthesis");

        var args = new ExpressionListNode() {
            StartToken = start,
        };

        while (!Token.Is(TokenType.RParen | TokenType.EOF)) {
            args.Add(ParseExpression());

            if (Token.IsComma) {
                Advance();
            } else if (!Token.IsRParen) {
                Expected("Expected comma or closing parenthesis");
                return null;
            }
        }

        EnsureAndConsume(TokenType.RParen, "Expected closing parenthesis");

        args.EndToken = Prev;

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
                arg.SetName(ParseIdentifier("Expected a parameter name"));
            } else {
                arg.SetType(ParseTypeIdentifier("Expected a type name identifier"));
                arg.SetName(ParseIdentifier("Expected a parameter name"));

                arg.StartToken = arg.TypeIdentifier.StartToken;
                arg.EndToken   = arg.Name.EndToken;
            }

            args.Add(arg);

            if (Token.IsComma)
                Advance();
            else if (!Token.IsRParen)
                Expected("Expected comma or closing parenthesis");
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

        AdvanceIfSemicolon();

        assignment.EndToken = Prev;

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
    }

    private Expression ParseComparison() {
        var left = ParseTerm();

        while (
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

        while (Token.IsOrOperator || Token.IsOrKeyword || Token.IsPipeOperator) {
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

        while (Token.IsAndOperator || Token.IsBitwiseAndOperator) {
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
            {IsNull   : true} => ParseNull(),
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
                    [() => Token.IsRParen, () => Token.IsArrow, /*() => Token.IsLBrace*/],
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
            ExpectedBuilder($"Lexer Error; unexpected token: {Token} {Token.ErrorMessage}")
               .Range(Token)
               .Report();
        }

        Unexpected();

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
            
            var caseNode = new MatchCaseNode() {
                StartToken = caseStart,
                EndToken   = Token,
                Pattern    = caseExpr,
            };
            
            if (Token.IsArrow) {
                EnsureAndConsume(TokenType.Arrow, "Expected '=>' after case expression");
                caseNode.Body = ParseExpression();
            }
            else if (Token.IsLBrace) {
                caseNode.Body = new BlockExpressionWrapper(ParseBlock());
            } else {
                Expected("Expected '=>' or opening brace after case expression");
            }

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
            var type  = ParseTypeIdentifier("Expected type name");

            return new TypePatternNode(type) {
                StartToken = start,
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
                    Expected("Invalid pattern match expression");
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

        Expected("Invalid pattern match expression");

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
                Expected("Expected comma or closing parenthesis");
        }

        var end = EnsureAndConsume(TokenType.RParen, "Expected closing parenthesis");

        args.StartToken = start;
        args.EndToken   = end;

        return args;
    }

    public ExpressionList ParseExpressionList() {
        var start = EnsureAndConsume(TokenType.LParen, "Expected opening parenthesis");

        var args = new ExpressionList {StartToken = start};

        while (!Token.IsRParen || Token.IsEOF) {
            var expr = ParseExpression();
            args.Add(expr);

            if (Token.IsComma)
                Advance();
            else if (!Token.IsRParen)
                Expected("Expected comma or closing parenthesis");
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
        bool IsFunctionCall() => IsFnCallLike(skipIdentifier: true).isRegularFunction;


        while ((IsMemberAccess() || IsIndexAccess() || IsFunctionCall()) && !Token.IsEOF) {
            // variable.x
            if (IsMemberAccess()) {
                var start = EnsureAndConsume(TokenType.Dot, "Expected '.' after variable name `variable*.*property`");

                var prop = ParseIdentifier("Expected property name after '.' `variable.*property*`");

                left = new MemberAccessExpression(left, prop) {
                    StartToken = left.StartToken,
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
                var callExpr = ParseFunctionCall(left);
                callExpr.StartToken = left.StartToken;
                callExpr.EndToken   = Prev;

                left = callExpr;
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

    private TypeIdentifierExpression ParseTypeIdentifier(string reason = "Expected identifier") {
        var identToken = EnsureAndConsume(TokenType.Identifier, reason);

        var ident = new TypeIdentifierExpression(identToken.Value) {
            StartToken = identToken,
            EndToken   = identToken,
        };

        if (Token.IsLAngle) {
            ident.TypeParameters = ParseTypeParameters();
        }

        ident.EndToken = Prev;

        return ident;
    }

    private ObjectLiteralExpression ParseObjectLiteral() {
        var start = EnsureAndConsume(TokenType.LBrace, "Expected opening brace");

        var obj = new ObjectLiteralExpression() {
            StartToken = start,
        };

        while (!Token.Is(TokenType.RBrace | TokenType.EOF)) {
            var key = EnsureAnyOfAndConsume(
                TokenType.Identifier | TokenType.String | TokenType.Int32 | TokenType.Int64,
                "Expected identifier as key"
            );
            EnsureAndConsume(TokenType.Colon, "Expected colon after key");

            var value = ParseExpression();

            var prop = obj.AddProperty(key.Value, value);
            prop.StartToken = key;
            prop.EndToken   = value.EndToken;

            if (Token.IsComma) {
                Advance();
            } else if (!Token.IsRBrace) {
                Expected("Expected comma or closing brace");
                return null;
            }
        }

        EnsureAndConsume(TokenType.RBrace, "Expected closing brace");

        obj.EndToken = Prev;
        
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
                Expected("Expected expression in array literal");
                return null;
            }

            array.Elements.Add(expr);

            if (Token.IsComma) {
                Advance();
            } else if (!Token.IsRBracket) {
                Expected("Expected comma or closing bracket");
                return null;
            }
        }

        var end = EnsureAndConsume(TokenType.RBracket, "Expected closing bracket");

        array.EndToken = end;

        return array;
    }
}