using System.Runtime.CompilerServices;
using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.Interpreter.Modules;
using CSScriptingLang.Lexing;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.Utils;
using Engine.Engine.Logging;

namespace CSScriptingLang.Parsing;

public class NodeScopeStack<T> : Stack<T> where T : BaseNode
{
    public T Current => Count > 0 ? Peek() : default;

    public UsingCallbackHandle Using(T node) {
        Push(node);
        return new UsingCallbackHandle(() => Pop());
    }
}

public enum ParserMode
{
    Default,
    PatternMatching,
}

public class ParserBase
{
    protected Logger Logger = Logs.Get<StandaloneParser>(LogLevel.Warning);

    public Script Script { get; set; }

    public LexerTokenStream Lexer;

    public Stack<ParserMode> ModeStack { get; } = new();
    public ParserMode        Mode      => ModeStack.Count > 0 ? ModeStack.Peek() : ParserMode.Default;

    protected int   Cursor   => Lexer.Cursor;
    protected Token Token    => Lexer.Current;
    protected Token Next     => Lexer.Next;
    protected Token NextNext => Lexer.NextNext;
    protected Token Prev     => Lexer.Prev;
    protected Token PrevPrev => Lexer.PrevPrev;

    protected Token Advance(int n = 1) {
        var prev = Token;
        Lexer.Advance(n);
        return prev;
    }
    protected Token AdvanceIfSemicolon() {
        if (Token.IsSemicolon) {
            return Advance();
        }

        return Token;
    }

    protected bool Sequence(params TokenType[] types) {
        for (var i = 0; i < types.Length; i++) {
            var tok = Lexer.Peek(i);

            if (!tok.Is(types[i])) {
                return false;
            }
        }

        return true;
    }

    protected TNodeType ParseLiteral<TNodeType, TValueType>(string name, TokenType type, Func<Token, TValueType> Converter) where TNodeType : LiteralValueExpression {
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

    protected StringExpression  ParseString()  => ParseLiteral<StringExpression, string>("string", TokenType.String, t => t.Value);
    protected BooleanExpression ParseBoolean() => ParseLiteral<BooleanExpression, bool>("boolean", TokenType.Boolean, t => bool.Parse(t.Value));
    protected LiteralNumberExpression ParseNumber() {
        var node = LiteralNumberExpression.CreateFromToken(Token);
        node.StartToken = Token;
        node.EndToken   = Token;
        Advance();
        return node;
    }

    protected ModuleDeclarationNode ParseModuleDeclaration() {
        var start = EnsureAndConsume(Keyword.Module, "Expected 'module' keyword");
        var name  = ParseString();

        AdvanceIfSemicolon();

        return new ModuleDeclarationNode() {
            StartToken = start,
            EndToken   = Token,
            Name       = name,
        };
    }
    /// <summary>
    /// Parse a list of imports:
    /// <code>
    /// import "module";
    /// import "a.b.c";
    /// </code>
    /// </summary>
    /// <returns></returns>
    protected ImportStatementsNode ParseImportStatements() {
        var importsList = new ImportStatementsNode() {
            StartToken = Token,
        };

        while (Token.IsImportKeyword && !Next.IsEOF) {
            var start = EnsureAndConsume(Keyword.Import, "Expected 'import' keyword");
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

    protected UsingCallbackHandle UsingMode(ParserMode mode) {
        ModeStack.Push(mode);
        return new UsingCallbackHandle(() => ModeStack.Pop());
    }

    protected void RawLogError(
        string     message,
        string     file      = "",      int        line    = 0, string member = "",
        TokenRange fromRange = default, TokenRange toRange = default
    ) {
        ErrorWriter.Create(Script, file, line, member)
           .SetSourceIfNull(Lexer.GetInput())
           .LogFatal(message, fromRange, toRange);
    }

    protected void LogError(string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "") {
        RawLogError(message, file, line, member, Token.Range, Next.Range);
    }
    protected void LogError(string message, Token fromToken, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "") {
        RawLogError(message, file, line, member, fromToken.Range, Next.Range);
    }
    protected void LogError(string message, Token fromToken, Token toToken, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "") {
        RawLogError(message, file, line, member, fromToken.Range, toToken.Range);
    }

    protected bool Ensure(TokenType type, string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "") {
        if (Token.Is(type))
            return true;

        RawLogError($"{message}; got {Token}", file, line, member, Token.Range, Next.Range);

        return false;
    }

    protected Token EnsureAnyOfAndConsume(TokenType type, string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "") {
        var curToken = Token;
        foreach (var tokenType in type.GetFlags()) {
            if (Token.Is(tokenType)) {
                Advance();
                return curToken;
            }
        }

        throw new ParserException($"{message}; got {Token}", Token.Range, Next.Range, Script)
           .WithCaller(Caller.FromAttributes(file, line, member))
           .WithInput(Lexer.GetInput());
    }
    protected Token EnsureAndConsume(TokenType type, string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "") {
        var curToken = Token;
        if (Token.Is(type))
            Advance();
        else {
            throw new ParserException($"{message}; got {Token}", Token.Range, Next.Range, Script)
               .WithCaller(Caller.FromAttributes(file, line, member))
               .WithInput(Lexer.GetInput());
        }
        // RawLogError($"{message}; got {Token}", file, line, member);

        return curToken;
    }
    
    protected Token EnsureAndConsume(Keyword type, string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "") {
        var curToken = Token;
        if (Token.Is(type))
            Advance();
        else {
            throw new ParserException($"{message}; got {Token}", Token.Range, Next.Range, Script)
               .WithCaller(Caller.FromAttributes(file, line, member))
               .WithInput(Lexer.GetInput());
        }
        // RawLogError($"{message}; got {Token}", file, line, member);

        return curToken;
    }

    public struct ParserLookAhead
    {
        public enum LookAheadType
        {
            MatchFirst,
            MatchAllSequential,
            MatchAllAnyOrder,
        }


        private ParserBase Parser         { get; set; }
        private int        MaxLookAhead   { get; set; } = 100;
        private int        StartingCursor { get; set; }

        public int Steps => Parser.Lexer.Cursor - StartingCursor;

        public LookAheadType Type { get; set; } = LookAheadType.MatchAllSequential;

        public List<Token> Tokens { get; set; } = new();

        public Func<bool>[] MatchPredicates { get; set; }
        public Func<bool>[] BreakPredicates { get; set; }

        public ParserLookAhead(
            ParserBase    parser,
            LookAheadType type,
            Func<bool>[]  predicates,
            Func<bool>[]  breakPredicates
        ) {
            Parser          = parser;
            Type            = type;
            StartingCursor  = parser.Lexer.Cursor;
            MatchPredicates = predicates;
            BreakPredicates = breakPredicates;
        }

        public ParserLookAhead SetStartPosition(Action action) {
            action();
            return this;
        }

        public ParserLookAhead WithMaxLookAhead(int max) {
            MaxLookAhead = max;
            return this;
        }

        public bool Execute(
            Action<ParserLookAhead, bool> onAnyResult = null,
            Action<ParserLookAhead>       onMatch     = null,
            Action<ParserLookAhead>       onFail      = null
        ) {

            var matchAnyPredicates = new bool[MatchPredicates.Length];
            var matchSequence      = new Queue<Func<bool>>(MatchPredicates);
            var predicateIdx       = 0;

            while (
                !Parser.Token.IsEOF
             && Steps < MaxLookAhead
            ) {

                Tokens.Add(Parser.Token);

                Parser.Advance();

                switch (Type) {
                    case LookAheadType.MatchFirst: {
                        if (MatchPredicates[0]()) {
                            onAnyResult?.Invoke(this, true);
                            onMatch?.Invoke(this);
                            return true;
                        }

                        break;
                    }

                    case LookAheadType.MatchAllSequential: {

                        if (matchSequence.Count == 0) {
                            onAnyResult?.Invoke(this, true);
                            onMatch?.Invoke(this);
                            return true;
                        }

                        if (matchSequence.Peek()()) {
                            matchSequence.Dequeue();
                            predicateIdx++;
                        } else {
                            if (predicateIdx > 0) {
                                onAnyResult?.Invoke(this, false);
                                onFail?.Invoke(this);
                                return false;
                            }
                        }

                        if (matchSequence.Count == 0) {
                            onAnyResult?.Invoke(this, true);
                            onMatch?.Invoke(this);
                            return true;
                        }


                        break;
                    }

                    case LookAheadType.MatchAllAnyOrder: {
                        for (var i = 0; i < MatchPredicates.Length; i++) {
                            if (matchAnyPredicates[i]) {
                                continue;
                            }

                            if (MatchPredicates[i]()) {
                                matchAnyPredicates[i] = true;
                            }
                        }

                        if (matchAnyPredicates.All(x => x)) {
                            onAnyResult?.Invoke(this, true);
                            onMatch?.Invoke(this);
                            return true;
                        }

                        break;
                    }

                }

                if (BreakPredicates != null) {
                    if (BreakPredicates.Any(x => x())) {
                        onAnyResult?.Invoke(this, false);
                        onFail?.Invoke(this);
                        return false;
                    }
                }
            }

            onAnyResult?.Invoke(this, false);
            onFail?.Invoke(this);

            return false;
        }

        public static implicit operator bool(ParserLookAhead la) => la.Execute();

        public void Rollback() {
            Parser.Lexer.Rewind(Steps);
        }
    }

    protected ParserLookAhead LookAheadFirst(Func<bool>[] predicate, Func<bool>[] breakPredicate = null) {
        return new ParserLookAhead(this, ParserLookAhead.LookAheadType.MatchFirst, predicate, breakPredicate);
    }

    protected ParserLookAhead LookAheadSequential(Func<bool>[] predicates, Func<bool>[] breakPredicates = null) {
        return new ParserLookAhead(this, ParserLookAhead.LookAheadType.MatchAllSequential, predicates, breakPredicates);
    }

    protected ParserLookAhead LookAheadAnyOrder(Func<bool>[] predicate, Func<bool>[] breakPredicate = null) {
        return new ParserLookAhead(this, ParserLookAhead.LookAheadType.MatchAllAnyOrder, predicate, breakPredicate);
    }
}