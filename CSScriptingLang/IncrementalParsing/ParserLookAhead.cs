using CSScriptingLang.Lexing;

namespace CSScriptingLang.IncrementalParsing;

public struct ParserLookAhead
{
    public enum LookAheadType
    {
        MatchFirst,
        MatchAllSequential,
        MatchAllAnyOrder,
    }


    private IncrementalParser Parser         { get; set; }
    private int               MaxLookAhead   { get; set; } = 100;
    private int               StartingCursor { get; set; }

    public int Steps => Parser.Lexer.Cursor - StartingCursor;

    public LookAheadType Type { get; set; } = LookAheadType.MatchAllSequential;

    public List<Token> Tokens { get; set; } = new();

    public Func<bool>[] MatchPredicates { get; set; }
    public Func<bool>[] BreakPredicates { get; set; }

    public ParserLookAhead(
        IncrementalParser parser,
        LookAheadType     type,
        Func<bool>[]      predicates,
        Func<bool>[]      breakPredicates
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


public struct ParserLookAhead_MatchAllSequential
{
    private IncrementalParser Parser         { get; set; }
    private int               MaxLookAhead   { get; set; } = 100;
    private int               StartingCursor { get; set; }
    private bool              RollsbackOnEnd { get; set; }

    public int Steps => Parser.Lexer.Cursor - StartingCursor;

    public List<Token> Tokens { get; set; } = new();

    public Func<bool>[] MatchPredicates  { get; set; }
    public Func<bool>[] ReadToPredicates { get; set; }
    public Func<bool>[] BreakPredicates  { get; set; }

    public Marker Mark { get; set; }

    private Action<ParserLookAhead_MatchAllSequential, bool> _onAnyResult { get; set; }
    private Action<ParserLookAhead_MatchAllSequential>       _onMatch     { get; set; }
    private Action<ParserLookAhead_MatchAllSequential>       _onFail      { get; set; }

    public ParserLookAhead_MatchAllSequential(
        IncrementalParser parser,
        Func<bool>[]      predicates,
        Func<bool>[]      breakPredicates,
        Func<bool>[]      readToPredicates = null
    ) {
        Parser           = parser;
        Mark             = Parser.Marker();
        StartingCursor   = parser.Lexer.Cursor;
        MatchPredicates  = predicates;
        BreakPredicates  = breakPredicates;
        ReadToPredicates = readToPredicates;
    }

    public ParserLookAhead_MatchAllSequential RollbackOnEnd(bool value = true) {
        RollsbackOnEnd = value;
        return this;
    }

    public ParserLookAhead_MatchAllSequential OnAnyResult(Action<ParserLookAhead_MatchAllSequential, bool> action) {
        _onAnyResult = action;
        return this;
    }
    public ParserLookAhead_MatchAllSequential OnMatch(Action<ParserLookAhead_MatchAllSequential> action) {
        _onMatch = action;
        return this;
    }
    public ParserLookAhead_MatchAllSequential OnFail(Action<ParserLookAhead_MatchAllSequential> action) {
        _onFail = action;
        return this;
    }

    public ParserLookAhead_MatchAllSequential SetStartPosition(Action action) {
        action();
        return this;
    }

    public ParserLookAhead_MatchAllSequential WithMaxLookAhead(int max) {
        MaxLookAhead = max;
        return this;
    }

    private void onAny(bool result) {
        _onAnyResult?.Invoke(this, result);

        if (RollsbackOnEnd) {
            Rollback();
        }
    }
    private void onMatched() {
        onAny(true);
        _onMatch?.Invoke(this);
    }
    private void onFailed() {
        onAny(false);
        _onFail?.Invoke(this);
    }

    public bool Execute() {
        var matchSequence = new Queue<Func<bool>>(MatchPredicates);
        var predicateIdx  = 0;

        while (!Parser.Token.IsEOF && Steps < MaxLookAhead) {
            Tokens.Add(Parser.Token);

            var tok = Parser.Advance();

            if (matchSequence.Count == 0) {
                onMatched();
                return true;
            }

            var pred = matchSequence.Peek();
            if (pred()) {
                matchSequence.Dequeue();
                predicateIdx++;
            } else {
                if (predicateIdx > 0) {
                    onFailed();
                    return false;
                }
            }

            if (matchSequence.Count == 0) {
                onMatched();
                return true;
            }

            if (BreakPredicates != null) {
                if (BreakPredicates.Any(x => x())) {
                    onFailed();
                    return false;
                }
            }
        }

        onFailed();

        return false;
    }

    public static implicit operator bool(ParserLookAhead_MatchAllSequential la) => la.Execute();

    public void Rollback() {
        Mark.Rollback();
    }
}


public struct LexerLookAhead_MatchAllSequential
{
    private IncrementalParser Parser { get; set; }
    private LexerTokenStream  Lexer  => Parser.Lexer;

    private int  MaxLookAhead   { get; set; } = 100;
    private int  StartingCursor { get; set; }
    private bool RollsbackOnEnd { get; set; }

    public int Steps => Parser.Lexer.Cursor - StartingCursor;

    public Func<Token, bool>[] MatchPredicates  { get; set; }
    public Func<Token, bool>[] ReadToPredicates { get; set; }
    public Func<Token, bool>[] BreakPredicates  { get; set; }

    private Action<LexerLookAhead_MatchAllSequential, bool> _onAnyResult { get; set; }
    private Action<LexerLookAhead_MatchAllSequential>       _onMatch     { get; set; }
    private Action<LexerLookAhead_MatchAllSequential>       _onFail      { get; set; }

    public LexerLookAhead_MatchAllSequential(
        IncrementalParser   parser,
        Func<Token, bool>[] predicates,
        Func<Token, bool>[] breakPredicates,
        Func<Token, bool>[] readToPredicates = null
    ) {
        Parser           = parser;
        StartingCursor   = parser.Lexer.Cursor;
        MatchPredicates  = predicates;
        BreakPredicates  = breakPredicates;
        ReadToPredicates = readToPredicates;
    }

    public LexerLookAhead_MatchAllSequential RollbackOnEnd(bool value = true) {
        RollsbackOnEnd = value;
        return this;
    }

    public LexerLookAhead_MatchAllSequential OnAnyResult(Action<LexerLookAhead_MatchAllSequential, bool> action) {
        _onAnyResult = action;
        return this;
    }
    public LexerLookAhead_MatchAllSequential OnMatch(Action<LexerLookAhead_MatchAllSequential> action) {
        _onMatch = action;
        return this;
    }
    public LexerLookAhead_MatchAllSequential OnFail(Action<LexerLookAhead_MatchAllSequential> action) {
        _onFail = action;
        return this;
    }

    public LexerLookAhead_MatchAllSequential SetStartPosition(Action action) {
        action();
        return this;
    }

    public LexerLookAhead_MatchAllSequential WithMaxLookAhead(int max) {
        MaxLookAhead = max;
        return this;
    }

    private void onAny(bool result) {
        _onAnyResult?.Invoke(this, result);

        if (RollsbackOnEnd) {
            Rollback();
        }
    }
    private void onMatched() {
        onAny(true);
        _onMatch?.Invoke(this);
    }
    private void onFailed() {
        onAny(false);
        _onFail?.Invoke(this);
    }

    public bool Execute() {
        var matchSequence = new Queue<Func<Token, bool>>(MatchPredicates);
        var predicateIdx  = 0;

        while (!Parser.Token.IsEOF && Steps < MaxLookAhead) {
            var tok = Lexer.Advance();
            tok = Lexer.AdvanceTrivia(tok);

            if (matchSequence.Count == 0) {
                onMatched();
                return true;
            }

            var pred = matchSequence.Peek();
            if (pred(tok)) {
                matchSequence.Dequeue();
                predicateIdx++;
            } else {
                if (predicateIdx > 0) {
                    onFailed();
                    return false;
                }
            }

            if (matchSequence.Count == 0) {
                onMatched();
                return true;
            }

            if (BreakPredicates != null) {
                if (BreakPredicates.Any(x => x(tok))) {
                    onFailed();
                    return false;
                }
            }
        }

        onFailed();

        return false;
    }

    public static implicit operator bool(LexerLookAhead_MatchAllSequential la) => la.Execute();

    public void Rollback() {
        Lexer.Rollback(StartingCursor);
    }
}