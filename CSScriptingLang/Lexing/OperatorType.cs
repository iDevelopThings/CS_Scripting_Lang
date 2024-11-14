using System.Diagnostics;
using System.Reflection;

namespace CSScriptingLang.Lexing;

[AttributeUsage(AttributeTargets.Enum | AttributeTargets.Field, AllowMultiple = true)]
public class OperatorChars : Attribute
{
    public string Token      { get; set; }
    public string Identifier { get; set; }

    public OperatorChars(string token, string identifier) {
        Token      = token;
        Identifier = identifier;
    }
}

[AttributeUsage(AttributeTargets.Enum | AttributeTargets.Field, AllowMultiple = true)]
public class OperatorTokenType(OperatorType type) : Attribute
{
    public OperatorType Type { get; set; } = type;
}

public enum OperatorType
{
    None,

    /// <summary>
    /// "+", "add"
    /// </summary>
    [OperatorChars("+", "add")]
    Plus,

    /// <summary>
    /// "+=", "addAssign"
    /// </summary>
    [OperatorChars("+=", "addAssign")]
    PlusEquals,

    /// <summary>
    /// "++", "inc"
    /// </summary>
    [OperatorChars("++", "inc")]
    Increment,

    /// <summary>
    /// "-", "sub"
    /// </summary>
    [OperatorChars("-", "sub")]
    Minus,

    /// <summary>
    /// "-=", "subAssign"
    /// </summary>
    [OperatorChars("-=", "subAssign")]
    MinusEquals,

    /// <summary>
    /// "--", "dec"
    /// </summary>
    [OperatorChars("--", "dec")]
    Decrement,

    /// <summary>
    /// "/", "div"
    /// </summary>
    [OperatorChars("/", "div")]
    Divide,

    [OperatorChars("/=", "divAssign")]
    DivideAssign,

    /// <summary>
    /// "*", "mul"
    /// </summary>
    [OperatorChars("*", "mul")]
    Multiply,

    [OperatorChars("*=", "mulAssign")]
    MultiplyAssign,

    /// <summary>
    /// "%", "mod"
    /// </summary>
    [OperatorChars("%", "mod")]
    Modulus,

    [OperatorChars("%=", "modAssign")]
    ModulusAssign,

    /// <summary>
    /// "**", "pow"
    /// </summary>
    [OperatorChars("**", "pow")]
    Pow,

    [OperatorChars("**=", "powAssign")]
    PowAssign,

    /// <summary>
    /// "==", "eq"
    /// </summary>
    [OperatorChars("==", "eq")]
    Equals,

    /// <summary>
    /// "==="
    /// </summary>
    [OperatorChars("===", "eqStrict")]
    EqualsStrict,

    /// <summary>
    /// "!=", "neq"
    /// </summary>
    [OperatorChars("!=", "neq")]
    NotEquals,

    /// <summary>
    /// "!=="
    /// </summary>
    [OperatorChars("!==", "neqStrict")]
    NotEqualsStrict,

    /// <summary>
    /// ">", "gt"
    /// </summary>
    [OperatorChars(">", "gt")]
    GreaterThan,

    /// <summary>
    /// "<", "lt"
    /// </summary>
    [OperatorChars("<", "lt")]
    LessThan,

    /// <summary>
    /// ">=", "gte"
    /// </summary>
    [OperatorChars(">=", "gte")]
    GreaterThanOrEqual,

    /// <summary>
    /// "<=", "lte"
    /// </summary>
    [OperatorChars("<=", "lte")]
    LessThanOrEqual,

    /// <summary>
    /// "&&", "and"
    /// </summary>
    [OperatorChars("&&", "and")]
    And,

    /// <summary>
    /// "&", "bitwiseAnd"
    /// </summary>
    [OperatorChars("&", "bitwiseAnd")]
    BitwiseAnd,

    /// <summary>
    /// "|", "pipe"
    /// </summary>
    [OperatorChars("|", "pipe")]
    Pipe,

    /// <summary>
    /// "||", "or"
    /// </summary>
    [OperatorChars("||", "or")]
    Or,

    /// <summary>
    /// "!", "not"
    /// </summary>
    [OperatorChars("!", "not")]
    Not,

    /// <summary>
    /// "=", "assign"
    /// </summary>
    [OperatorChars("=", "assign")]
    Assignment,


    [OperatorChars("<<", "bitLeftShift")]
    BitLeftShift,

    [OperatorChars(">>", "bitRightShift")]
    BitRightShift,

    [OperatorChars("^", "bitXor")]
    BitXor,

    [OperatorChars("~", "bitNot")]
    BitNot,

    [OperatorChars("<<=", "bitLeftShiftAssign")]
    BitLeftShiftAssign,

    [OperatorChars(">>=", "bitRightShiftAssign")]
    BitRightShiftAssign,

    [OperatorChars("&=", "bitAndAssign")]
    BitAndAssign,

    [OperatorChars("|=", "bitOrAssign")]
    BitOrAssign,

    [OperatorChars("^=", "bitXorAssign")]
    BitXorAssign,

    [OperatorChars("=>", "arrow")]
    Arrow,
}

public class OperatorTypes
{
    public struct OperatorInfo
    {
        public OperatorType Type       { get; set; }
        public TokenType    TokenType  { get; set; }
        public string       Token      { get; set; }
        public string       Identifier { get; set; }
    }

    public static Dictionary<string, OperatorType>       TokenToOperatorType      { get; } = new();
    public static Dictionary<string, OperatorType>       IdentifierToOperatorType { get; } = new();
    public static Dictionary<OperatorType, string>       OperatorTypeToToken      { get; } = new();
    public static Dictionary<OperatorType, OperatorInfo> Info                     { get; } = new();

    // For ex, with (`+` `++` `+=`), `OperatorCharCount['+']` will be 2
    // This will let the lexer read the correct number of characters for the operator
    public static Dictionary<char, int> OperatorCharCount { get; } = new();

    static OperatorTypes() {

        var enumType = typeof(OperatorType);
        var fields   = enumType.GetFields(BindingFlags.Public | BindingFlags.Static);

        var tokenTypeEnum = typeof(TokenType);
        var operatorTokenFields = tokenTypeEnum.GetFields(BindingFlags.Public | BindingFlags.Static)
           .Select(f => (Field: f, Attr: f.GetCustomAttribute<OperatorTokenType>()))
           .Where(t => t.Attr != null)
           .ToDictionary(t => t.Attr!.Type, t => t.Field.Name);

        foreach (var field in fields) {
            var attr = field.GetCustomAttribute<OperatorChars>();
            if (attr == null) {
                continue;
            }

            var token      = attr.Token;
            var identifier = attr.Identifier;
            var op         = (OperatorType) field.GetValue(null)!;

            TokenToOperatorType[token]           = op;
            IdentifierToOperatorType[identifier] = op;
            OperatorTypeToToken[op]              = token;
            Info[op] = new OperatorInfo {
                Type       = op,
                Token      = token,
                Identifier = identifier,
                TokenType = operatorTokenFields.TryGetValue(op, out var tokenField)
                    ? TokenType.Operator | (TokenType) tokenTypeEnum.GetField(tokenField)!.GetValue(null)!
                    : TokenType.Operator,
            };

            if (OperatorCharCount.ContainsKey(token[0])) {
                OperatorCharCount[token[0]] = Math.Max(OperatorCharCount[token[0]], token.Length);
            } else {
                OperatorCharCount.Add(token[0], token.Length);
            }
        }
    }
}

public static class OperatorTypeExtensions
{
    [DebuggerStepThrough]
    public static string GetOverloadFnName(this OperatorType op) {
        return $"operator_{OperatorTypes.Info[op].Identifier}";
    }

    public static bool IsUnaryAssign(this OperatorType op) {
        return op switch {
            OperatorType.Increment => true,
            OperatorType.Decrement => true,
            OperatorType.BitNot    => true,
            _                      => false,
        };
    }

    public static bool IsEquality(this OperatorType op) {
        return op switch {
            OperatorType.Equals          => true,
            OperatorType.EqualsStrict    => true,
            OperatorType.NotEquals       => true,
            OperatorType.NotEqualsStrict => true,
            _                            => false,
        };
    }

    public static bool IsComparison(this OperatorType op) {
        return op switch {
            OperatorType.GreaterThan        => true,
            OperatorType.LessThan           => true,
            OperatorType.GreaterThanOrEqual => true,
            OperatorType.LessThanOrEqual    => true,
            _                               => false,
        };
    }
    public static bool IsTerm(this OperatorType op) {
        return op switch {
            OperatorType.Plus  => true,
            OperatorType.Minus => true,
            // OperatorType.PlusEquals  => true,
            // OperatorType.MinusEquals => true,
            _ => false,
        };
    }

    public static bool IsFactor(this OperatorType op) {
        return op switch {
            OperatorType.Multiply => true,
            OperatorType.Divide   => true,
            OperatorType.Modulus  => true,
            OperatorType.BitXor   => true,
            // OperatorType.MultiplyAssign => true,
            // OperatorType.DivideAssign   => true,
            // OperatorType.ModulusAssign  => true,
            _ => false,
        };
    }

    public static bool IsAssignment(this OperatorType op) {
        return op switch {
            OperatorType.Assignment     => true,
            OperatorType.PlusEquals     => true,
            OperatorType.MinusEquals    => true,
            OperatorType.MultiplyAssign => true,
            OperatorType.DivideAssign   => true,
            OperatorType.ModulusAssign  => true,
            _                           => false,
        };
    }

    public static bool IsOr(this OperatorType op) {
        return op switch {
            OperatorType.Or   => true,
            OperatorType.Pipe => true,
            _                 => false,
        };
    }
    public static bool IsAnd(this OperatorType op) {
        return op switch {
            OperatorType.And        => true,
            OperatorType.BitwiseAnd => true,
            _                       => false,
        };
    }

    public static bool IsUnary(this OperatorType op) {
        return op switch {
            OperatorType.Not    => true,
            OperatorType.Minus  => true,
            OperatorType.Plus   => true,
            OperatorType.BitNot => true,
            _                   => false,
        };
    }
    public static bool IsBitwiseShift(this OperatorType op) {
        return op switch {
            OperatorType.BitLeftShift  => true,
            OperatorType.BitRightShift => true,
            _                          => false,
        };
    }

    public static string ToSymbol(this OperatorType op) {
        return op switch {
            OperatorType.Plus               => "+",
            OperatorType.PlusEquals         => "+=",
            OperatorType.Increment          => "++",
            OperatorType.Minus              => "-",
            OperatorType.MinusEquals        => "-=",
            OperatorType.Decrement          => "--",
            OperatorType.Multiply           => "*",
            OperatorType.Divide             => "/",
            OperatorType.Modulus            => "%",
            OperatorType.Equals             => "==",
            OperatorType.NotEquals          => "!=",
            OperatorType.GreaterThan        => ">",
            OperatorType.LessThan           => "<",
            OperatorType.GreaterThanOrEqual => ">=",
            OperatorType.LessThanOrEqual    => "<=",
            OperatorType.And                => "&&",
            OperatorType.BitwiseAnd         => "&",
            OperatorType.Pipe               => "|",
            OperatorType.Or                 => "||",
            OperatorType.Not                => "!",
            OperatorType.Assignment         => "=",
            _                               => "",
        };
    }
}