using System.Runtime.CompilerServices;
using CSScriptingLang.Interpreter.Modules;
using CSScriptingLang.Lexing;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.Utils;

namespace CSScriptingLang.Parsing;

public class NodeScopeStack<T> : Stack<T> where T : BaseNode
{
    public T Current => Count > 0 ? Peek() : default;

    public UsingCallbackHandle Using(T node) {
        Push(node);
        return new UsingCallbackHandle(() => Pop());
    }
}

public partial class Parser
{
    private Lexer       Lexer;
    public  LexerCursor Cursor => Lexer.Cursor;
    public  Script      Script { get; set; }

    public ProgramNode Program = new();

    private NodeScopeStack<InlineFunctionDeclarationNode> functionScopeStack = new();
    private NodeScopeStack<BlockNode>                     blockScopeStack    = new();

    public Parser(Script script) {
        Script = script;
        Lexer  = new Lexer(Script.Source);

        Advance();

        // using (ScopeTimer.NewWith($"Parse Script: {Script.FilePath}, Module: {Script.Module.Name}")) {
        Parse();
        // }

    }

    public Parser(Lexer lexer) {
        Lexer = lexer;

        Advance();

        using (ScopeTimer.New()) {
            Parse();
        }
    }

    public void Parse() {
        ParseProgram();

        var linker = new ASTParentLinker();
        linker.VisitProgramNode(Program);
    }

    private void ParseProgram() {
        Program.StartToken = Token;

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
    }

    private BaseNode ParseStatement() {
        // using var _ = ScopeTimer.NewPrefixed(CurrentToken.ToString());
        BaseNode node = (Token, Next) switch {
            ({IsVarKeyword     : true}, { })              => ParseVariableDeclaration(),
            ({IsForKeyword     : true}, { })              => ParseForLoop(),
            ({IsIfKeyword      : true}, { })              => ParseIfStatement(),
            ({IsReturnKeyword  : true}, { })              => ParseReturnStatement(),
            ({IsDeferKeyword   : true}, { })              => ParseDeferStatement(),
            ({IsIdentifier     : true}, {IsLParen: true}) => ParseFunctionCall(),
            ({IsFunctionKeyword: true}, { })              => ParseFunctionDeclaration(),

            _ => ParseExpression()
        };

        if (Token.IsSemicolon) {
            Advance();
        }

        return node;
    }

    /// <summary>
    /// Parse a list of imports:
    /// <code>
    /// import "module";
    /// import "a.b.c";
    /// </code>
    /// </summary>
    /// <returns></returns>
    private ImportStatementsNode ParseImportStatements() {
        var importsList = new ImportStatementsNode() {
            StartToken = Token,
        };

        while (Token.IsImportKeyword && !Next.IsEOF) {
            var start = EnsureAndConsume(TokenType.Import, "Expected 'import' keyword");
            var path  = ParseString();
            var end   = EnsureAndConsume(TokenType.Semicolon, "Expected semicolon after import statement");

            importsList.Add(new ImportStatementNode(path) {
                StartToken = start,
                EndToken   = end,
            });
        }

        importsList.EndToken = Token;

        return importsList;
    }

    private BaseNode ParseReturnStatement() {
        var start = EnsureAndConsume(TokenType.Return, "Expected 'return' keyword");

        BaseNode value = null;
        if (!Token.IsSemicolon) {
            value = ParseExpression();
        }

        if (Token.IsSemicolon) {
            Advance();
        }

        var node = new ReturnStatementNode(value) {
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
        EnsureAndConsume(TokenType.If, "Expected 'if' keyword");
        // Expect a '(' for the condition & consume it
        EnsureAndConsume(TokenType.LParen, "Expected '(' after 'if'");

        // Parse the condition expression
        var condition = ParseExpression();

        // Expect a ')' after the condition & consume it
        EnsureAndConsume(TokenType.RParen, "Expected ')' after condition");

        // Parse the 'then' branch, which is a block statement
        var thenBranch = ParseBlock();

        // Check for an 'else' part
        BlockNode elseBranch = null;
        if (Token.IsElseKeyword) {
            Advance();                 // Consume 'else'
            elseBranch = ParseBlock(); // Parse the else block
        }

        return new IfStatementNode(condition, thenBranch, elseBranch);
    }

    private BaseNode ParseForLoop() {
        var start = EnsureAndConsume(TokenType.For, "Expected 'for' keyword");
        EnsureAndConsume(TokenType.LParen, "Expected '(' after 'for'");

        // Parse the initialization part
        BaseNode  initialization = null;
        BaseNode  condition      = null;
        BaseNode  iteration      = null;

        var hasInit = false;

        // We can also skip the var if it's defined outside the loop init
        // var i = 0; for(; i < 10; i++) { }
        if (Token.IsSemicolon) {
            Advance();
        } else {
            // var i = 0;
            initialization = ParseExpression();
        }

        if (initialization != null) {
            if (
                initialization is VariableDeclarationNode varDecl &&
                varDecl.Assignment?.Value is RangeNode r
            ) {
                var forRange = new ForRangeNode() {
                    StartToken = start,
                };

                // var indexVar = varDecl.Assignment.VariableName;
                if (varDecl.Assignment.Variable is TupleListDeclarationNode tuple) {
                    if (tuple.Nodes.Count == 0) {
                        LogError("Expected at least one variable in tuple");
                    }

                    // indexVar = ((VariableNode) tuple.Nodes[0]).Name;

                    foreach (var el in tuple) {
                        forRange.Indexers.Add(new VariableDeclarationNode(new AssignmentNode(((VariableNode) el).Name, null)));
                    }
                } else {
                    forRange.Indexers = new TupleListDeclarationNode();
                    forRange.Indexers.Add(new VariableDeclarationNode(new AssignmentNode(varDecl.Assignment.VariableName, null)));

                    // indexVar = varDecl.Assignment.VariableName;
                }

                EnsureAndConsume(TokenType.RParen, "Expected ')' after iteration");

                forRange.Range    = r;
                forRange.Body     = ParseBlock();
                forRange.EndToken = Token;

                return forRange;


                /*
                var init = new VariableDeclarationNode(new AssignmentNode(varDecl.Assignment.VariableName, new NumberNode(0)));
                var cond = new BinaryOperationNode(new VariableNode(varDecl.Assignment.VariableName), OperatorType.LessThan, r.Expression);
                var iter = new UnaryOperationNode(OperatorType.Increment, new VariableNode(varDecl.Assignment.VariableName));

                range = r;

                initialization = init;
                condition      = cond;
                iteration      = iter;
                */

            }

            EnsureAndConsume(TokenType.Semicolon, "Expected ';' after initialization");

            hasInit = true;
        }


        // Parse the condition part
        condition = ParseExpression();
        EnsureAndConsume(TokenType.Semicolon, "Expected ';' after condition");


        if (!hasInit && condition.Cursor.First.Child<VariableNode>(out var variable)) {
            initialization = new VariableNode(variable.Name);
        }

        // Parse the iteration part
        iteration = ParseExpression();

        EnsureAndConsume(TokenType.RParen, "Expected ')' after iteration");

        // Parse the body of the loop
        var body = ParseBlock();

        return new ForLoopNode(initialization, condition, iteration, body);
    }


    private InlineFunctionDeclarationNode ParseInlineFunctionDeclaration() {
        var start = EnsureAndConsume(TokenType.Function, "Expected 'function' keyword");

        var fn = new InlineFunctionDeclarationNode() {
            StartToken = start,
        };

        functionScopeStack.Push(fn);

        fn.Parameters = ParseArgumentListDeclaration();
        fn.Body       = ParseBlock();

        fn.EndToken = Token;

        if (!fn.HasReturnStatementDefined) {
            fn.Body.Add(new ReturnStatementNode(null));
        }

        functionScopeStack.Pop();

        return fn;
    }

    private FunctionDeclarationNode ParseFunctionDeclaration() {
        var start = EnsureAndConsume(TokenType.Function, "Expected 'function' keyword");

        var functionName = Token.Value;

        var fn = new FunctionDeclarationNode(functionName);
        functionScopeStack.Push(fn);

        EnsureAndConsume(TokenType.Identifier, "Expected function name");

        fn.Parameters = ParseArgumentListDeclaration();

        if (Token.Type != TokenType.LBrace) {
            LogError("Expected opening brace");
            functionScopeStack.Pop();
            return null;
        }

        fn.Body       = ParseBlock();
        fn.StartToken = start;
        fn.EndToken   = Token;

        if (!fn.HasReturnStatementDefined) {
            fn.Body.Add(new ReturnStatementNode(null));
        }

        functionScopeStack.Pop();

        return fn;
    }


    private BlockNode ParseBlock() {
        var start = EnsureAndConsume(TokenType.LBrace, "Expected opening brace");

        var node = new BlockNode {
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

    private bool IsFnCallLike()     => Token.IsIdentifier && Next.IsLParen;
    private bool IsLambdaDeclLike() => Token.IsFunctionKeyword && Next.IsLParen;

    private DeferStatementNode ParseDeferStatement() {
        var start = EnsureAndConsume(TokenType.Defer, "Expected 'defer' keyword");

        BaseNode node = null;
        if (IsLambdaDeclLike()) {
            node = ParseInlineFunctionDeclaration();
            var args = ParseArgumentsList();
            node = new FunctionCallNode(node, args);
        } else {
            node = ParseFunctionCall();
        }

        var defer = new DeferStatementNode(node) {
            StartToken = start,
            EndToken   = Token,
        };

        if (blockScopeStack.Count > 0) {
            blockScopeStack.Current.DeferStatements.Add(defer);
        }

        return defer;
    }

    private FunctionCallNode ParseFunctionCall() {
        if (!IsFnCallLike()) {
            LogError("Expected function call");
            return null;
        }

        var startToken = Token;

        var fnName = Token.Value;
        Advance(); // consume identifier

        var args = ParseArgumentsList();

        return new FunctionCallNode(fnName, args) {
            StartToken = startToken,
            EndToken   = Token,
        };
    }


    private ExpressionListNode ParseArgumentsList() {
        if (Token.Type != TokenType.LParen) {
            LogError("Expected opening parenthesis");
            return null;
        }

        Advance(); // consume '('

        var args = new ExpressionListNode();

        while (!Token.Is(TokenType.RParen | TokenType.EOF)) {
            args.Add(ParseExpression());

            if (Token.IsComma) {
                Advance();
            }
        }

        Advance(); // consume ')'

        return args;
    }


    private ArgumentListDeclarationNode ParseArgumentListDeclaration() {
        var start = EnsureAndConsume(TokenType.LParen, "Expected opening parenthesis");

        var args = new ArgumentListDeclarationNode();

        while (!Token.IsRParen || Token.IsEOF) {
            var paramType = EnsureAndConsume(TokenType.Identifier, "Expected a type name identifier");
            var paramName = EnsureAndConsume(TokenType.Identifier, "Expected a parameter name");

            args.Add(new(paramName.Value, paramType.Value));

            if (Token.IsComma)
                Advance();

        }

        var end = EnsureAndConsume(TokenType.RParen, "Expected closing parenthesis");

        args.StartToken = start;
        args.EndToken   = end;

        return args;
    }


    private BaseNode ParseVariableDeclaration() {
        Advance(); // consume 'var'

        var node = ParseAssignment();

        // if (Token.IsSemicolon)
        // Advance();

        // using var _ = ScopeTimer.NewPrefixed(CurrentToken.ToString());

        if (node is AssignmentNode assignment) {
            return new VariableDeclarationNode(assignment);
        }

        return node;
    }

    private BaseNode ParseRangeExpression() {
        var start = EnsureAndConsume(TokenType.Range, "Expected 'range' keyword");

        var expr = ParseExpression();

        return new RangeNode(expr) {
            StartToken = start,
            EndToken   = Token,
        };
    }

    private BaseNode ParseExpression() {
        if (Token.IsVarKeyword)
            return ParseVariableDeclaration();
        if (Token.IsRangeKeyword)
            return ParseRangeExpression();

        // return ParseLogicalOr();
        return ParseAssignment();
    }


    private BaseNode ParseAssignment() {
        // using var _ = ScopeTimer.NewPrefixed(CurrentToken.ToString());

        var isIdent = Token.IsIdentifier;
        var isOp    = Next.IsOperator;
        var isEq    = Next.Op == OperatorType.Assignment;

        if (isIdent && isOp && isEq) {
            var variableName = Token.Value;
            Advance(); // consume identifier
            Advance(); // consume '='

            var value = ParseExpression();
            return new AssignmentNode(variableName, value);
        }

        var node = ParseLogicalOr();

        if (Token.IsOp(OperatorType.Assignment)) {
            var op = Token.Op;
            Advance();
            var right = ParseExpression();
            node = new AssignmentNode(node, right);
        }

        return node;
    }


    private BaseNode ParseTerm() {
        var node = ParseFactor();

        while (Token.IsOp(
                   OperatorType.Plus, OperatorType.Minus,
                   OperatorType.PlusEquals, OperatorType.MinusEquals
               )) {
            var operatorToken = Token;
            Advance();
            var right = ParseFactor();
            node = new BinaryOperationNode(node, operatorToken.Op, right);
        }

        return node;
        // return ParseComparison(node); // Move to comparison if applicable
    }

    // private BaseNode ParseComparison(BaseNode left) {
    private BaseNode ParseComparison() {
        var left = ParseTerm();

        while (
            Token.IsOp(
                OperatorType.Equals, OperatorType.NotEquals, OperatorType.GreaterThan,
                OperatorType.LessThan, OperatorType.GreaterThanOrEqual, OperatorType.LessThanOrEqual
            )
        ) {
            var operatorToken = Token;
            Advance();
            var right = ParseTerm(); // Parse the right side of the comparison
            left = new BinaryOperationNode(left, operatorToken.Op, right);
        }

        return left;
    }

    private BaseNode ParseFactor() {
        var node = ParsePrimary();

        while (Token.IsOp(OperatorType.Multiply, OperatorType.Divide, OperatorType.Modulus)) {
            var operatorToken = Token;
            Advance();
            var right = ParsePrimary();
            node = new BinaryOperationNode(node, operatorToken.Op, right);
        }

        return node;
    }

    private BaseNode ParseLogicalOr() {
        var left = ParseLogicalAnd(); // Logical AND has higher precedence, parse it first

        while (Token.IsOp(OperatorType.Or)) {
            var operatorToken = Token;
            Advance();
            var right = ParseLogicalAnd();                                 // Parse the right-hand side
            left = new BinaryOperationNode(left, operatorToken.Op, right); // Combine into a BinaryOperationNode
        }

        return left;
    }

    private BaseNode ParseLogicalAnd() {
        var left = ParseComparison(); // Comparison has higher precedence, parse it first

        while (Token.IsOp(OperatorType.And)) {
            var operatorToken = Token;
            Advance();
            var right = ParseComparison();                                 // Parse the right-hand side
            left = new BinaryOperationNode(left, operatorToken.Op, right); // Combine into a BinaryOperationNode
        }

        return left;
    }

    private TNodeType ParseLiteral<TNodeType, TValueType>(string name, TokenType type, Func<Token, TValueType> Converter) where TNodeType : LiteralValueNode {
        var start = EnsureAndConsume(type, $"Expected {name} literal");
        var value = Converter(start);
        var node  = (TNodeType) Activator.CreateInstance(typeof(TNodeType), value);
        if (node != null) {
            node.StartToken = start;
            node.EndToken   = Token;
            return node;
        }

        throw new Exception("Failed to create node");
    }

    private StringNode  ParseString()  => ParseLiteral<StringNode, string>("string", TokenType.String, t => t.Value);
    private BooleanNode ParseBoolean() => ParseLiteral<BooleanNode, bool>("boolean", TokenType.Boolean, t => bool.Parse(t.Value));
    private LiteralNumberNode ParseNumber() {
        var node = LiteralNumberNode.CreateFromToken(Token);
        node.StartToken = Token;
        node.EndToken   = Token;
        Advance();
        return node;
    }

    private BaseNode ParsePrimary() {

        if (Token.IsLBracket) {
            return ParseArrayLiteral();
        }

        BaseNode valueNode = Token switch {
            {IsString : true} => ParseString(),
            {IsBoolean: true} => ParseBoolean(),
            {IsNumber : true} => ParseNumber(),
            _                 => null
        };
        if (valueNode != null) {
            return valueNode;
        }

        if (IsLambdaDeclLike()) {
            return ParseInlineFunctionDeclaration();
        }

        if (IsFnCallLike()) {
            return ParseFunctionCall();
        }

        if (Token.IsIdentifier) {
            var name = Token.Value;
            Advance();

            BaseNode variableNode = new VariableNode(name);

            variableNode = TryParseMemberAccess((VariableNode) variableNode);

            /*if (Token.IsDot) {

                BaseNode node = variableNode;

                while (Token is {IsDot: true, IsEOF: false}) {
                    Advance(); // consume dot
                    var prop = EnsureAndConsume(TokenType.Identifier, "Expected property name after dot");
                    node = new PropertyAccessNode(node, prop.Value);
                }

                return node;
            }*/

            var op = Token.Op;

            // Check for postfix increment or decrement
            if (Token.IsOp(OperatorType.Increment)) {
                Advance();
                return new UnaryOperationNode(op, variableNode, true);
            }

            if (Token.IsOp(OperatorType.PlusEquals)) {
                Advance();
                return new BinaryOperationNode(variableNode, op, ParseExpression());
            }

            if (Token.IsOp(OperatorType.Decrement)) {
                Advance();
                return new UnaryOperationNode(op, variableNode, true);
            }

            if (Token.IsOp(OperatorType.MinusEquals)) {
                Advance();
                return new BinaryOperationNode(variableNode, op, ParseExpression());
            }

            return variableNode;
        }

        if (Token.IsOp(OperatorType.Increment) || Token.IsOp(OperatorType.Decrement)) {
            var operatorToken = Token.Op;
            Advance();
            var variableNode = ParsePrimary();                          // Expect an identifier after the increment/decrement
            return new UnaryOperationNode(operatorToken, variableNode); // Prefix increment/decrement
        }

        if (Token.IsLParen) {
            if (Sequence(TokenType.LParen, TokenType.Identifier, TokenType.Comma)) {
                return ParseTupleList();
            }

            Advance(); // consume '('
            var expression = ParseExpression();
            EnsureAndConsume(TokenType.RParen, "Expected closing parenthesis"); // consume ')'
            return expression;
        }

        if (Token.IsLBrace) {
            return ParseObjectLiteral();
        }

        LogError("Unexpected token: " + Token);

        return null;
    }
    private TupleListDeclarationNode ParseTupleList() {
        var start = EnsureAndConsume(TokenType.LParen, "Expected opening parenthesis");

        var args = new TupleListDeclarationNode();

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

    private BaseNode TryParseMemberAccess(VariableNode variable) {
        var      lastName = variable.Name;
        BaseNode left     = variable;

        while ((Token.IsDot || Token.IsLBracket || Token.IsLParen) && !Token.IsEOF) {
            // variable.x
            if (Token.IsDot) {
                EnsureAndConsume(TokenType.Dot, "Expected '.' after variable name `variable*.*property`");

                var prop = EnsureAndConsume(TokenType.Identifier, "Expected property name after '.' `variable.*property*`");
                lastName = prop.Value;
                left     = new PropertyAccessNode(left, prop.Value);
            }

            // variable[x] | variable['x'] | variable[0..9] | variable[expression]
            if (Token.IsLBracket) {
                EnsureAndConsume(TokenType.LBracket, "Expected opening bracket after variable name `variable*[*`");

                var index = ParseExpression();

                EnsureAndConsume(TokenType.RBracket, "Expected closing bracket after index expression `variable[index*]*`");

                left = new IndexAccessNode(left, index);
            }

            // variable.fn() | variable['fn']() | variable[0..9]() | variable[expression]()
            if (Token.IsLParen) {
                var args = ParseArgumentsList();
                left = new FunctionCallNode(left, args);
            }
        }

        return left;
    }

    private ObjectLiteralNode ParseObjectLiteral() {
        EnsureAndConsume(TokenType.LBrace, "Expected opening brace");

        var obj = new ObjectLiteralNode();

        while (!Token.Is(TokenType.RBrace | TokenType.EOF)) {
            /*if (Sequence(TokenType.Function, TokenType.Identifier, TokenType.LParen)) {
                var fn = ParseFunctionDeclaration();
                obj.Properties.Add(fn.Name, fn);
            } else {

            }*/

            var key = EnsureAndConsume(TokenType.Identifier, "Expected identifier as key");
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

    private ArrayLiteralNode ParseArrayLiteral() {
        var start = EnsureAndConsume(TokenType.LBracket, "Expected opening bracket");

        var array = new ArrayLiteralNode() {
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

    private void RawLogError(string message, string file = "", int line = 0, string member = "") {
        ErrorWriter.CallerFilePath = file;
        ErrorWriter.CallerLine     = line;
        ErrorWriter.CallerMethod   = member;

        Lexer.ErrorWriter.LogFatal(message, Token.Range, Next.Range);
    }
    private void LogError(string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "") {
        RawLogError(message, file, line, member);
    }

    private bool Ensure(TokenType type, string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "") {
        if (Token.Is(type))
            return true;

        RawLogError($"{message}; got {Token}", file, line, member);

        return false;
    }

    private Token EnsureAndConsume(TokenType type, string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "") {
        var curToken = Token;
        if (Token.Is(type))
            Advance();
        else
            RawLogError($"{message}; got {Token}", file, line, member);

        return curToken;
    }
}