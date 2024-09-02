using CSScriptingLang.Utils;

namespace CSScriptingLang.Lexing;

public class Lexer
{
    public InputStream Input { get; set; }

    public char Current => Input.Current;

    public LexerCursor Cursor;
    public ErrorWriter ErrorWriter;
    public List<Token> Tokens { get; set; }

    public Lexer(string input) {
        Input       = new InputStream(input);
        Cursor      = new LexerCursor(this);
        ErrorWriter = new ErrorWriter(input);

        Tokens = [];

        TokenizeNext();
    }

    private void NextChar() => Input.NextChar();

    public Token GetEOFToken() {
        Input.MarkEnd();
        var range = Input.GetMarkerRange();
        return new Token(TokenType.EOF, "", range);
    }

    private Token GetToken(TokenType type, bool callNextChar) {
        if (callNextChar) {
            NextChar();
        }

        Input.MarkEnd();

        var range = Input.GetMarkerRange();
        var text  = Input.Substring(range.Start, range.End);

        return new Token(type, text, range);
    }
    private void PushToken(Token token) {
        Tokens.Add(token);
    }
    private void PushToken(TokenType type, bool callNextChar) {
        var token = GetToken(type, callNextChar);
        Tokens.Add(token);
    }
    private void PushError(string message) {
        var token = GetToken(TokenType.Error, false);
        token.ErrorMessage = message;
        Tokens.Add(token);

        ErrorWriter.LogError(token.ErrorMessage, token.Range, Input.GetCurrentPositionRange());
    }

    public void TokenizeNext() {
        if (Input.IsWhitespace()) {
            Input.SkipWhitespace();
        }

        Input.MarkStart();

        if (Current == '\0') {
            PushToken(TokenType.EOF, false);
            return;
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
            '.' => TokenType.Dot,
            ',' => TokenType.Comma,
            _   => TokenType.Error,
        };

        if (basicTokenType != TokenType.Error) {
            PushToken(basicTokenType, true);
            return;
        }

        Action AdditionalMatchers = Current switch {
            '"'  => StringState,
            '\'' => StringState,

            // Input.IsDigit()
            >= '0' and <= '9' => NumberState,

            // Input.IsIdentifier()
            >= 'a' and <= 'z' or >= 'A' and <= 'Z' or '_' => IdentifierState,

            _ => null
        };

        if (AdditionalMatchers is not null) {
            AdditionalMatchers();
            return;
        }

        // Handle line comments
        if (Current == '/' && Input.Peek() == '/') {
            LineCommentState();
            return;
        }

        if (Current == '/' && Input.Peek() == '*') {
            BlockCommentState();
            return;
        }

        var (isOperator, tokenType, opType, opChars) = Input.IsOperator();
        if (isOperator) {
            for (var i = 0; i < opChars; i++) {
                NextChar();
            }

            var opToken = GetToken(tokenType, false);
            opToken.Op = opType;


            PushToken(opToken);
            return;
        }


        PushError($"Unexpected character: {Current}");
        NextChar();

        /*switch (Current) {
            case '"':
                StringState();
                break;
            default:
                if (Input.IsDigit()) {
                    NumberState();
                    break;
                }

                if (Input.IsIdentifier()) {
                    IdentifierState();
                    break;
                }

                var (isOperator, tokenType, opType) = Input.IsOperator();
                if (isOperator) {
                    var opToken = GetToken(tokenType, true);
                    opToken.Op = opType;
                    PushToken(opToken);
                    break;
                }

                PushError($"Unexpected character: {Current}");
                NextChar();

                break;
        }*/
    }


    private void IdentifierState() {
        // numbers are allowed after the first character
        var idx = 0;
        while (Input.IsIdentifier() || (idx > 0 && Input.IsDigit())) {
            NextChar();
            idx++;
        }

        var tok = GetToken(TokenType.Identifier, false);

        if (Keywords.KeywordTypes.TryGetValue(tok.Value, out var keywordType)) {
            tok.Type = tok.Type.SetFlags(TokenType.KeywordIdentifier | keywordType, true);
        }

        PushToken(tok);
    }

    private void NumberState() {
        var tokenStr = "";
        var hasDot   = false;
        // If we have `0.f` or `0.01f` for example, we need to handle the float suffix
        var hasFloatSuffix = false;

        while (Input.IsDigit() || (!hasDot && Current == '.') || (hasDot && !hasFloatSuffix && Current is 'f' or 'F')) {
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
                PushToken(TokenType.Float, false);
                return;
            }

            PushToken(TokenType.Double, false);
            return;
        }

        var int32MaxLen = int.MaxValue.ToString().Length;
        var int64MaxLen = long.MaxValue.ToString().Length;
        var numberLen   = tokenStr.Length;

        if (numberLen <= int32MaxLen) {
            PushToken(TokenType.Int32, false);
            return;
        }

        if (numberLen <= int64MaxLen) {
            PushToken(TokenType.Int64, false);
            return;
        }

        throw new Exception("Number is too large");
    }

    private void StringState() {
        var quote = Current;

        NextChar(); // skip the opening quote
        while (Current != quote && Current != '\0') {
            NextChar();
        }

        if (Current == quote) {
            NextChar(); // skip the closing quote
            PushToken(TokenType.String, false);
        } else {
            PushError("Unterminated string");
        }
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
}