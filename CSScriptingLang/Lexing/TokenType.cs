namespace CSScriptingLang.Lexing;

[Flags]
public enum TokenType : long
{
    None = 0,

    Identifier = 1 << 0,
    Keyword    = 1 << 1,

    Int32  = 1 << 2,
    Int64  = 1 << 3,
    Float  = 1 << 4,
    Double = 1 << 5,
    // Number  = Int32 | Int64 | Float | Double,

    Boolean = 1 << 6,
    String  = 1 << 7,

    LParen   = 1 << 8,
    RParen   = 1 << 9,
    LBrace   = 1 << 10,
    RBrace   = 1 << 11,
    LBracket = 1 << 12,
    RBracket = 1 << 13,

    Dot       = 1 << 14,
    Semicolon = 1 << 15,
    Comma     = 1 << 16,
    Colon     = 1 << 17,

    LineComment  = 1 << 18,
    BlockComment = 1 << 19,

    Operator = 1 << 20, // Operator is a catch-all for all operators

    [Keyword("import")]
    Import = 1 << 21,

    [Keyword("if")]
    If = 1 << 22,

    [Keyword("else")]
    Else = 1 << 23,

    [Keyword("while")]
    While = 1 << 24,

    [Keyword("for")]
    For = 1 << 25,

    [Keyword("function")]
    Function = 1 << 26,

    [Keyword("return")]
    Return = 1 << 27,

    [Keyword("var")]
    Var = 1 << 28,

    [Keyword("range")]
    Range = 1 << 29,

    [Keyword("defer")]
    Defer = 1 << 30,

    [Keyword("true")]
    True = Boolean | Keyword,

    [Keyword("false")]
    False = Boolean | Keyword,

    EOF   = 1 << 31,
    Error = 1L << 32,

    KeywordIdentifier = Identifier | Keyword,
}