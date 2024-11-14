using CSScriptingLang.Common.Extensions;
using CSScriptingLang.IncrementalParsing;
using CSScriptingLang.IncrementalParsing.Syntax;

namespace CSScriptingLang.Lexing;

public class TokenMarker
{
    // Index of the token in the token stream
    public int Idx    { get; set; } = -1;
    public int EndIdx { get; set; } = -1;

    public bool IsClosed => EndIdx > 0;

    public string Context { get; set; } = null;

    public LexerTokenStream Stream { get; set; }

    public TokenMarker() { }

    public TokenMarker(
        LexerTokenStream stream,
        Token            token,
        string           context = null
    ) {
        Stream  = stream;
        Idx     = token.Idx;
        Context = context;
    }

    public void Close(Token token) {
        if (EndIdx > 0) {
            throw new InvalidOperationException("Token marker is already closed");
        }
        EndIdx = token.Idx;
        Stream.Markers.Add(this);
    }

    public void Rollback() {
        Stream.RollbackToMarker(this);
    }

}

public class LexerTokenStream
{
    private Lexer Lexer;

    public int Cursor { get; set; }

    public List<Token> Tokens => Lexer.Tokens;
    public int         Count  => Tokens.Count;
    // public Token       Current => Cursor < Tokens.Count ? Tokens[Cursor] : null;
    public Token Current  => Tokens[PeekClamped(0)];
    public Token Next     => Tokens[PeekClamped(1)];
    public Token NextNext => Tokens[PeekClamped(2)];
    public Token Prev     => Tokens[PeekClamped(-1)];
    public Token PrevPrev => Tokens[PeekClamped(-2)];

    public SourceText InputSource => Lexer.InputSource;

    public Stack<TokenMarker> MarkerStack = new();
    public List<TokenMarker>  Markers     = new();

    public LexerTokenStream(
        string        input,
        Action<Token> onToken = null,
        bool          lexAll  = true,
        LexerState    state   = LexerState.None
    ) {
        Lexer = new Lexer(input, onToken, lexAll, state);
    }

    public void ForceParseTrivia() {
        var next = Cursor;
        PeekSkipTrivia(ref next, 1);
        Cursor = next;
    }
    public Token GetCurrent(IncrementalParser p, int offset = 0) {
        var off = PeekClamped(offset);
        ParseTrivia(p, ref off);
        Cursor = off;

        return Tokens[off];
    }

    private void ParseTrivia(IncrementalParser p, ref int index) {

        var lineCount    = 0;
        var docTokenData = new List<Token>();
        for (; index < Tokens.Count; index++) {
            var tok = Tokens[index];

            switch (tok.Type) {
                case TokenType.BlockComment:
                case TokenType.LineComment: {
                    lineCount = 0;
                    docTokenData.Add(tok);
                    break;
                }

                case TokenType.NewLine: {
                    lineCount++;
                    if (lineCount >= 2 && docTokenData.Count > 0) {
                        ParseComments(p, docTokenData);
                        docTokenData.Clear();
                    } else if (docTokenData.Count == 1 && index - 2 >= 0) {
                        var tempIndex     = index - 2;
                        var inlineComment = false;
                        for (; tempIndex >= 0; tempIndex--) {
                            var tempTok = Tokens[tempIndex];
                            switch (tempTok) {
                                case {IsNewLine: true}: {
                                    goto endLoop;
                                }
                                case {IsWhitespace: true}: {
                                    continue;
                                }
                                default: {
                                    inlineComment = true;
                                    goto endLoop;
                                }
                            }
                        }
                        endLoop:

                        if (inlineComment) {
                            ParseComments(p, docTokenData);
                            docTokenData.Clear();
                        }
                    }

                    goto case TokenType.Whitespace;
                }

                case TokenType.Whitespace: {
                    if (docTokenData.Count == 0) {
                        p.Events.Add(
                            new MarkEvent.EatToken(
                                Tokens[index].Range,
                                Tokens[index].SourceRange,
                                Tokens[index].Type,
                                index
                            )
                        );
                    } else {
                        docTokenData.Add(Tokens[index]);
                    }

                    break;
                }
                default: {
                    // ReSharper disable once InvertIf
                    if (docTokenData.Count > 0) {
                        ParseComments(p, docTokenData);
                        docTokenData.Clear();
                    }

                    return;
                }


            }
        }

        // ReSharper disable once InvertIf
        if (docTokenData.Count > 0) {
            ParseComments(p, docTokenData);
            docTokenData.Clear();
        }
    }
    private void ParseComments(IncrementalParser p, List<Token> tokenData) {
        var afterAddList = new List<Token>();
        // Traverse tokenData in reverse and remove the blanks and line endings inside
        for (var i = tokenData.Count - 1; i >= 0; i--) {
            if (tokenData[i].Type is TokenType.Whitespace or TokenType.NewLine) {
                afterAddList.Add(tokenData[i]);
                tokenData.RemoveAt(i);
            } else {
                break;
            }
        }

        if (tokenData.Count > 0) {
            foreach (var tok in tokenData) {
                var m = p.Marker(tok.Idx);
                p.Events.Add(new MarkEvent.EatToken(tok.Range, tok.SourceRange, tok.Type, tok.Idx));
                m.Complete(SyntaxKind.Comment);
            }
        }


        // _docParser.Parse(tokenData);
        // ReSharper disable once InvertIf
        if (afterAddList.Count != 0) {
            // Reverse traversal is added to the event
            for (var i = afterAddList.Count - 1; i >= 0; i--) {
                p.Events.Add(
                    new MarkEvent.EatToken(
                        afterAddList[i].Range,
                        afterAddList[i].SourceRange,
                        afterAddList[i].Type,
                        afterAddList[i].Idx
                    )
                );
            }
        }
    }

    public Token PeekSkipTrivia(int n = 1) {
        var next   = Cursor;
        var nIdx   = PeekSkipTrivia(ref next, n);
        var newIdx = Tokens.ClampedIndex(next);
        return Tokens[newIdx];
    }

    private int PeekSkipTrivia(ref int index, int n = 1) {
        var isForward = n > 0;
        var skipped   = 0;

        for (; index < Tokens.Count; index += isForward ? 1 : -1) {
            if (!Tokens.IsValidIndex(index))
                break;

            var tok = Tokens[index];
            if (tok.IsTriviaToken) continue;

            skipped++;
            if (skipped == Math.Abs(n)) {
                break;
            }
        }


        /*index += isForward ? 1 : -1;

        while (
            skipped < Math.Abs(n) &&
            index >= 0 && index < Tokens.Count
        ) {
            var tok = Tokens[index];
            if (
                tok.IsWhitespace ||
                tok.IsNewLine ||
                tok.IsLineComment ||
                tok.IsBlockComment
            ) {
                index += isForward ? 1 : -1;
                continue;
            }
            skipped++;
            index += isForward ? 1 : -1;
            if (skipped == Math.Abs(n)) {
                break;
            }
        }

        return;*/

        return index;
    }

    public void ResetToStart() {
        Cursor = 0;
    }

    public string GetInput() => Lexer.InputSource;
    public void AppendInput(string input) {
        var lexer = Lexer;

        if (lexer.Current == '\0' && lexer.InputSource.Length > 0) {
            // remove the EOF character
            lexer.InputSource = lexer.InputSource[..^1];
            // Add new line character
            lexer.InputSource += '\n';
        }

        lexer.InputSource += input;
        // Add EOF character
        lexer.InputSource += '\0';

        lexer.Position.End = lexer.InputSource.Length;
        // lexer.Current      = lexer.InputSource.Length > 0 ? lexer.InputSource[lexer.Position.Current] : '\0';

        while (lexer.Current != '\0') {
            lexer.TokenizeNext();
        }

        if (lexer.Current == '\0') {
            lexer.PushToken(TokenType.EOF, false);
        }

        Lexer = lexer;
    }
    public void OnAppendInput() {
        // if our current token is an EOF token, we need to remove it
        if (Tokens.Count > 0 && Tokens[^1].Type.Equals(TokenType.EOF)) {
            Tokens.RemoveAt(Tokens.Count - 1);
            if (Cursor > Tokens.Count) {
                Cursor = Tokens.Count;
            }
        }


    }

    public bool NextIs(TokenType type)        => Next.Type == type;
    public bool NextIs(TokenType type, int n) => Peek(n).Type == type;

    public int PeekClamped(int n) => Math.Max(0, Math.Min(Tokens.Count - 1, Cursor + n));

    public Token Peek(int n = 1) {
        return Tokens[PeekClamped(n)];
    }

    public List<Token> Range(int prevN = -5, int nextN = 5) {
        var prevNClamped = Math.Max(0, Math.Min(Tokens.Count - 1, Cursor + prevN));
        var nextNClamped = Math.Max(0, Math.Min(Tokens.Count - 1, Cursor + nextN));

        return Tokens.GetRange(prevNClamped, nextNClamped - prevNClamped);
    }

    public string SourceRange(int prevN = -5, int nextN = 5, bool colored = false) {
        var range = Range(prevN, nextN);
        var str   = string.Join("", range.Select(t => t.Value));
        return str;
    }

    public Token Advance(int n = 1) {
        Cursor = PeekClamped(n);
        return Current;
    }
    public Token AdvanceTrivia(Token prev = null) {
        prev ??= Current;
        while (Current.IsTriviaToken) {
            prev = Advance();
        }
        return prev;
    }
    public Token Rollback(int toCursor) {
        Cursor = toCursor;
        AdvanceTrivia();

        return Current;
    }

    public Token Rewind(int steps = 1) {
        Cursor = Math.Max(0, Cursor - steps);
        return Current;
    }

    public LexerTokenStream CreateChildWithState(LexerState state, bool lexAll = true) {
        var child = new LexerTokenStream(Lexer.InputSource, Lexer.OnToken, lexAll);

        child.Lexer.State = state;

        return child;
    }

    public IEnumerable<Token> Tokenize() => Lexer.Tokenize();

    public TokenMarker Mark(string context = null) {
        var m = new TokenMarker(this, Current, context);
        MarkerStack.Push(m);
        return m;
    }
    public void MarkEnd() {
        if (MarkerStack.Count <= 0)
            return;

        var marker = MarkerStack.Pop();
        marker.Close(Current);
    }
    public void DropMark() {
        if (MarkerStack.Count <= 0)
            return;
        MarkerStack.Pop();
    }
    public void Rollback() {
        if (MarkerStack.Count <= 0)
            return;
        var marker = MarkerStack.Pop();
        Cursor = marker.Idx;
    }
    public void RollbackToMarker(TokenMarker marker) {
        Cursor = marker.Idx;
    }
}