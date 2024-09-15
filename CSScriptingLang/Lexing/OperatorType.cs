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

public enum OperatorType
{
    None,

    [OperatorChars("+", "add")]
    Plus,

    [OperatorChars("+=", "addAssign")]
    PlusEquals,

    [OperatorChars("++", "inc")]
    Increment,

    [OperatorChars("-", "sub")]
    Minus,

    [OperatorChars("-=", "subAssign")]
    MinusEquals,

    [OperatorChars("--", "dec")]
    Decrement,

    [OperatorChars("/", "div")]
    Divide,

    [OperatorChars("*", "mul")]
    Multiply,

    [OperatorChars("%", "mod")]
    Modulus,

    [OperatorChars("^", "pow")]
    Pow,

    [OperatorChars("==", "eq")]
    Equals,

    [OperatorChars("!=", "neq")]
    NotEquals,

    [OperatorChars(">", "gt")]
    GreaterThan,

    [OperatorChars("<", "lt")]
    LessThan,

    [OperatorChars(">=", "gte")]
    GreaterThanOrEqual,

    [OperatorChars("<=", "lte")]
    LessThanOrEqual,

    [OperatorChars("&&", "and")]
    And,

    [OperatorChars("&", "bitwiseAnd")]
    BitwiseAnd,

    [OperatorChars("|", "pipe")]
    Pipe,

    [OperatorChars("||", "or")]
    Or,

    [OperatorChars("!", "not")]
    Not,

    [OperatorChars("=", "assign")]
    Assignment,
}

public class OperatorTypes
{
    public static Dictionary<string, OperatorType> TokenToOperatorType      { get; } = new();
    public static Dictionary<string, OperatorType> IdentifierToOperatorType { get; } = new();
    public static Dictionary<OperatorType, string> OperatorTypeToToken      { get; } = new();

    // For ex, with (`+` `++` `+=`), `OperatorCharCount['+']` will be 2
    // This will let the lexer read the correct number of characters for the operator
    public static Dictionary<char, int> OperatorCharCount { get; } = new();

    static OperatorTypes() {

        var enumType = typeof(OperatorType);
        var fields   = enumType.GetFields(BindingFlags.Public | BindingFlags.Static);

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
    public static bool IsBinaryArithmetic(this OperatorType op) {
        return op switch {
            OperatorType.Plus     => true,
            OperatorType.Minus    => true,
            OperatorType.Multiply => true,
            OperatorType.Divide   => true,
            OperatorType.Modulus  => true,
            _                     => false,
        };
    }

    public static bool IsUnaryArithmetic(this OperatorType op) {
        return op switch {
            OperatorType.Increment => true,
            OperatorType.Decrement => true,
            _                      => false,
        };
    }

    public static bool IsComparison(this OperatorType op) {
        return op switch {
            OperatorType.Equals             => true,
            OperatorType.NotEquals          => true,
            OperatorType.GreaterThan        => true,
            OperatorType.LessThan           => true,
            OperatorType.GreaterThanOrEqual => true,
            OperatorType.LessThanOrEqual    => true,
            _                               => false,
        };
    }

    public static bool IsLogical(this OperatorType op) {
        return op switch {
            OperatorType.And => true,
            OperatorType.Or  => true,
            _                => false,
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