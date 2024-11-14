using CSScriptingLang.IncrementalParsing.Syntax;
using JOS.Enumeration;

namespace CSScriptingLang.Lexing;

[Flags]
public enum TokenType : long
{
    None = 0,

    Identifier = 1L << 0,
    Keyword    = 1L << 1,

    Int32  = 1L << 2,
    Int64  = 1L << 3,
    Float  = 1L << 4,
    Double = 1L << 5,
    // Number  = Int32 | Int64 | Float | Double,

    Boolean = 1L << 6,
    String  = 1L << 7,
    Null    = 1L << 8,

    LParen   = 1L << 9,  // (
    RParen   = 1L << 10, // )
    LBrace   = 1L << 11, // {
    RBrace   = 1L << 12, // }
    LBracket = 1L << 13, // [
    RBracket = 1L << 14, // ]
    LAngle   = 1L << 15, // <
    RAngle   = 1L << 16, // >

    Arrow      = 1L << 17, // =>
    Dot        = 1L << 18, // .
    DotDotDot  = 1L << 19, // ...
    Semicolon  = 1L << 20, // ;
    Comma      = 1L << 21, // ,
    Colon      = 1L << 22, // :
    And        = 1L << 23, // &
    At         = 1L << 24, // @
    Underscore = 1L << 25, // _
    Question   = 1L << 26, // ?
    Tilde      = 1L << 27, // ~
    Hash       = 1L << 28, // #
    HashHash   = 1L << 29, // ##
    Dollar     = 1L << 30, // $
    Backslash  = 1L << 31, // \

    [OperatorTokenType(OperatorType.Plus)]
    Plus = 1L << 32, // +
    [OperatorTokenType(OperatorType.Increment)]
    PlusPlus = 1L << 33, // ++
    [OperatorTokenType(OperatorType.Minus)]
    Minus = 1L << 34, // -
    [OperatorTokenType(OperatorType.Decrement)]
    MinusMinus = 1L << 35, // --

    LineComment  = 1L << 36, // //
    BlockComment = 1L << 37, // /* */

    Operator = 1L << 38, // Operator is a catch-all for all operators

    [OperatorTokenType(OperatorType.Assignment)]
    Assignment = 1L << 39, // =
    [OperatorTokenType(OperatorType.Equals)]
    Equals = 1L << 40, // ==
    [OperatorTokenType(OperatorType.EqualsStrict)]
    EqualsStrict = 1L << 41, // ===

    EOF   = 1L << 42,
    Error = 1L << 43,

    NewLine    = 1L << 44,
    Whitespace = 1L << 45,

    KeywordIdentifier = Identifier | Keyword,

}

public partial record TokenIdentifierType : IEnumeration<int, TokenIdentifierType>
{
    public TokenType TokenType { get; }

    private TokenIdentifierType(int value, string name, TokenType tokenType) : this(value, name) {
        TokenType = tokenType;
    }

    public static readonly TokenIdentifierType None = new(0, "None");
    public static readonly TokenIdentifierType Bool = new(1, "Bool", TokenType.Identifier | TokenType.KeywordIdentifier | TokenType.Boolean);


}

public static class TokenTypeExtensions
{
    public static SyntaxKind ToTriviaSyntaxKind(this TokenType type) {
        return type switch {
            TokenType.NewLine      => SyntaxKind.Trivia,
            TokenType.Whitespace   => SyntaxKind.Trivia,
            TokenType.LineComment  => SyntaxKind.Comment,
            TokenType.BlockComment => SyntaxKind.Comment,
            _                      => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}