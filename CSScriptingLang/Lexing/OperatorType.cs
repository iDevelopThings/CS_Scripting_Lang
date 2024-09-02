
namespace CSScriptingLang.Lexing;

public enum OperatorType
{
    None,

    Plus,               // +
    PlusEquals,         // +=
    Minus,              // -
    MinusEquals,        // -=
    Divide,             // /
    Multiply,           // *
    Modulus,            // %
    Increment,          // ++
    Decrement,          // --
    Equals,             // ==
    NotEquals,          // !=
    GreaterThan,        // >
    LessThan,           // <
    GreaterThanOrEqual, // >=
    LessThanOrEqual,    // <=
    And,                // &&
    BitwiseAnd,         // &
    Pipe,               // |
    Or,                 // ||
    Not,                // !
    Assignment,         // =
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