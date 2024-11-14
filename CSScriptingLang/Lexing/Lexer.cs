using CSScriptingLang.Common.Extensions;
using CSScriptingLang.Core.Diagnostics;
using CSScriptingLang.Utils;
using CSScriptingLang.Core.Logging;

namespace CSScriptingLang.Lexing;

[Flags]
public enum LexerState
{
    None                = 0,
    CanOutputComments   = 1 << 0,
    CanOutputWhitespace = 1 << 1,
    CanOutputNewLines   = 1 << 2,
}

public partial struct Lexer
{
    private static Logger Logger = Logs.Get<Lexer>();

    public string     InputSource;
    public Position   Position;
    public LexerState State { get; set; } = LexerState.None;

    public Stack<Position> PositionMarkerStack { get; set; } = new();

    public  char Current             => CharForIdx(Position.Current);
    private char CharForIdx(int idx) => idx >= InputSource.Length ? '\0' : InputSource[idx];

    public List<Token> Tokens { get; set; }

    public Action<Token> OnToken { get; set; }

    public Lexer(
        SourceText    input,
        Action<Token> onToken = null,
        bool          lexAll  = true,
        LexerState    state   = LexerState.None
    ) {
        InputSource = input;
        Tokens      = [];
        State       = state;

        Position = new Position {
            Start   = 0,
            Current = 0,
            End     = InputSource.Length,
            Input   = InputSource,
        };

        OnToken = onToken;

        if (lexAll)
            LexAll();
    }

    public IEnumerable<Token> Tokenize() {
        Token current  = null;
        Token previous = null;
        while (Current != '\0') {
            var next = TokenizeNext();
            if (next == null) {
                continue;
            }
            previous = current;
            current  = next;

            yield return current;

            if (previous != null) {
                previous.Next    = current;
                current.Previous = previous;
            }

            OnToken?.Invoke(current);
        }

        if (Current == '\0' && current?.Type != TokenType.EOF) {
            var tok = PushToken(TokenType.EOF, false);
            // tok.Previous = previous;
            // previous.Next = tok;
            yield return tok;
        }
    }

    private void LexAll() {
        // MacroExpansion();
        Tokens = Tokenize().ToList();
    }


    public char Peek(int n = 1) {
        var peekChar = CharForIdx(Position.Forward(n));
        Position.Backward(n);
        return peekChar;
    }

    public void NextChar() {
        Position.Current = Position.Forward();
    }

    public Token SkipWhitespace() {
        while (Current.IsWhitespace()) {
            NextChar();
        }

        if ((State & LexerState.CanOutputWhitespace) == LexerState.CanOutputWhitespace)
            return PushToken(TokenType.Whitespace, false);

        return null;
    }

    private Token SkipNewLine() {
        while (Current.IsNewLine()) {
            NextChar();
        }

        if ((State & LexerState.CanOutputNewLines) == LexerState.CanOutputNewLines)
            return PushToken(TokenType.NewLine, false);

        return null;
    }

    private Token GetToken(TokenType type, bool callNextChar) {
        if (callNextChar) {
            NextChar();
        }

        MarkEnd();

        var range = GetMarkerRange();
        var text  = Substring(range.Start, range.End);
        range.InputSource = InputSource;

        return new Token(type, text, range);
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

        DiagnosticManager.Diagnostic_Error_Fatal().Message(message).Range(token).Report();
    }

    public Token TokenizeNext() {

        MarkStart();

        if (Current.IsNewLine())
            return SkipNewLine();

        if (Current.IsWhitespace())
            return SkipWhitespace();

        if (Current.IsEOF()) {
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
            '+' => Peek() == '+' ? TokenType.PlusPlus : TokenType.Plus,
            '-' => Peek() == '-' ? TokenType.MinusMinus : TokenType.Minus,
            '=' => Peek() switch {
                '>' => TokenType.Arrow,
                _   => Peek(2) == '=' ? TokenType.EqualsStrict : TokenType.Equals,
            },

            _ => TokenType.Error,
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

        if (Current == '#' && Peek() == '#') {
            NextChar();
            return PushToken(TokenType.HashHash, true);
        }

        Func<Token> AdditionalMatchers = Current switch {
            '"'  => StringState,
            '\'' => StringState,

            >= '0' and <= '9' => NumberState,
            '#'               => MacroIdentifierState,

            // IsIdentifier()
            >= 'a' and <= 'z' or >= 'A' and <= 'Z' or '_' => IdentifierState,

            _ => null,
        };

        if (AdditionalMatchers is not null) {
            return AdditionalMatchers();
        }

        if (Current == '-' && Peek().IsNumber()) {
            return NumberState();
        }

        if (Current == '=' && Peek() == '>') {
            NextChar();
            return PushToken(TokenType.Arrow, true);
        }

        // Handle line comments
        if (Current == '/' && Peek() == '/') {
            return LineCommentState();
        }

        if (Current == '/' && Peek() == '*') {
            return BlockCommentState();
        }

        if (CanLexOperator()) {
            return OperatorState();
        }

        PushError($"Unexpected character: {Current}");
        NextChar();

        return null;
    }

    private Token IdentifierState() => IdentifierStateImpl(false);
    private Token IdentifierStateImpl(bool isMacro) {
        // numbers are allowed after the first character
        var idx = 0;
        while (
            Current.IsIdentifier() ||
            (idx > 0 && Current.IsDigit()) ||
            (idx == 0 && isMacro && Current == '#')
        ) {
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
        var hasMinus = Current == '-';
        if (hasMinus) {
            tokenStr += Current;
            NextChar();
        }

        // If we have `0.f` or `0.01f` for example, we need to handle the float suffix
        var hasFloatSuffix = false;

        while (
            Current.IsDigit() ||
            (!hasDot && Current == '.') ||
            (hasDot && !hasFloatSuffix && Current is 'f' or 'F')
        ) {
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

    private Token LineCommentState() {
        while (Current is not '\n' and not '\0') {
            NextChar();
        }

        if ((State & LexerState.CanOutputComments) == LexerState.CanOutputComments)
            return PushToken(TokenType.LineComment, false);

        return null;
    }

    private Token BlockCommentState() {
        NextChar(); // skip the opening '/'
        NextChar(); // skip the opening '*'

        // Continue until we find the closing '*/'
        while (Current != '\0') {
            if (Current == '*' && Peek() == '/') {
                break;
            }
            NextChar();
            if (Current.IsWhitespace())
                SkipWhitespace();
        }

        NextChar(); // skip the closing '*'
        NextChar(); // skip the closing '/'

        if ((State & LexerState.CanOutputComments) == LexerState.CanOutputComments)
            return PushToken(TokenType.BlockComment, false);

        return null;
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


}