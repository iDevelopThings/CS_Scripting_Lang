namespace CSScriptingLang.Lexing;

public class LexerCursor
{
    private Lexer Lexer       { get; set; }
    private int   TokenCursor { get; set; }

    public List<Token> Tokens => Lexer.Tokens;

    public LexerCursor(Lexer lexer) {
        Lexer = lexer;
    }

    public bool CanRead()              => PeekToken().Type != TokenType.EOF;
    public bool NextIs(TokenType type) => PeekToken().Type == type;
    public bool NextIs(TokenType type, int n) {
        var token = PeekNTokens(n);
        return token.Type == type;
    }
    public Token Read() => GetNextToken();
    public Token Read(int n) {
        if (n == 1)
            return Read();

        while (n-- > 0) {
            Read();
        }

        return Current();
    }
    public Token Current()   => Tokens[Math.Max(0, TokenCursor - 1)];
    public Token Peek()      => PeekToken();
    public Token Peek(int n) => PeekNTokens(n);

    public bool AdvanceIf(TokenType type) {
        if (!NextIs(type))
            return false;

        Read();
        return true;
    }

    private Token GetNextToken() {
        if (TokenCursor < Tokens.Count) {
            return Tokens[TokenCursor++];
        }

        while (Lexer.Current != '\0') {
            Lexer.TokenizeNext();
            if (Tokens.Count > TokenCursor) {
                return Tokens[TokenCursor++];
            }
        }

        var tok = Lexer.GetEOFToken();
        if (!Tokens[TokenCursor - 1].Type.Equals(TokenType.EOF)) {
            Tokens.Add(tok);
        }

        return tok;
    }

    private Token PeekToken() {
        if (TokenCursor < Tokens.Count) {
            return Tokens[TokenCursor];
        }

        while (Lexer.Current != '\0') {
            Lexer.TokenizeNext();
            if (Tokens.Count > TokenCursor) {
                return Tokens[TokenCursor];
            }
        }

        return Lexer.GetEOFToken();
    }
    private Token PeekNTokens(int n) {
        var cursor = TokenCursor;
        PeekToken();
        for (var i = 0; i < n; i++) {
            Read();
        }

        var result = PeekToken();
        TokenCursor = cursor;
        return result;
    }

    public Token Rewind(int steps = 1) {
        TokenCursor = Math.Max(0, TokenCursor - steps);
        return Current();
    }

    public static LexerCursor operator ++(LexerCursor cursor) {
        cursor.Read();
        return cursor;
    }
    public static LexerCursor operator --(LexerCursor cursor) {
        cursor.Rewind();
        return cursor;
    }
}