using System.Runtime.CompilerServices;
using CSScriptingLang.Core.Diagnostics;
using CSScriptingLang.Core.Logging;
using CSScriptingLang.Interpreter.Execution.Declaration;
using CSScriptingLang.Interpreter.Modules;
using CSScriptingLang.Lexing;
using CSScriptingLang.Mixins;
using CSScriptingLang.Parsing.AST.NamedSymbol;
using CSScriptingLang.IncrementalParsing.Grammar;
using CSScriptingLang.IncrementalParsing.Syntax;
using CSScriptingLang.Utils;

namespace CSScriptingLang.IncrementalParsing;

public class UnexpectedTokenException : ApplicationException
{
    private TokenType Token { get; set; }
    public UnexpectedTokenException() { }
    public UnexpectedTokenException(string message) : base(message) { }
    public UnexpectedTokenException(string message, Exception inner) : base(message, inner) { }
    public UnexpectedTokenException(string message, TokenType token) : this(message) {
        Token = token;
    }
}

[Flags]
public enum ParseState
{
    None             = 0,
    PatternMatch     = 1 << 0,
    ForLoopCondition = 1 << 1,
}

[AddMixin(typeof(DiagnosticLoggingMixin))]
public partial class IncrementalParser
{
    protected Logger Logger = Logs.Get<IncrementalParser>(LogLevel.Warning);

    public ParseState State { get; set; } = ParseState.None;

    public Script                      Script         { get; set; }
    public LexerTokenStream            Lexer          { get; set; }
    public Stack<AttributeDeclaration> AttributeStack { get; } = new();
    public List<MarkEvent>             Events         { get; } = new();

    public IEnumerable<MarkEvent> Events_NoTrivias =>
        Events.Where(e => !e.IsTriviaLike());

    public int       Cursor => Lexer.Cursor;
    public Token     Token  => Lexer.Current;
    public TokenType Type   => Token.Type;

    public Token Next {
        get {
            var peekIdx = 1;
            var peeked  = Lexer.Tokens[Lexer.PeekClamped(peekIdx)];
            while (peeked.IsTriviaToken && Cursor + peekIdx < Lexer.Tokens.Count) {
                peeked = Lexer.Tokens[Lexer.PeekClamped(++peekIdx)];
            }
            return peeked;
        }
    }
    public Token NextNext {
        get {
            var peeked = Next.Next;
            while (peeked.IsTriviaToken && !peeked.IsEOF) {
                peeked = peeked.Next;
            }
            return peeked;
        }
    }
    public Token Prev {
        get {
            var peekIdx = -1;
            var peeked  = Lexer.Tokens[Lexer.PeekClamped(peekIdx)];
            while (peeked.IsTriviaToken && Cursor + peekIdx < Lexer.Tokens.Count) {
                peeked = Lexer.Tokens[Lexer.PeekClamped(++peekIdx)];
            }
            return peeked;
        }
    }
    public Token PrevPrev {
        get {
            var peekIdx = -2;
            var peeked  = Lexer.Tokens[Lexer.PeekClamped(peekIdx)];
            while (peeked.IsTriviaToken && Cursor + peekIdx < Lexer.Tokens.Count) {
                peeked = Lexer.Tokens[Lexer.PeekClamped(++peekIdx)];
            }
            return peeked;
        }
    }

    public int Idx => Token.Idx;

    public bool IsInPatternMatchState {
        get => State.HasFlag(ParseState.PatternMatch);
        set => State = State.SetFlags(ParseState.PatternMatch, value);
    }
    public bool IsInForLoopConditionState {
        get => State.HasFlag(ParseState.ForLoopCondition);
        set => State = State.SetFlags(ParseState.ForLoopCondition, value);
    }

    public UsingCallbackHandle UsingState(ParseState state) {
        State = State.SetFlags(state, true);
        return new UsingCallbackHandle(() => State = State.SetFlags(state, false));
    }

    public IncrementalParser(Script script) {
        Script = script;
        Lexer  = script.IncrementalLexer;
        Lexer.ResetToStart();
    }

    public IncrementalParser(string input) {
        Lexer = new LexerTokenStream(input);
    }

    public void Parse() {
        using var _ = TimedScope.Scoped_Print("IncrementalParser::Parse");

        if (Token.IsTriviaToken) {
            while (Token.IsTriviaToken) {
                Advance();
            }
        }

        BlockParser.Block(this, true);
    }

    public Marker MarkerWithContext(string context) {
        var position = Events.Count;
        var tokenIdx = Idx;
        Events.Add(new MarkEvent.NodeStart(0, SyntaxKind.None, context));
        return new Marker(this, position, tokenIdx);
    }
    public Marker Marker([CallerMemberName] string context = "") {
        var position = Events.Count;
        var tokenIdx = Idx;
        Events.Add(new MarkEvent.NodeStart(0, SyntaxKind.None, context));
        return new Marker(this, position, tokenIdx);
    }
    public Marker Marker(int tokenIdx, [CallerMemberName] string context = "") {
        var position = Events.Count;
        Events.Add(new MarkEvent.NodeStart(0, SyntaxKind.None, context));
        return new Marker(this, position, tokenIdx);
    }

    public Token Advance(bool canEatTokens = true) {
        var prev = Token;

        if (canEatTokens)
            Events.Add(new MarkEvent.EatToken(Token.Range, Token.SourceRange, Token.Type, Idx));
        Lexer.Advance();

        return AdvanceTrivia(prev, canEatTokens);
    }
    public Token AdvanceTrivia(Token prev = null, bool canEatTokens = true) {
        prev ??= Token;

        while (Token.IsTriviaToken) {
            prev = Token;

            if (canEatTokens)
                Events.Add(new MarkEvent.EatToken(Token.Range, Token.SourceRange, Token.Type, Idx));

            Lexer.Advance();
        }

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

    public ParserLookAhead LookAheadFirst(Func<bool>[] predicate, Func<bool>[] breakPredicate = null) {
        return new ParserLookAhead(this, ParserLookAhead.LookAheadType.MatchFirst, predicate, breakPredicate);
    }

    public ParserLookAhead_MatchAllSequential LookAheadSequential(Func<bool>[] predicates, Func<bool>[] breakPredicates = null) {
        return new ParserLookAhead_MatchAllSequential(this, predicates, breakPredicates);
    }
    public LexerLookAhead_MatchAllSequential LookAheadSequentialLexer(Func<Token, bool>[] predicates, Func<Token, bool>[] breakPredicates = null) {
        return new LexerLookAhead_MatchAllSequential(this, predicates, breakPredicates);
    }

    public ParserLookAhead LookAheadAnyOrder(Func<bool>[] predicate, Func<bool>[] breakPredicate = null) {
        return new ParserLookAhead(this, ParserLookAhead.LookAheadType.MatchAllAnyOrder, predicate, breakPredicate);
    }


    public void Expected(string message, NamedSymbolRange range = null) {
        ExpectedBuilder(message)
           .Range(range ?? Token)
           .Report();
    }

    public void Unexpected(string message = "", NamedSymbolRange range = null) {
        ExpectedBuilder($"Unexpected token: {Token} {message}")
           .Range(range ?? Token)
           .Report();
    }


    public bool Ensure(TokenType type, string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "") {
        if (Token.Is(type))
            return true;

        ExpectedBuilder($"{message}; got {Token}")
           .Range((Token, Next))
           .Caller(Caller.FromAttributes(file, line, member))
           .Report();

        return false;
    }

    public Token EnsureAnyOfAndConsume(TokenType type, string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "") {
        var curToken = Token;
        foreach (var tokenType in type.GetFlags()) {
            if (Token.Is(tokenType)) {
                Advance();
                return curToken;
            }
        }

        ExpectedBuilder($"{message}; got {Token}")
           .Range((Token, Next))
           .Caller(Caller.FromAttributes(file, line, member))
           .Report();

        return curToken;
        // throw new ParserException($"{message}; got {Token}", Token.Range, Next.Range, Script)
        // .WithCaller(Caller.FromAttributes(file, line, member))
        // .WithInput(Lexer.GetInput());
    }
    public Token EnsureAndConsume(TokenType type, string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "") {
        var curToken = Token;
        if (Token.Is(type))
            Advance();
        else {

            ExpectedBuilder($"{message}; got {Token}")
               .Range((Token, Next))
               .Caller(Caller.FromAttributes(file, line, member))
               .Report();

            // throw new ParserException($"{message}; got {Token}", Token.Range, Next.Range, Script)
            // .WithCaller(Caller.FromAttributes(file, line, member))
            // .WithInput(Lexer.GetInput());
        }

        return curToken;
    }

    public Token EnsureAndConsume(Keyword type, string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "") {
        var curToken = Token;
        if (Token.Is(type))
            Advance();
        else {
            ExpectedBuilder($"{message}; got {Token}")
               .Range((Token, Next))
               .Caller(Caller.FromAttributes(file, line, member))
               .Report();

            // throw new ParserException($"{message}; got {Token}", Token.Range, Next.Range, Script)
            // .WithCaller(Caller.FromAttributes(file, line, member))
            // .WithInput(Lexer.GetInput());
        }
        // RawLogError($"{message}; got {Token}", file, line, member);

        return curToken;
    }
}