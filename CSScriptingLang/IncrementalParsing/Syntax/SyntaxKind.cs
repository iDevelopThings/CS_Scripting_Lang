namespace CSScriptingLang.IncrementalParsing.Syntax;

public enum SyntaxKind : ushort
{
    None,
    Source,

    Failed,
    UnknownStatement,
    UnknownExpression,

    Comment,
    Trivia,
    Block,

    AttributeDeclaration,
    TypeDeclaration,

    InterfaceDeclaration,
    SignalDeclaration,

    InlineFunctionDeclaration, // Regular unnamed function, or lambda `(args) => expr` / `(args) => { ... }`
    FunctionDeclaration,       // full fn; `function name{<T>}?(... args) type? { ... }`
    DefDeclaration_Function,   // `def func() type`

    StructDeclaration,
    StructDeclarationField,
    StructDeclarationConstructor,
    StructDeclarationMethod,

    EnumDeclaration,
    EnumMemberConstructor,
    EnumMemberMethod,
    EnumMemberDeclaration,               // `EnumMember`
    EnumMemberDeclaration_WithValue,     // `EnumMember = 10`
    EnumMemberDeclaration_WithValueCtor, // `EnumMember(value, value2)`

    VariableDeclaration, // `var x = expr` or `var (a, b) = (1, 2)`

    BooleanExpression,
    NullValueExpression,
    StringExpression,
    Int32Expression,
    Int64Expression,
    FloatExpression,
    DoubleExpression,

    ArrayLiteralExpression,
    ObjectProperty,
    ObjectLiteralExpression,

    IdentifierExpression,     // A standalone identifier, ie `x`
    TypeIdentifierExpression, // An identifier which optionally can have type params, ie `Something<T>`

    BinaryOpExpression,
    PrefixUnaryOpExpression,
    PostfixUnaryOpExpression,

    CallExpression,
    ArgumentList,            // This would be call expr arguments, ie `(expr, expr, expr)` `(1, 2, 3 * 4)`
    ArgumentListDeclaration, // This would be function arguments, ie `(type name, type name, type name)`
    ArgumentDeclaration,     // This would be function argument, ie `type name`
    TypeParametersList,      // For idents; `Type<T, U, V>` / `Func<T>(T arg)`
    TypeParameter,           // Represents a type in a type list; `T` in `Type<T, U, V>`
    TupleExpression,         // Could be for var; ie: `var (a, b)` or exprs ie: `(expr, expr)`

    IndexAccessExpression,  // `some.member[expr]`
    MemberAccessExpression, // `some.member`
    RangeExpression,        // Used in loops; `for (var x = range list)`

    MatchExpression,
    MatchPattern_Default,
    MatchPattern_Literal,
    MatchPattern_IsType, // `case is TypeName`
    MatchPattern_Identifier,
    MatchCase,

    IfStatement,
    IfClause,

    ForWhileLoop,
    ForIndexedLoop,
    ForRange,

    AwaitStatement,
    DeferStatement,
    ReturnStatement,
    BreakStatement,
    ContinueStatement,
    YieldStatement,
}