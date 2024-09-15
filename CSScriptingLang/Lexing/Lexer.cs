using CSScriptingLang.Common.Extensions;
using CSScriptingLang.Utils;

namespace CSScriptingLang.Lexing;

public struct Lexer
{
    // public InputStream Input { get; set; }

    public string   InputSource;
    public Position Position;

    // public LexerPosition Pos = new();

    public Stack<Position> PositionMarkerStack { get; set; } = new();

    public char Current => CharForIdx(Position.Current);

    public List<Token> Tokens { get; set; }

    public Lexer(string input) {
        // Input       = new InputStream(input);
        InputSource = input;
        Tokens      = [];

        Position = new Position {
            Start   = 0,
            Current = 0,
            End     = InputSource.Length,
            Input   = InputSource,
        };

        Token current  = null;
        Token previous = null;
        while (Current != '\0') {
            previous = current;
            current  = TokenizeNext();

            if (previous != null && current != null) {
                previous.Next    = current;
                current.Previous = previous;
            }
        }

        if (Current == '\0') {
            PushToken(TokenType.EOF, false);
        }
    }

    private char CharForIdx(int idx) => idx >= InputSource.Length ? '\0' : InputSource[idx];

    public char Peek(int n = 1) {
        var peekChar = CharForIdx(Position.Forward(n));
        Position.Backward(n);
        return peekChar;
    }


    // private void NextChar() => Input.NextChar();
    public void NextChar() {
        Position.Current = Position.Forward();
        // Current = CharForIdx(Position.Forward());
    }

    public void SkipWhitespace() {
        while (IsWhitespace()) {
            NextChar();
        }
    }

    public Token GetEOFToken() {
        MarkEnd();
        var range = GetMarkerRange();
        return new Token(TokenType.EOF, "", range);
    }

    private Token GetToken(TokenType type, bool callNextChar) {
        if (callNextChar) {
            NextChar();
        }

        MarkEnd();

        var range = GetMarkerRange();
        var text  = Substring(range.Start, range.End);

        return new Token(type, text, range);
    }

    public void PushToken(Token token) {
        Tokens.Add(token);
    }
    public Token PushToken(TokenType type, bool callNextChar) {
        var token = GetToken(type, callNextChar);
        Tokens.Add(token);

        token.Idx = Tokens.Count - 1;

        token.Previous = Tokens.Count > 1 ? Tokens[^2] : null;

        return token;
    }
    private void PushError(string message) {
        var token = PushToken(TokenType.Error, false);
        token.ErrorMessage = message;

        throw new LexerException(message, token.Range, new TokenRange() {Start = token.Range.Start, End = token.Range.End})
           .WithInput(InputSource)
           .WithCaller(Caller.GetFromFrame(2));
        // ErrorWriter.LogError(token.ErrorMessage, token.Range, Input.GetCurrentPositionRange());
    }

    public Token TokenizeNext() {
        if (IsWhitespace()) {
            SkipWhitespace();
        }

        MarkStart();

        if (Current == '\0') {
            return PushToken(TokenType.EOF, false);
        }

        var basicTokenType = Current switch {
            '(' => TokenType.LParen,
            ')' => TokenType.RParen,
            '{' => TokenType.LBrace,
            '}' => TokenType.RBrace,
            '[' => TokenType.LBracket,
            ']' => TokenType.RBracket,
            ';' => TokenType.Semicolon,
            ':' => TokenType.Colon,
            ',' => TokenType.Comma,
            '&' => TokenType.And,
            '@' => TokenType.At,
            '_' => TokenType.Underscore,
            '<' => TokenType.LAngle,
            '>' => TokenType.RAngle,
            
            _   => TokenType.Error,
        };

        if (basicTokenType != TokenType.Error) {

            if (CanLexOperator()) {
                var tok = OperatorState();
                tok.Type = tok.Type.SetFlags(TokenType.Operator | basicTokenType, true);

                return tok;
            }
            return PushToken(basicTokenType, true);
        }

        // `.` and `...` cases
        if (Current == '.') {
            if (Peek() == '.' && Peek(2) == '.') {
                NextChar();
                NextChar();
                return PushToken(TokenType.DotDotDot, true);
            }

            return PushToken(TokenType.Dot, true);
        }

        Func<Token> AdditionalMatchers = Current switch {
            '"'  => StringState,
            '\'' => StringState,

            // IsDigit()
            >= '0' and <= '9' => NumberState,

            // IsIdentifier()
            >= 'a' and <= 'z' or >= 'A' and <= 'Z' or '_' => IdentifierState,

            _ => null,
        };

        if (AdditionalMatchers is not null) {
            return AdditionalMatchers();
        }


        if (Current == '=' && Peek() == '>') {
            NextChar();
            return PushToken(TokenType.Arrow, true);
        }

        // Handle line comments
        if (Current == '/' && Peek() == '/') {
            LineCommentState();
            return null;
        }

        if (Current == '/' && Peek() == '*') {
            BlockCommentState();
            return null;
        }

        if (CanLexOperator()) {
            return OperatorState();
        }

        PushError($"Unexpected character: {Current}");
        NextChar();

        return null;
    }


    private Token IdentifierState() {
        // numbers are allowed after the first character
        var idx = 0;
        while (IsIdentifier() || (idx > 0 && IsDigit())) {
            NextChar();
            idx++;
        }

        var tok = PushToken(TokenType.Identifier, false);

        if (Keywords.TryMatch(tok, out var keywordType)) {
            tok.Type    = tok.Type.SetFlags(TokenType.KeywordIdentifier, true);
            tok.Keyword = keywordType;

            if (Keywords.KeywordTokenTypes.TryGetValue(keywordType, out var tokenType)) {
                tok.Type = tok.Type.SetFlags(TokenType.KeywordIdentifier | tokenType, true);
            }
        }

        return tok;
    }

    private Token NumberState() {
        var tokenStr = "";
        var hasDot   = false;
        // If we have `0.f` or `0.01f` for example, we need to handle the float suffix
        var hasFloatSuffix = false;

        while (IsDigit() || (!hasDot && Current == '.') || (hasDot && !hasFloatSuffix && Current is 'f' or 'F')) {
            tokenStr += Current;

            if (Current == '.') {
                hasDot = true;
            }

            if (Current is 'f' or 'F') {
                hasFloatSuffix = true;
            }

            NextChar();
        }

        // var tok = GetToken(TokenType.Number, false);

        // Now we need to check the size of the number value, and set the appropriate type, possible types are: Int32, Int64, Float, Double
        if (tokenStr.Contains('.')) {
            if (tokenStr.Contains('f') || tokenStr.Contains('F')) {
                return PushToken(TokenType.Float, false);
            }

            return PushToken(TokenType.Double, false);
        }

        var int32MaxLen = int.MaxValue.ToString().Length;
        var int64MaxLen = long.MaxValue.ToString().Length;
        var numberLen   = tokenStr.Length;

        if (numberLen <= int32MaxLen) {
            return PushToken(TokenType.Int32, false);
        }

        if (numberLen <= int64MaxLen) {
            return PushToken(TokenType.Int64, false);
        }

        throw new Exception("Number is too large");
    }

    private Token StringState() {
        var quote = Current;

        NextChar(); // skip the opening quote
        while (Current != quote && Current != '\0') {
            NextChar();
        }

        if (Current == quote) {
            NextChar(); // skip the closing quote
            return PushToken(TokenType.String, false);
        }

        PushError("Unterminated string");

        return null;
    }
    private void LineCommentState() {
        while (Current is not '\n' and not '\0') {
            NextChar();
        }

        // PushToken(TokenType.LineComment, false);
    }

    private void BlockCommentState() {
        NextChar(); // skip the opening '/'
        NextChar(); // skip the opening '*'
        while (Current is not '*' and not '\0') {
            NextChar();
        }

        if (Current == '*') {
            NextChar(); // skip the closing '*'
            if (Current == '/') {
                NextChar(); // skip the closing '/'
                // PushToken(TokenType.BlockComment, false);
            } else {
                PushError("Unterminated block comment");
            }
        } else {
            PushError("Unterminated block comment");
        }
    }


    public bool IsWhitespace()   => Current is ' ' or '\t' or '\r' or '\n';
    public bool IsEOF()          => Current == '\0';
    public bool IsDigit()        => Current is >= '0' and <= '9';
    public bool IsLetter()       => Current is >= 'a' and <= 'z' or >= 'A' and <= 'Z';
    public bool IsAlphaNumeric() => IsLetter() || IsDigit();
    public bool IsIdentifier()   => IsLetter() || Current == '_';

    public (OperatorType, int) GetOperatorType() {
        var next = Peek();
        return Current switch {
            '+' => next switch {
                '+' => (OperatorType.Increment, 2),
                '=' => (OperatorType.PlusEquals, 2),
                _   => (OperatorType.Plus, 1),
            },
            '-' => next switch {
                '-' => (OperatorType.Decrement, 2),
                '=' => (OperatorType.MinusEquals, 2),
                _   => (OperatorType.Minus, 1),
            },
            '/' => (OperatorType.Divide, 1),
            '*' => (OperatorType.Multiply, 1),
            '%' => (OperatorType.Modulus, 1),
            '=' => next switch {
                '=' => (OperatorType.Equals, 2),
                _   => (OperatorType.Assignment, 1),
            },
            '!' => next switch {
                '=' => (OperatorType.NotEquals, 2),
                _   => (OperatorType.Not, 1),
            },
            '>' => next switch {
                '=' => (OperatorType.GreaterThanOrEqual, 2),
                _   => (OperatorType.GreaterThan, 1),
            },
            '<' => next switch {
                '=' => (OperatorType.LessThanOrEqual, 2),
                _   => (OperatorType.LessThan, 1),
            },
            '&' => next switch {
                '&' => (OperatorType.And, 2),
                _   => (OperatorType.None, 1),
            },
            '|' => next switch {
                '|' => (OperatorType.Or, 2),
                _   => (OperatorType.None, 1),
            },
            _ => (OperatorType.None, 0),
        };
    }

    public bool CanLexOperator() => OperatorTypes.OperatorCharCount.ContainsKey(Current);
    public Token OperatorState() {
        OperatorTypes.OperatorCharCount.TryGetValue(Current, out var maxChars);

        var tokenStr       = Current.ToString();
        var validOperators = new Stack<(string, OperatorType)>();


        if (OperatorTypes.TokenToOperatorType.TryGetValue(tokenStr, out var opType)) {
            validOperators.Push((tokenStr, opType));
        }

        for (var i = 1; i < maxChars; i++) {
            var nextChar = Peek(i);
            if (nextChar == '\0' || nextChar.IsWhitespace()) {
                break;
            }

            if (OperatorTypes.TokenToOperatorType.TryGetValue(tokenStr + nextChar, out opType)) {
                validOperators.Push((tokenStr + nextChar, opType));
            } else {
                break;
            }

            tokenStr += nextChar;

        }

        while (validOperators.Count > 0) {
            var (opStr, type) = validOperators.Pop();
            if (type == OperatorType.None)
                continue;

            for (var i = 0; i < opStr.Length; i++) {
                NextChar();
            }

            var token = PushToken(TokenType.Operator, false);
            token.Op = type;

            return token;
        }

        return null;
    }
    public Tuple<bool, TokenType, OperatorType, int> IsOperator() {
        var (type, chars) = GetOperatorType();

        return type switch {
            OperatorType.None => Tuple.Create(false, TokenType.Error, OperatorType.None, 0),

            OperatorType.LessThan    => Tuple.Create(true, TokenType.Operator | TokenType.LAngle, type, chars),
            OperatorType.GreaterThan => Tuple.Create(true, TokenType.Operator | TokenType.RAngle, type, chars),

            _ => Tuple.Create(true, TokenType.Operator, type, chars),
        };
    }

    public string Substring(int start, int end) {
        if (start == end) {
            return "";
        }

        return InputSource.Substring(start, end - start);
    }


    public void PushMarker() {
        PositionMarkerStack.Push(Position.Rent().From(Position));
    }
    public void ResetToStart() {
        var marker = PositionMarkerStack.Pop();
        Position.From(marker);
        marker.Return();
    }
    public void MarkStart() {
        Position.Start = Position.Current;
        PushMarker();
    }
    public void MarkEnd() {
        Position.End = Position.Current;
    }
    public TokenRange GetMarkerRange() {
        var range = new TokenRange {
            Start       = Position.Start,
            StartColumn = Position.Column,
            StartLine   = Position.Line,

            End       = Position.End,
            EndColumn = Position.Column,
            EndLine   = Position.Line,
        };

        if (PositionMarkerStack.Count != 0) {
            var marker = PositionMarkerStack.Pop();
            range.Start       = marker.Start;
            range.StartColumn = marker.Column;
            range.StartLine   = marker.Line;
            marker.Return();
        }

        return range;
    }
}

public struct LexerTokenStream
{
    private Lexer Lexer { get; set; }

    public int Cursor { get; set; }

    public List<Token> Tokens   => Lexer.Tokens;
    public int         Count    => Tokens.Count;
    public Token       Current  => Tokens[PeekClamped(0)];
    public Token       Next     => Tokens[PeekClamped(1)];
    public Token       NextNext => Tokens[PeekClamped(2)];
    public Token       Prev     => Tokens[PeekClamped(-1)];
    public Token       PrevPrev => Tokens[PeekClamped(-2)];

    public LexerTokenStream(string input) {
        Lexer = new Lexer(input);
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

    public Token Rollback(int toCursor) {
        Cursor = toCursor;
        return Current;
    }

    public Token Rewind(int steps = 1) {
        Cursor = Math.Max(0, Cursor - steps);
        return Current;
    }
}