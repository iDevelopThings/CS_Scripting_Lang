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

    LParen   = 1L << 8,  // (
    RParen   = 1L << 9,  // )
    LBrace   = 1L << 10, // {
    RBrace   = 1L << 11, // }
    LBracket = 1L << 12, // [
    RBracket = 1L << 13, // ]
    LAngle   = 1L << 14, // <
    RAngle   = 1L << 15, // >

    Arrow      = 1L << 16, // =>
    Dot        = 1L << 17, // .
    DotDotDot  = 1L << 18, // ...
    Semicolon  = 1L << 19, // ;
    Comma      = 1L << 20, // ,
    Colon      = 1L << 21, // :
    And        = 1L << 22, // &
    At         = 1L << 23, // @
    Underscore = 1L << 24, // _
    Question   = 1L << 26, // ?
    Tilde      = 1L << 41, // ~
    Hash       = 1L << 42, // #
    Dollar     = 1L << 43, // $
    Backslash  = 1L << 44, // \


    LineComment  = 1L << 45, // //
    BlockComment = 1L << 46, // /* */

    Operator = 1L << 47, // Operator is a catch-all for all operators

    EOF   = 1L << 48,
    Error = 1L << 49,

    KeywordIdentifier = Identifier | Keyword,

}