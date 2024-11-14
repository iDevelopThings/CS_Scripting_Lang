using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.Interpreter.Execution.Statements;
using CSScriptingLang.Interpreter.Modules;
using CSScriptingLang.Lexing;
using CSScriptingLang.Core.Logging;

namespace CSScriptingLang.Parsing;

public partial class SubParser
{
    protected Logger Logger = Logs.Get<SubParser>(LogLevel.Warning);

    protected SubParser ParentParser { get; set; }

    protected Parser Parser => ParentParser is Parser parser ? parser : throw new NullReferenceException("ParentParser is null");

    private Script _script;
    public Script Script {
        get => _script ?? ParentParser?.Script;
        set => _script = value;
    }

    private LexerTokenStream _lexer;
    public LexerTokenStream Lexer {
        get => _lexer ?? ParentParser?.Lexer ?? throw new NullReferenceException("Lexer is null");
        set => _lexer = value;
    }

    private NodeScopeStack<BlockExpression> _blockScopeStack = new();
    public NodeScopeStack<BlockExpression> BlockScopeStack {
        get => _blockScopeStack ?? ParentParser?.BlockScopeStack ?? throw new NullReferenceException("BlockScopeStack is null");
        set => _blockScopeStack = value;
    }

    public int   Cursor   => Lexer.Cursor;
    public Token Token    => Lexer.Current;
    public Token Next     => Lexer.Next;
    public Token NextNext => Lexer.NextNext;
    public Token Prev     => Lexer.Prev;
    public Token PrevPrev => Lexer.PrevPrev;

    public ParserCollection Parsers { get; set; }

    public SubParser() {
        Parsers = new ParserCollection(this);
    }

    public virtual void SetParent(SubParser parent) {
        ParentParser = parent;
    }

    public Expression ParseExpression(int precedence = 0) {
        if (!TryParseExpression(out var expression, precedence)) {
            Expected("Failed to parse expression");
        }

        return expression;
    }
    public bool TryParseExpression(out Expression expression, int precedence = 0) {
        expression = null;

        if (!Parsers.TryGetPrefixParser(this, out var prefixParser)) {
            Diagnostic_Error_Fatal($"Failed to get prefix parser: token={Token}").Range(Token).Report();
            return false;
        }

        expression          = prefixParser.Parse();
        expression.EndToken = Prev;

        while (Parsers.GetPrecedence() > precedence) {
            if (!TryGetInfixParser(out var infixParser))
                return false;

            expression          = infixParser.Parse(expression);
            expression.EndToken = Prev;
        }

        return true;
    }

    public bool TryGetInfixParser(out IInfixParser<Expression> infixParser, bool failOnNotMatched = true) {
        if (!Parsers.TryGetInfixParser(this, out infixParser)) {
            if (failOnNotMatched)
                Diagnostic_Error_Fatal($"Failed to parse infix expression: token={Token}").Range(Token).Report();
            return false;
        }
        return true;
    }

    public bool TryGetPrefixParser(out IPrefixParser<Expression> prefixParser, bool failOnNotMatched = true) {
        if (!Parsers.TryGetPrefixParser(this, out prefixParser)) {
            if (failOnNotMatched)
                Diagnostic_Error_Fatal($"Failed to parse prefix expression: token={Token}").Range(Token).Report();
            return false;
        }
        return true;
    }

    public bool TryParseStatement(out Expression expression) {
        expression = null;

        if (!Parsers.TryGetStatementParser(this, out var statementParser)) {

            if (TryParseExpression(out expression, 0)) {
                AdvanceIfSemicolon();

                expression.EndToken = Prev;
                return true;
            }

            return false;
        }

        expression = new StatementExpression(statementParser.Parse());

        AdvanceIfSemicolon();

        expression.EndToken = Prev;

        return true;
    }
    public Expression ParseStatement() {
        if (!TryParseStatement(out var expression)) {
            Expected("Failed to parse statement");
        }

        return expression;
    }


    public Token Advance(int n = 1) {
        var prev = Token;
        Lexer.Advance(n);
        return prev;
    }

    public Token AdvanceIfSemicolon() {
        return Token.IsSemicolon ? Advance() : Token;
    }

    public bool Sequence(params TokenType[] types) {
        for (var i = 0; i < types.Length; i++) {
            var tok = Lexer.Peek(i);

            if (!tok.Is(types[i])) {
                return false;
            }
        }

        return true;
    }

    public struct ParserLookAhead
    {
        public enum LookAheadType
        {
            MatchFirst,
            MatchAllSequential,
            MatchAllAnyOrder,
        }


        private SubParser Parser         { get; set; }
        private int       MaxLookAhead   { get; set; } = 100;
        private int       StartingCursor { get; set; }

        public int Steps => Parser.Lexer.Cursor - StartingCursor;

        public LookAheadType Type { get; set; } = LookAheadType.MatchAllSequential;

        public List<Token> Tokens { get; set; } = new();

        public Func<bool>[] MatchPredicates { get; set; }
        public Func<bool>[] BreakPredicates { get; set; }

        public ParserLookAhead(
            SubParser     parser,
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

    public ParserLookAhead LookAheadFirst(Func<bool>[] predicate, Func<bool>[] breakPredicate = null) {
        return new ParserLookAhead(this, ParserLookAhead.LookAheadType.MatchFirst, predicate, breakPredicate);
    }

    public ParserLookAhead LookAheadSequential(Func<bool>[] predicates, Func<bool>[] breakPredicates = null) {
        return new ParserLookAhead(this, ParserLookAhead.LookAheadType.MatchAllSequential, predicates, breakPredicates);
    }

    public ParserLookAhead LookAheadAnyOrder(Func<bool>[] predicate, Func<bool>[] breakPredicate = null) {
        return new ParserLookAhead(this, ParserLookAhead.LookAheadType.MatchAllAnyOrder, predicate, breakPredicate);
    }
}